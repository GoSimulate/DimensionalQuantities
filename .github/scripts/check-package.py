from pathlib import Path
import sys
import xml.etree.ElementTree as ET
import zipfile

version = sys.argv[1]
packages = list(Path("artifacts/packages").glob("*.nupkg"))
assert len(packages) == 1, packages
with zipfile.ZipFile(packages[0]) as package:
    names = package.namelist()
    assert "lib/net10.0/DimensionalQuantities.dll" in names
    assert "analyzers/dotnet/cs/DimensionalQuantities.Roslyn.dll" in names
    assert not any("Microsoft.CodeAnalysis" in name for name in names)
    specification = ET.fromstring(package.read("DimensionalQuantities.nuspec"))
    assert specification.find(".//{*}version").text == version
    dependencies = specification.findall(".//{*}dependency")
    assert not any("Roslyn" in item.attrib["id"] or "CodeAnalysis" in item.attrib["id"] for item in dependencies)
print(f"Verified {packages[0]}: runtime plus compiler tooling, version {version}")
