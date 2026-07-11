#!/usr/bin/env python3
"""Detect projects with modified .fs files relative to the dev branch.

Writes results to tmp/detect-changes.txt.
No console output. No timeout on subprocess calls.
"""

import subprocess
import os
import sys
from pathlib import Path

OUTPUT_FILE = "tmp/detect-changes.txt"

FS_PROJ_NAMES = [
    "src/FLPQ.Cli/FLPQ.Cli.fsproj",
    "src/FLPQ.GraphAnalysis/FLPQ.GraphAnalysis.fsproj",
    "src/FLPQ.Languages/FLPQ.Languages.fsproj",
    "src/FLPQ.LinearAlgebra/FLPQ.LinearAlgebra.fsproj",
    "src/FLPQ.Printers/FLPQ.Printers.fsproj",
    "src/FLPQ.RPQ/FLPQ.RPQ.fsproj",
    "tests/FLPQ.Cli.Tests/FLPQ.Cli.Tests.fsproj",
    "tests/FLPQ.GraphAnalysis.Tests/FLPQ.GraphAnalysis.Tests.fsproj",
    "tests/FLPQ.Languages.Tests/FLPQ.Languages.Tests.fsproj",
    "tests/FLPQ.LinearAlgebra.Tests/FLPQ.LinearAlgebra.Tests.fsproj",
    "tests/FLPQ.Printers.Tests/FLPQ.Printers.Tests.fsproj",
    "tests/FLPQ.RPQ.Tests/FLPQ.RPQ.Tests.fsproj",
    "tests/FLPQ.TestUtilities/FLPQ.TestUtilities.fsproj",
]


def find_project_for_file(fs_file: str) -> str | None:
    """Map a changed .fs file to its .fsproj by finding the first matching
    parent directory that contains a .fsproj file."""
    parts = Path(fs_file).parts
    for i in range(len(parts)):
        candidate = Path(*parts[: i + 1])
        for proj in FS_PROJ_NAMES:
            proj_path = Path(proj)
            if str(candidate) == str(proj_path.parent) or candidate == proj_path:
                return proj
    return None


def main() -> None:
    os.makedirs("tmp", exist_ok=True)

    lines: list[str] = []
    lines.append("DETECT CHANGES SUMMARY")
    lines.append("")

    # Check git branch
    try:
        branch_result = subprocess.run(
            ["git", "branch", "--show-current"],
            capture_output=True, text=True, timeout=None
        )
        branch = branch_result.stdout.strip()
    except Exception:
        branch = "unknown"

    # Get changed .fs files relative to dev
    try:
        result = subprocess.run(
            ["git", "diff", "--name-only", "dev", "--", "*.fs"],
            capture_output=True, text=True, timeout=None
        )
        changed_files = [f for f in result.stdout.strip().split("\n") if f]
    except Exception as e:
        lines.append(f"ERROR: git diff failed: {e}")
        lines.append("STATUS: ERROR")
        lines.append("")
        lines.append("--- DETAILED LOG ---")
        with open(OUTPUT_FILE, "w") as f:
            f.write("\n".join(lines))
        sys.exit(1)

    if not changed_files:
        lines.append("No modified .fs files detected relative to dev.")
        lines.append("STATUS: CLEAN")
        lines.append("")
        lines.append("--- DETAILED LOG ---")
        lines.append(f"Branch: {branch}")
        with open(OUTPUT_FILE, "w") as f:
            f.write("\n".join(lines))
        sys.exit(0)

    # Map files to projects
    project_set: set[str] = set()
    file_to_project: dict[str, str] = {}
    unmapped: list[str] = []

    for fs_file in changed_files:
        proj = find_project_for_file(fs_file)
        if proj:
            project_set.add(proj)
            file_to_project[fs_file] = proj
        else:
            unmapped.append(fs_file)

    lines.append(f"Branch: {branch}")
    lines.append(f"Found {len(changed_files)} modified .fs file(s) in {len(project_set)} project(s):")
    lines.append("")

    for proj in sorted(project_set):
        lines.append(f"  {proj}")

    if unmapped:
        lines.append("")
        lines.append(f"WARNING: {len(unmapped)} file(s) could not be mapped to a project:")
        for uf in unmapped:
            lines.append(f"  {uf}")

    lines.append("")
    lines.append("STATUS: MODIFIED")
    lines.append("")
    lines.append("--- DETAILED LOG ---")
    lines.append("Changed files (file -> project mapping):")
    for fs_file in sorted(changed_files):
        proj = file_to_project.get(fs_file, "UNMAPPED")
        lines.append(f"  {fs_file} -> {proj}")

    with open(OUTPUT_FILE, "w") as f:
        f.write("\n".join(lines))

    sys.exit(0)


if __name__ == "__main__":
    main()
