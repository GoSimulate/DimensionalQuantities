from pathlib import Path
import os
import sys
import xml.etree.ElementTree as ET
import zipfile


version = sys.argv[1]
package_directory = Path("artifacts/packages")
core_id = "DimensionalQuantities"
metric_id = "DimensionalQuantities.Extensions.Metric"
expected_packages = {
    f"{core_id}.{version}.nupkg",
    f"{metric_id}.{version}.nupkg",
}
expected_symbols = {
    f"{core_id}.{version}.snupkg",
    f"{metric_id}.{version}.snupkg",
}
actual_packages = {package.name for package in package_directory.glob("*.nupkg")}
actual_symbols = {package.name for package in package_directory.glob("*.snupkg")}
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


core_files, core_metadata = read_package(core_id, "nupkg")
metric_files, metric_metadata = read_package(metric_id, "nupkg")
core_symbols, _ = read_package(core_id, "snupkg")
metric_symbols, _ = read_package(metric_id, "snupkg")

core_runtime = "lib/netstandard2.0/DimensionalQuantities.dll"
metric_runtime = "lib/netstandard2.0/DimensionalQuantities.Extensions.Metric.dll"
analyzer_directory = "analyzers/dotnet/cs/"
tooling = {
    f"{analyzer_directory}DimensionalQuantities.dll",
    f"{analyzer_directory}DimensionalQuantities.Roslyn.dll",
    f"{analyzer_directory}DimensionalQuantities.Roslyn.CodeFixes.dll",
}
assert core_runtime in core_files
assert tooling <= core_files, tooling - core_files
assert metric_runtime in metric_files
assert core_runtime not in metric_files
assert not any(name.startswith(analyzer_directory) for name in metric_files)

for names in (core_files, metric_files):
    assert not any("Microsoft.CodeAnalysis" in name for name in names)
    assert not any(".Tests" in name for name in names)

for metadata in (core_metadata, metric_metadata):
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
assert not any(item.attrib["id"] == metric_id for item in core_dependencies)
metric_dependencies = metric_metadata.findall(".//{*}dependency")
core_dependency = [item for item in metric_dependencies if item.attrib["id"] == core_id]
assert len(core_dependency) == 1, core_dependency
assert core_dependency[0].attrib["version"] == f"[{version}]", core_dependency[0].attrib
assert core_dependency[0].attrib.get("include", "All").lower() == "all", core_dependency[0].attrib
assert "analyzers" not in core_dependency[0].attrib.get("exclude", "").lower(), core_dependency[0].attrib

assert "lib/netstandard2.0/DimensionalQuantities.pdb" in core_symbols
assert "lib/netstandard2.0/DimensionalQuantities.Extensions.Metric.pdb" in metric_symbols
for assembly in tooling:
    assert assembly.removesuffix(".dll") + ".pdb" in core_symbols

print(f"Verified {core_id} and {metric_id} packages and symbols at {version}")
