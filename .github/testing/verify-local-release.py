import json
import os
from pathlib import Path
import subprocess

expected = os.environ.get("DQ_EXPECTED_VERSION", "1.1.0")
if not expected:
    assert os.environ["RELEASE_PUBLISHED"] != "true"
    assert not Path("artifacts/local-release.json").exists()
    tags = subprocess.check_output(["git", "--git-dir=/tmp/dimensional-quantities-remote.git", "tag"], text=True)
    assert tags.strip() == "v1.0.0", tags
    print("Verified documentation-only commit creates no release")
else:
    assert os.environ["RELEASE_PUBLISHED"] == "true"
    assert os.environ["RELEASE_VERSION"] == expected
    release = json.loads(Path("artifacts/local-release.json").read_text())
    assert release["version"] == expected
    assert os.environ.get("DQ_EXPECTED_NOTES", "add dimensional release fixture") in release["notes"]
    assert subprocess.check_output(["git", "--git-dir=/tmp/dimensional-quantities-remote.git", "rev-parse", f"refs/tags/v{expected}"], text=True).strip()
    assert list(Path("artifacts/local-feed").glob(f"DimensionalQuantities.{expected}.nupkg"))
    print(f"Verified act release v{expected}: local tag, generated notes, versioned NuGet package")
