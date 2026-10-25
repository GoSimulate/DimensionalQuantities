using System.Buffers.Binary;
using System.Diagnostics;
using System.Reflection;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using Microsoft.Build.Locator;
using Xunit;

namespace DimensionalQuantities.IntegrationTests.Packaging;

[Collection("MSBuild")]
public sealed class SymbolPackageTests : IDisposable
{
	private static readonly Guid _sourceLinkKind = new("CC110556-A091-4D38-9FEC-25AB9A351A6A");
	private static readonly Guid _embeddedSourceKind = new("0E8A571B-6926-466E-B4AD-8AB04611F5FE");
	private readonly string _scratch = Directory.CreateTempSubdirectory("dimensional-quantities-symbols-").FullName;

	static SymbolPackageTests()
	{
		if (!MSBuildLocator.IsRegistered)
		{
			MSBuildLocator.RegisterDefaults();
		}
	}

	[Fact]
	public void CoreSymbolPackageMatchesRuntimeAndAnalyzerAssemblies()
	{
		(FileInfo package, FileInfo symbols) = Pack("DimensionalQuantities");
		using ZipArchive binaries = ZipFile.OpenRead(package.FullName);
		using ZipArchive symbolArchive = ZipFile.OpenRead(symbols.FullName);
		string commit = GetCurrentCommit();
		foreach (string stem in new[]
		{
			"lib/netstandard2.0/DimensionalQuantities",
			"analyzers/dotnet/cs/DimensionalQuantities",
			"analyzers/dotnet/cs/DimensionalQuantities.Roslyn",
			"analyzers/dotnet/cs/DimensionalQuantities.Roslyn.CodeFixes",
		})
		{
			SymbolDetails details = ReadSymbolDetails(binaries, symbolArchive, stem);
			Assert.Multiple(
				() => Assert.Equal(details.AssemblyGuid, details.PdbGuid),
				() => Assert.Equal(details.AssemblyStamp, details.PdbStamp),
				() => Assert.Contains($"https://raw.githubusercontent.com/GoSimulate/DimensionalQuantities/{commit}/*", details.SourceUrls));
		}
	}

	[Fact]
	public void MetricSymbolPackageMatchesAssemblyAndEmbedsGeneratedSource()
	{
		(FileInfo package, FileInfo symbols) = Pack("DimensionalQuantities.Extensions.Metric");
		using ZipArchive binaries = ZipFile.OpenRead(package.FullName);
		using ZipArchive symbolArchive = ZipFile.OpenRead(symbols.FullName);
		SymbolDetails details = ReadSymbolDetails(binaries, symbolArchive, "lib/netstandard2.0/DimensionalQuantities.Extensions.Metric");
		string commit = GetCurrentCommit();
		string sourceDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
		string configuration = typeof(SymbolPackageTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
		byte[] generatedSource = File.ReadAllBytes(Path.Combine(sourceDirectory, "DimensionalQuantities.Extensions.Metric", "obj", configuration, "netstandard2.0",
			"generated", "DimensionalQuantities.Roslyn", "DimensionalQuantities.Roslyn.Generators.QuantityGenerator", "DimensionalQuantities.Extensions.Metric.MetricUnits.g.cs"));
		Assert.Multiple(
			() => Assert.Equal(details.AssemblyGuid, details.PdbGuid),
			() => Assert.Equal(details.AssemblyStamp, details.PdbStamp),
			() => Assert.Contains($"https://raw.githubusercontent.com/GoSimulate/DimensionalQuantities/{commit}/*", details.SourceUrls),
			() => Assert.Equal(generatedSource, details.EmbeddedMetricConversions));
	}

	private (FileInfo Package, FileInfo Symbols) Pack(string projectName)
	{
		string sourceDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
		FileInfo package = PackageTestSupport.PackLibrary(Path.Combine(sourceDirectory, projectName, projectName + ".csproj"), _scratch);
		FileInfo symbols = new(Path.ChangeExtension(package.FullName, ".snupkg"));
		if (!symbols.Exists)
		{
			throw new FileNotFoundException("Pack did not create the expected symbol package.", symbols.FullName);
		}
		return (package, symbols);
	}

	private static SymbolDetails ReadSymbolDetails(ZipArchive binaries, ZipArchive symbols, string stem)
	{
		byte[] assemblyBytes = ReadEntry(binaries, stem + ".dll");
		byte[] pdbBytes = ReadEntry(symbols, stem + ".pdb");
		using PEReader assembly = new(new MemoryStream(assemblyBytes));
		using MetadataReaderProvider pdb = MetadataReaderProvider.FromPortablePdbStream(new MemoryStream(pdbBytes));
		DebugDirectoryEntry codeView = assembly.ReadDebugDirectory().Single(entry => entry.Type == DebugDirectoryEntryType.CodeView);
		CodeViewDebugDirectoryData codeViewData = assembly.ReadCodeViewDebugDirectoryData(codeView);
		MetadataReader reader = pdb.GetMetadataReader();
		byte[] pdbId = reader.DebugMetadataHeader!.Id.ToArray();
		string sourceLink = reader.CustomDebugInformation
			.Select(reader.GetCustomDebugInformation)
			.Where(info => reader.GetGuid(info.Kind) == _sourceLinkKind)
			.Select(info => Encoding.UTF8.GetString(reader.GetBlobBytes(info.Value)))
			.Single();
		using JsonDocument sourceLinkDocument = JsonDocument.Parse(sourceLink);
		string[] sourceUrls = sourceLinkDocument.RootElement.GetProperty("documents").EnumerateObject()
			.Select(mapping => mapping.Value.GetString()!).ToArray();
		byte[]? embeddedMetricConversions = reader.Documents
			.Where(handle => reader.GetString(reader.GetDocument(handle).Name).EndsWith("DimensionalQuantities.Extensions.Metric.MetricUnits.g.cs", StringComparison.Ordinal))
			.SelectMany(handle => reader.GetCustomDebugInformation(handle))
			.Select(reader.GetCustomDebugInformation)
			.Where(info => reader.GetGuid(info.Kind) == _embeddedSourceKind)
			.Select(info => DecodeEmbeddedSource(reader.GetBlobBytes(info.Value)))
			.SingleOrDefault();
		return new(codeViewData.Guid, codeView.Stamp, new Guid(pdbId.AsSpan(0, 16)), BitConverter.ToUInt32(pdbId, 16), sourceUrls, embeddedMetricConversions);
	}

	private static byte[] DecodeEmbeddedSource(byte[] blob)
	{
		if (blob.Length < sizeof(int))
		{
			throw new InvalidDataException("Embedded source blob is too short.");
		}
		int uncompressedSize = BinaryPrimitives.ReadInt32LittleEndian(blob);
		if (uncompressedSize == 0)
		{
			return blob[sizeof(int)..];
		}
		if (uncompressedSize < 0)
		{
			throw new InvalidDataException("Embedded source has an invalid uncompressed size.");
		}
		using MemoryStream compressed = new(blob, sizeof(int), blob.Length - sizeof(int));
		using DeflateStream deflate = new(compressed, CompressionMode.Decompress);
		using MemoryStream source = new();
		deflate.CopyTo(source);
		byte[] bytes = source.ToArray();
		if (bytes.Length != uncompressedSize)
		{
			throw new InvalidDataException("Embedded source length does not match its header.");
		}
		return bytes;
	}

	private static byte[] ReadEntry(ZipArchive archive, string path)
	{
		ZipArchiveEntry entry = archive.GetEntry(path) ?? throw new InvalidOperationException($"Package does not contain {path}.");
		using Stream stream = entry.Open();
		using MemoryStream bytes = new();
		stream.CopyTo(bytes);
		return bytes.ToArray();
	}

	private static string GetCurrentCommit()
	{
		string sourceDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
		ProcessStartInfo start = new("git") { WorkingDirectory = sourceDirectory, RedirectStandardOutput = true, RedirectStandardError = true };
		start.ArgumentList.Add("rev-parse");
		start.ArgumentList.Add("HEAD");
		using Process process = Process.Start(start) ?? throw new InvalidOperationException("Could not start git.");
		string commit = process.StandardOutput.ReadToEnd().Trim();
		string error = process.StandardError.ReadToEnd();
		process.WaitForExit();
		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException($"git rev-parse HEAD failed: {error}");
		}
		return commit;
	}

	private sealed record SymbolDetails(Guid AssemblyGuid, uint AssemblyStamp, Guid PdbGuid, uint PdbStamp, string[] SourceUrls, byte[]? EmbeddedMetricConversions);

	public void Dispose() => Directory.Delete(_scratch, recursive: true);
}
