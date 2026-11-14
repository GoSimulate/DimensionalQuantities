using System.Reflection;

namespace DimensionalQuantities.IntegrationTests.Packaging;

internal sealed class PackageAssemblyContext : System.Runtime.Loader.AssemblyLoadContext, IDisposable
{
	private readonly string _directory;

	public PackageAssemblyContext(string directory) : base(isCollectible: true)
	{
		_directory = directory;
	}

	protected override Assembly? Load(AssemblyName assemblyName)
	{
		string path = Path.Combine(_directory, assemblyName.Name + ".dll");
		return File.Exists(path) ? LoadFromAssemblyPath(path) : null;
	}

	public void Dispose() => Unload();
}
