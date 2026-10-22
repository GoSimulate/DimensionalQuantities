"""Run real workflows against a disposable local Git repository; no credentials."""
import argparse
import json
from pathlib import Path
import shutil
import subprocess
import tempfile

parser = argparse.ArgumentParser()
parser.add_argument("--case", choices=("minor", "patch", "major", "none"), default="minor")
parser.add_argument("--workflow", choices=("ci", "release", "both"), default="both")
args = parser.parse_args()
root = Path(__file__).resolve().parents[2]
fixture = Path(tempfile.mkdtemp(prefix="dimensional-quantities-act-"))
shutil.copytree(root, fixture, dirs_exist_ok=True,
                ignore=shutil.ignore_patterns(".git", "bin", "obj", "artifacts", "node_modules", ".idea"))

def git(*arguments):
    subprocess.run(["git", *arguments], cwd=fixture, check=True, stdout=subprocess.DEVNULL)

git("init", "-b", "main")
git("config", "user.name", "Local workflow test")
git("config", "user.email", "local-test@example.invalid")
git("remote", "add", "origin", "https://github.com/local/DimensionalQuantities.git")
# Temporary tests ensure that all three v3 runners really discover and execute.
for project in (fixture / "src").glob("*Tests"):
    (project / "WorkflowFixtureTests.cs").write_text(
        'public class WorkflowFixtureTests { [Xunit.Fact] public void RunnerExecutes() '
        '{ Xunit.Assert.Equal(4, 2 + 2); } }\n')
git("add", ".")
git("commit", "-m", "chore: initialise workflow fixture")
git("tag", "v1.0.0")
message, expected, note = {
    "minor": ("feat: add dimensional release fixture", "1.1.0", "add dimensional release fixture"),
    "patch": ("fix: correct dimensional release fixture", "1.0.1", "correct dimensional release fixture"),
    "major": ("feat!: change dimensional encoding", "2.0.0", "change dimensional encoding"),
    "none": ("docs: explain dimensional quantities", "", ""),
}[args.case]
git("commit", "--allow-empty", "-m", message)
head = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=fixture, text=True).strip()
event = fixture / "event.json"
event.write_text(json.dumps({"ref": "refs/heads/main", "after": head,
    "repository": {"name": "DimensionalQuantities", "full_name": "local/DimensionalQuantities",
                   "default_branch": "main", "owner": {"login": "local", "name": "local"}},
    "pull_request": {"number": 1, "title": message, "head": {"ref": "fixture", "sha": head},
                     "base": {"ref": "main", "sha": head}}}))
base = ["act", "--directory", str(fixture), "--eventpath", str(event),
        "--secret-file", "/dev/null", "--var-file", "/dev/null", "--env-file", "/dev/null",
        "--platform", "blacksmith-2vcpu-ubuntu-2404=catthehacker/ubuntu:act-latest", "--pull=false",
        "--env", "ACT=true", "--env", "DQ_EXPECTED_VERSION=" + expected,
        "--env", "DQ_EXPECTED_NOTES=" + note]
print("Disposable checkout:", fixture, flush=True)
if args.workflow in ("ci", "both"):
    subprocess.run([*base, "pull_request", "-W", ".github/workflows/ci.yml"], cwd=fixture, check=True)
if args.workflow in ("release", "both"):
    subprocess.run([*base, "push", "-W", ".github/workflows/cd.yml"], cwd=fixture, check=True)
