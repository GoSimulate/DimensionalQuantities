using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Xml.Linq;

namespace DimensionalQuantities.IntegrationTests.Delivery;

internal sealed class PackageSandbox : IDisposable
{
	public string Root { get; } = Path.Combine(Path.GetTempPath(), "dq-consumer-" + Guid.NewGuid().ToString("N"));
	public string Feed => Path.Combine(Root, "feed");
	public string Consumer => Path.Combine(Root, "consumer");
	public string Cache => Path.Combine(Root, "packages");
	public string Version { get; private set; } = "";

	public PackageSandbox()
	{
		Directory.CreateDirectory(Feed);
		Directory.CreateDirectory(Consumer);
		File.WriteAllText(Path.Combine(Root, "NuGet.Config"), $"<configuration><packageSources><clear/><add key=\"local\" value=\"{Feed}\"/></packageSources></configuration>");
	}

	public async Task Pack(params string[] optionalPackages)
	{
		string repository = AppContext.BaseDirectory;
		while (!File.Exists(Path.Combine(repository, "global.json")))
		{
			repository = Directory.GetParent(repository)!.FullName;
		}

		File.Copy(Path.Combine(repository, "global.json"), Path.Combine(Root, "global.json"), overwrite: true);
		string configuration = typeof(PackageSandbox).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;

		foreach (string id in new[] { "DimensionalQuantities" }.Concat(optionalPackages).Distinct())
		{
			ProcessResult result = await Run(repository, "pack", Path.Combine(repository, "src", id, id + ".csproj"), "-c", configuration, "--no-build", "--no-restore", "-o", Feed, "-p:NuspecOutputPath=" + Path.Combine(Root, "nuspec", id), "--nologo");
			result.EnsureSuccess();
		}

		using ZipArchive archive = ZipFile.OpenRead(ArchivePath("DimensionalQuantities"));
		using Stream stream = archive.Entries.Single(e => e.FullName.EndsWith(".nuspec", StringComparison.Ordinal)).Open();
		Version = XDocument.Load(stream).Descendants().Single(e => e.Name.LocalName == "version").Value;
	}

	public string ArchivePath(string id, bool symbols = false) => Directory.GetFiles(Feed, id + ".*." + (symbols ? "snupkg" : "nupkg"))
		.Single(path => Path.GetFileName(path).StartsWith(id + ".", StringComparison.Ordinal) &&
						!Path.GetFileName(path)[(id.Length + 1)..].StartsWith("Extensions.", StringComparison.Ordinal));

	public PackageSandbox CreateConsumer()
	{
		PackageSandbox consumer = new();
		consumer.Version = Version;
		File.Copy(Path.Combine(Root, "global.json"), Path.Combine(consumer.Root, "global.json"));
		foreach (string archive in Directory.GetFiles(Feed))
		{
			File.Copy(archive, Path.Combine(consumer.Feed, Path.GetFileName(archive)));
		}
		return consumer;
	}

	public async Task PackProducer(string id, string source, params string[] packageIds)
	{
		string directory = Path.Combine(Root, id);
		Directory.CreateDirectory(directory);
		string references = string.Join("\n", packageIds.Select(packageId => $"<PackageReference Include=\"{packageId}\" Version=\"[{Version}]\"/>"));
		File.WriteAllText(Path.Combine(directory, id + ".csproj"), $$"""
			<Project Sdk="Microsoft.NET.Sdk">
			  <PropertyGroup>
			    <TargetFramework>net11.0</TargetFramework>
			    <LangVersion>preview</LangVersion>
			    <ImplicitUsings>enable</ImplicitUsings>
			    <PackageVersion>{{Version}}</PackageVersion>
			  </PropertyGroup>
			  <ItemGroup>
			    {{references}}
			  </ItemGroup>
			</Project>
			""");
		File.WriteAllText(Path.Combine(directory, id + ".cs"), source);
		(await Run(directory, "restore", "--configfile", Path.Combine(Root, "NuGet.Config"), "--packages", Cache, "--nologo")).EnsureSuccess();
		(await Run(directory, "pack", "-c", "Release", "--no-restore", "-o", Feed, "--nologo")).EnsureSuccess();
	}

	public void WriteConsumer(string source, params string[] packageIds)
	{
		string references = string.Join("\n", packageIds.Select(id => $"<PackageReference Include=\"{id}\" Version=\"[{Version}]\"/>"));
		File.WriteAllText(Path.Combine(Consumer, "Consumer.csproj"), $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net11.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>preview</LangVersion><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
              <ItemGroup>{{references}}</ItemGroup>
            </Project>
            """);
		File.WriteAllText(Path.Combine(Consumer, "Program.cs"), source);
	}

	public async Task<ProcessResult> BuildConsumer()
	{
		(await Run(Consumer, "restore", "--configfile", Path.Combine(Root, "NuGet.Config"), "--packages", Cache, "--nologo")).EnsureSuccess();
		return await Run(Consumer, "build", "-c", "Release", "--no-restore", "--nologo");
	}

	public async Task<string> ExecuteConsumer()
	{
		(await BuildConsumer()).EnsureSuccess();
		return await ExecuteBuiltConsumer();
	}

	public async Task<string> ExecuteBuiltConsumer()
	{
		ProcessResult result = await Run(Consumer, Path.Combine(Consumer, "bin", "Release", "net11.0", "Consumer.dll"));
		result.EnsureSuccess();
		return result.Output.Trim();
	}

	public async Task<ProcessResult> Run(string workingDirectory, params string[] arguments)
	{
		ProcessStartInfo start = new("dotnet") { WorkingDirectory = workingDirectory, RedirectStandardOutput = true, RedirectStandardError = true };
		foreach (string argument in arguments)
		{
			start.ArgumentList.Add(argument);
		}

		if (workingDirectory == Consumer)
		{
			start.Environment["NUGET_PACKAGES"] = Cache;
		}

		using Process process = Process.Start(start)!;
		Task<string> stdout = process.StandardOutput.ReadToEndAsync();
		Task<string> stderr = process.StandardError.ReadToEndAsync();
		await process.WaitForExitAsync();
		return new(process.ExitCode, await stdout + "\n" + await stderr);
	}

	public void Dispose() => Directory.Delete(Root, recursive: true);

	internal sealed record ProcessResult(int ExitCode, string Output)
	{
		public void EnsureSuccess()
		{
			if (ExitCode != 0)
			{
				throw new InvalidOperationException($"dotnet exited {ExitCode}:\n{Output}");
			}
		}
	}
}
