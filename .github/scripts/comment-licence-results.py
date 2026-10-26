"""Post a licence finding from an unprivileged ORT run on its pull request."""

import html
import io
import json
import os
import re
from pathlib import Path
from urllib.parse import urlparse
from urllib.error import HTTPError
from urllib.request import HTTPRedirectHandler, Request, build_opener, urlopen
from zipfile import ZipFile


MARKER = "<!-- ort-licence-compliance -->"
API = "https://api.github.com"


def api(path, method="GET", body=None):
    data = json.dumps(body).encode() if body is not None else None
    request = Request(
        API + path,
        data=data,
        method=method,
        headers={
            "Authorization": f"Bearer {os.environ['GH_TOKEN']}",
            "Accept": "application/vnd.github+json",
            "X-GitHub-Api-Version": "2022-11-28",
            "Content-Type": "application/json",
        },
    )
    with urlopen(request) as response:
        return json.load(response)


def cell(value):
    content = html.escape(str(value).replace("|", "\\|").replace("\n", " ")[:300])
    return f"<code>{content}</code>"


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, request, fp, code, message, headers, new_url):
        return None


def result_files(repo, run_id):
    artifacts = api(f"/repos/{repo}/actions/runs/{run_id}/artifacts?per_page=100")["artifacts"]
    matches = [item for item in artifacts if item["name"] == "ort-licence-results" and not item["expired"]]
    if not matches:
        raise ValueError("ORT run has no results artifact")

    path = f"/repos/{repo}/actions/artifacts/{matches[0]['id']}/zip"
    request = Request(API + path, headers={"Authorization": f"Bearer {os.environ['GH_TOKEN']}"})
    try:
        build_opener(NoRedirect()).open(request)
    except HTTPError as error:
        if error.code != 302:
            raise
        location = error.headers["Location"]
    else:
        raise ValueError("GitHub did not redirect to the artifact archive")
    if urlparse(location).scheme != "https":
        raise ValueError("Artifact archive did not use HTTPS")

    with urlopen(location) as response:
        payload = response.read(20_000_001)
    if len(payload) > 20_000_000:
        raise ValueError("ORT result artifact is unexpectedly large")
    with ZipFile(io.BytesIO(payload)) as archive:
        return {
            name: archive.read(name)
            for name in ("pr-number", "evaluation-result.json")
            if name in archive.namelist() and archive.getinfo(name).file_size <= 10_000_000
        }


def failure_body(files, run_url):
    lines = [MARKER, "## Licence compliance failed", ""]
    if "evaluation-result.json" in files:
        try:
            violations = json.loads(files["evaluation-result.json"])["evaluator"]["violations"]
        except (ValueError, KeyError, TypeError):
            violations = None
        if violations is None:
            lines.append("ORT produced an unreadable evaluation result. Check the workflow log.")
        elif violations:
            lines.extend(["| Package | Licence | Finding |", "| --- | --- | --- |"])
            for violation in violations[:20]:
                lines.append(
                    f"| {cell(violation['pkg'])} | {cell(violation.get('license') or 'Unknown')} "
                    f"| {cell(violation['message'])} |"
                )
            if len(violations) > 20:
                lines.append(f"\n{len(violations) - 20} more findings are in the workflow result.")
        else:
            lines.append("ORT failed without a policy violation. Check the workflow log.")
    else:
        lines.append("ORT did not produce an evaluation result. Check the workflow log.")
    lines.extend(["", f"[View the ORT run]({run_url})"])
    return "\n".join(lines)


def main():
    event = json.loads(Path(os.environ["GITHUB_EVENT_PATH"]).read_text())
    run = event["workflow_run"]
    if run["event"] != "pull_request" or run["conclusion"] not in {"success", "failure"}:
        return

    repo = os.environ["GITHUB_REPOSITORY"]
    files = result_files(repo, run["id"])
    raw_number = files["pr-number"].decode().strip()
    if not re.fullmatch(r"[1-9][0-9]*", raw_number):
        raise ValueError("Invalid pull request number in ORT artifact")

    number = int(raw_number)
    associated_prs = run.get("pull_requests", [])
    if associated_prs and number not in {item["number"] for item in associated_prs}:
        raise ValueError("ORT artifact names a different pull request from the workflow run")
    pr = api(f"/repos/{repo}/pulls/{number}")
    if pr["base"]["repo"]["full_name"] != repo or run["head_sha"] not in {
        pr["head"]["sha"],
        pr["merge_commit_sha"],
    }:
        raise ValueError("ORT run does not match the pull request's current head")

    existing = None
    page = 1
    while True:
        comments = api(f"/repos/{repo}/issues/{number}/comments?per_page=100&page={page}")
        for comment in comments:
            if comment["user"]["login"] == "github-actions[bot]" and MARKER in comment["body"]:
                existing = comment
                break
        if existing or len(comments) < 100:
            break
        page += 1

    if run["conclusion"] == "failure":
        run_url = f"https://github.com/{repo}/actions/runs/{run['id']}"
        body = failure_body(files, run_url)
    elif existing:
        body = f"{MARKER}\n## Licence compliance passed\n\nThe current ORT run passed."
    else:
        return

    if existing:
        api(f"/repos/{repo}/issues/comments/{existing['id']}", "PATCH", {"body": body})
    else:
        api(f"/repos/{repo}/issues/{number}/comments", "POST", {"body": body})


if __name__ == "__main__":
    main()
