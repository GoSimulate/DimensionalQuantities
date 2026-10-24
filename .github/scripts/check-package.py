from pathlib import Path
import os
import sys
import xml.etree.ElementTree as ET
import zipfile


version = sys.argv[1]
package_directory = Path("artifacts/packages")
core_id = "DimensionalQuantities"
extension_ids = sorted(
    Path(project.attrib["Path"]).stem
    for project in ET.parse("src/DimensionalQuantities.slnx").iter("Project")
    if Path(project.attrib["Path"]).stem.startswith("DimensionalQuantities.Extensions.")
    and not Path(project.attrib["Path"]).stem.endswith(".Tests")
)
package_ids = [core_id, *extension_ids]
expected_packages = {f"{package_id}.{version}.nupkg" for package_id in package_ids}
expected_symbols = {f"{package_id}.{version}.snupkg" for package_id in package_ids}
actual_packages = {package.name for package in package_directory.glob(f"*.{version}.nupkg")}
actual_symbols = {package.name for package in package_directory.glob(f"*.{version}.snupkg")}
assert actual_packages == expected_packages, (actual_packages, expected_packages)
assert actual_symbols == expected_symbols, (actual_symbols, expected_symbols)


def read_package(package_id, extension):
    path = package_directory / f"{package_id}.{version}.{extension}"
    with zipfile.ZipFile(path) as package:
        names = set(package.namelist())
        specification = ET.fromstring(package.read(f"{package_id}.nuspec"))
    metadata = specification.find("{*}metadata")
    assert metadata is not None
    assert metadata.find("{*}id").text == package_id
    assert metadata.find("{*}version").text == version
    return names, metadata


packages = {package_id: read_package(package_id, "nupkg") for package_id in package_ids}
symbols = {package_id: read_package(package_id, "snupkg")[0] for package_id in package_ids}
core_files, core_metadata = packages[core_id]
core_symbols = symbols[core_id]

core_runtime = "lib/netstandard2.0/DimensionalQuantities.dll"
analyzer_directory = "analyzers/dotnet/cs/"
tooling = {
    f"{analyzer_directory}DimensionalQuantities.dll",
    f"{analyzer_directory}DimensionalQuantities.Roslyn.dll",
    f"{analyzer_directory}DimensionalQuantities.Roslyn.CodeFixes.dll",
}
assert core_runtime in core_files
assert tooling <= core_files, tooling - core_files
for extension_id in extension_ids:
    extension_files, _ = packages[extension_id]
    assert f"lib/netstandard2.0/{extension_id}.dll" in extension_files
    assert core_runtime not in extension_files
    assert not any(name.startswith(analyzer_directory) for name in extension_files)
    assert f"lib/netstandard2.0/{extension_id}.pdb" in symbols[extension_id]

for names, _ in packages.values():
    assert not any("Microsoft.CodeAnalysis" in name for name in names)
    assert not any(".Tests" in name for name in names)

for _, metadata in packages.values():
    repository = metadata.find("{*}repository")
    assert repository is not None
    assert repository.attrib["type"] == "git"
    assert repository.attrib["url"].removesuffix(".git") == "https://github.com/GoSimulate/DimensionalQuantities"
    commit = repository.attrib["commit"]
    assert len(commit) == 40 and all(character in "0123456789abcdef" for character in commit)
    if "GITHUB_SHA" in os.environ:
        assert commit == os.environ["GITHUB_SHA"], (commit, os.environ["GITHUB_SHA"])
    dependencies = metadata.findall(".//{*}dependency")
    assert not any("Roslyn" in item.attrib["id"] or "CodeAnalysis" in item.attrib["id"] for item in dependencies)

core_dependencies = core_metadata.findall(".//{*}dependency")
assert not any(item.attrib["id"] in extension_ids for item in core_dependencies)
for extension_id in extension_ids:
    _, metadata = packages[extension_id]
    dependencies = metadata.findall(".//{*}dependency")
    core_dependency = [item for item in dependencies if item.attrib["id"] == core_id]
    assert len(core_dependency) == 1, (extension_id, core_dependency)
    assert core_dependency[0].attrib["version"] == f"[{version}]", core_dependency[0].attrib
    assert core_dependency[0].attrib.get("include", "All").lower() == "all", core_dependency[0].attrib
    assert "analyzers" not in core_dependency[0].attrib.get("exclude", "").lower(), core_dependency[0].attrib

assert "lib/netstandard2.0/DimensionalQuantities.pdb" in core_symbols
for assembly in tooling:
    assert assembly.removesuffix(".dll") + ".pdb" in core_symbols

print(f"Verified {', '.join(package_ids)} packages and symbols at {version}")
