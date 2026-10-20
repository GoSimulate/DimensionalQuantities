import os
import re
import sys

title = os.environ.get("PR_TITLE", "")
if not re.fullmatch(r"[a-z]+(?:\([^()\r\n]+\))?!?: \S[^\r\n]*", title):
    sys.exit("PR title must be a Conventional Commit, e.g. 'feat: add unit conversion' or 'fix: correct dimensions'.")
print("Conventional PR title accepted")
