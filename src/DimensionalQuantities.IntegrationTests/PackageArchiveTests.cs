using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using DimensionalQuantities.IntegrationTests.Delivery;
using Xunit;

namespace DimensionalQuantities.IntegrationTests;

public sealed class PackageArchiveTests
{
	[Theory]
	[InlineData("DimensionalQuantities")]
	[InlineData("DimensionalQuantities.Extensions.Metric")]
	[InlineData("DimensionalQuantities.Extensions.Imperial")]
	[InlineData("DimensionalQuantities.Extensions.Nautical")]
	[InlineData("DimensionalQuantities.Extensions.CgsEmag")]
	public async Task RuntimePackagesShipNetstandardBinaryAndExactDependencyClosure(string id)
	{
		using PackageSandbox sandbox = new();
		await sandbox.Pack(id);
		using ZipArchive archive = ZipFile.OpenRead(sandbox.ArchivePath(id));
		ZipArchiveEntry binary = archive.GetEntry($"lib/netstandard2.0/{id}.dll")!;
		Assert.NotNull(binary);
		using (MemoryStream contents = new())
		{
			using (Stream stream = binary.Open())
			{
				stream.CopyTo(contents);
			}

			contents.Position = 0;
			using PEReader pe = new(contents);
			MetadataReader metadata = pe.GetMetadataReader();
			Assert.Multiple(
				() => Assert.Equal(id, metadata.GetString(metadata.GetAssemblyDefinition().Name)),
				() => Assert.DoesNotContain(metadata.AssemblyReferences.Select(handle => metadata.GetString(metadata.GetAssemblyReference(handle).Name)),
					name => name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal) || name is "JetBrains.Annotations" or "PolySharp"));
		}
		using Stream manifest = archive.Entries.Single(e => e.FullName.EndsWith(".nuspec", StringComparison.Ordinal)).Open();
		XDocument nuspec = XDocument.Load(manifest);
		XElement[] dependencies = nuspec.Descendants().Where(e => e.Name.LocalName == "dependency").ToArray();
		if (id == "DimensionalQuantities")
		{
			Assert.Empty(dependencies);
		}
		else
		{
			XElement dependency = Assert.Single(dependencies);
			Assert.Multiple(
				() => Assert.Equal("DimensionalQuantities", (string?)dependency.Attribute("id")),
				() => Assert.Equal($"[{sandbox.Version}]", (string?)dependency.Attribute("version")));
		}
		Assert.Contains(nuspec.Descendants(), e => e.Name.LocalName == "repository" && (string?)e.Attribute("type") == "git");
	}

	[Theory]
	[InlineData("DimensionalQuantities")]
	[InlineData("DimensionalQuantities.Extensions.Metric")]
	[InlineData("DimensionalQuantities.Extensions.Imperial")]
	[InlineData("DimensionalQuantities.Extensions.Nautical")]
	[InlineData("DimensionalQuantities.Extensions.CgsEmag")]
	public async Task SymbolPackageContainsPortableSymbolsMatchingRuntimeBinary(string id)
	{
		using PackageSandbox sandbox = new();
		await sandbox.Pack(id);
		using ZipArchive archive = ZipFile.OpenRead(sandbox.ArchivePath(id));
		using ZipArchive symbols = ZipFile.OpenRead(sandbox.ArchivePath(id, symbols: true));
		AssertPortablePair(archive.GetEntry($"lib/netstandard2.0/{id}.dll")!, symbols.GetEntry($"lib/netstandard2.0/{id}.pdb")!);
	}

	[Theory]
	[InlineData("DimensionalQuantities")]
	[InlineData("DimensionalQuantities.Roslyn")]
	[InlineData("DimensionalQuantities.Roslyn.CodeFixes")]
	public async Task CorePackageShipsCompilerAssetsWithMatchingPortableSymbols(string name)
	{
		using PackageSandbox sandbox = new();
		await sandbox.Pack();
		using ZipArchive archive = ZipFile.OpenRead(sandbox.ArchivePath("DimensionalQuantities"));
		AssertPortablePair(archive.GetEntry($"analyzers/dotnet/cs/{name}.dll")!, archive.GetEntry($"analyzers/dotnet/cs/{name}.pdb")!);
	}

	private static void AssertPortablePair(ZipArchiveEntry dll, ZipArchiveEntry pdb)
	{
		Assert.Multiple(
			() => Assert.NotNull(dll),
			() => Assert.NotNull(pdb));
		using MemoryStream binary = new();
		using (Stream stream = dll.Open())
		{
			stream.CopyTo(binary);
		}

		binary.Position = 0;
		using PEReader pe = new(binary);
		using MemoryStream symbolStream = new();
		using (Stream stream = pdb.Open())
		{
			stream.CopyTo(symbolStream);
		}

		symbolStream.Position = 0;
		using MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(symbolStream);
		MetadataReader metadata = provider.GetMetadataReader();
		CodeViewDebugDirectoryData codeView = pe.ReadCodeViewDebugDirectoryData(Assert.Single(pe.ReadDebugDirectory(), entry => entry.Type == DebugDirectoryEntryType.CodeView));
		Assert.Multiple(
			() => Assert.Equal(new Guid(metadata.DebugMetadataHeader!.Id.Take(16).ToArray()), codeView.Guid),
			() => Assert.Equal(BitConverter.ToUInt32(metadata.DebugMetadataHeader!.Id.AsSpan()[16..]),
				Assert.Single(pe.ReadDebugDirectory(), entry => entry.Type == DebugDirectoryEntryType.CodeView).Stamp),
			() => Assert.NotEmpty(metadata.Documents),
			() => Assert.All(metadata.Documents, handle =>
			{
				Document document = metadata.GetDocument(handle);
				Assert.Multiple(
					() => Assert.NotEmpty(metadata.GetString(document.Name)),
					() => Assert.NotEmpty(metadata.GetBlobBytes(document.Hash)),
					() => Assert.NotEqual(Guid.Empty, metadata.GetGuid(document.HashAlgorithm)));
			}));
	}
}
