using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.Json;
using DimensionalQuantities.IntegrationTests.Delivery;
using Xunit;

namespace DimensionalQuantities.IntegrationTests;

public sealed class PackageSourceTests
{
	[Theory]
	[InlineData("DimensionalQuantities", "lib/netstandard2.0", "DimensionalQuantities")]
	[InlineData("DimensionalQuantities", "analyzers/dotnet/cs", "DimensionalQuantities")]
	[InlineData("DimensionalQuantities", "analyzers/dotnet/cs", "DimensionalQuantities.Roslyn")]
	[InlineData("DimensionalQuantities", "analyzers/dotnet/cs", "DimensionalQuantities.Roslyn.CodeFixes")]
	[InlineData("DimensionalQuantities.Extensions.Metric", "lib/netstandard2.0", "DimensionalQuantities.Extensions.Metric")]
	[InlineData("DimensionalQuantities.Extensions.Imperial", "lib/netstandard2.0", "DimensionalQuantities.Extensions.Imperial")]
	[InlineData("DimensionalQuantities.Extensions.Nautical", "lib/netstandard2.0", "DimensionalQuantities.Extensions.Nautical")]
	[InlineData("DimensionalQuantities.Extensions.CgsEmag", "lib/netstandard2.0", "DimensionalQuantities.Extensions.CgsEmag")]
	public async Task PackagedSymbolsMapSourceToCurrentCommit(string id, string directory, string assembly)
	{
		using PackageSandbox sandbox = new();
		await sandbox.Pack(id);
		using ZipArchive symbols = ZipFile.OpenRead(sandbox.ArchivePath(id, symbols: directory == "lib/netstandard2.0"));
		using Stream stream = symbols.GetEntry($"{directory}/{assembly}.pdb")!.Open();
		using MemoryStream pdb = new();
		await stream.CopyToAsync(pdb, TestContext.Current.CancellationToken);
		pdb.Position = 0;
		using MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(pdb);
		MetadataReader reader = provider.GetMetadataReader();
		Guid sourceLinkKind = new("CC110556-A091-4D38-9FEC-25AB9A351A6A");
		CustomDebugInformation sourceLink = Assert.Single(reader.CustomDebugInformation.Select(reader.GetCustomDebugInformation),
			info => reader.GetGuid(info.Kind) == sourceLinkKind);
		using JsonDocument mapping = JsonDocument.Parse(reader.GetBlobBytes(sourceLink.Value));
		string commit = ReadCurrentCommit();

		Assert.Contains(mapping.RootElement.GetProperty("documents").EnumerateObject(),
			entry => entry.Value.GetString() == $"https://raw.githubusercontent.com/GoSimulate/DimensionalQuantities/{commit}/*");
	}

	[Theory]
	[InlineData("Metric")]
	[InlineData("Imperial")]
	[InlineData("Nautical")]
	[InlineData("CgsEmag")]
	public async Task ExtensionSymbolsEmbedGeneratedSourceMatchingBuildOutput(string extension)
	{
		string id = "DimensionalQuantities.Extensions." + extension;
		using PackageSandbox sandbox = new();
		await sandbox.Pack(id);
		using ZipArchive symbols = ZipFile.OpenRead(sandbox.ArchivePath(id, symbols: true));
		using Stream stream = symbols.GetEntry($"lib/netstandard2.0/{id}.pdb")!.Open();
		using MemoryStream pdb = new();
		await stream.CopyToAsync(pdb, TestContext.Current.CancellationToken);
		pdb.Position = 0;
		using MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(pdb);
		MetadataReader reader = provider.GetMetadataReader();
		string fileName = $"{id}.{extension}Units.g.cs";
		DocumentHandle document = Assert.Single(reader.Documents,
			handle => Path.GetFileName(reader.GetString(reader.GetDocument(handle).Name)) == fileName);
		Guid embeddedSourceKind = new("0E8A571B-6926-466E-B4AD-8AB04611F5FE");
		CustomDebugInformation embedded = Assert.Single(reader.GetCustomDebugInformation(document).Select(reader.GetCustomDebugInformation),
			info => reader.GetGuid(info.Kind) == embeddedSourceKind);
		byte[] actual = DecodeEmbeddedSource(reader.GetBlobBytes(embedded.Value));
		string configuration = typeof(PackageSourceTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
		byte[] expected = await File.ReadAllBytesAsync(Path.Combine(SourceDirectory, id, "obj", configuration, "netstandard2.0", "generated",
			"DimensionalQuantities.Roslyn", "DimensionalQuantities.Roslyn.Generators.QuantityGenerator", fileName), TestContext.Current.CancellationToken);

		Assert.Equal(expected, actual);
	}

	private static string SourceDirectory => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

	private static string ReadCurrentCommit()
	{
		ProcessStartInfo start = new("git") { WorkingDirectory = SourceDirectory, RedirectStandardOutput = true, RedirectStandardError = true };
		start.ArgumentList.Add("rev-parse");
		start.ArgumentList.Add("HEAD");
		using Process process = Process.Start(start)!;
		string commit = process.StandardOutput.ReadToEnd().Trim();
		string error = process.StandardError.ReadToEnd();
		process.WaitForExit();
		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException($"git rev-parse HEAD failed: {error}");
		}
		return commit;
	}

	private static byte[] DecodeEmbeddedSource(byte[] blob)
	{
		int uncompressedSize = BinaryPrimitives.ReadInt32LittleEndian(blob);
		if (uncompressedSize == 0)
		{
			return blob[sizeof(int)..];
		}
		using MemoryStream compressed = new(blob, sizeof(int), blob.Length - sizeof(int));
		using DeflateStream deflate = new(compressed, CompressionMode.Decompress);
		using MemoryStream source = new();
		deflate.CopyTo(source);
		return source.ToArray();
	}
}
