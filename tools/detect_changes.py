#!/usr/bin/env python3
"""Detect projects with modified .fs files relative to the dev branch.

Writes results to tmp/detect-changes.txt.
No console output. No timeout on subprocess calls.
"""

import sys

from common import (
    run_cmd,
    find_fsproj_paths,
    find_project_for_file,
    ensure_output_dir,
    remove_output_file,
    write_output_file,
)

OUTPUT_FILE = "tmp/detect-changes.txt"


def main() -> None:
    ensure_output_dir("tmp")
    remove_output_file(OUTPUT_FILE)

    lines: list[str] = []
    lines.append("DETECT CHANGES SUMMARY")
    lines.append("")

    # Check git branch
    branch_rc, branch_stdout, _ = run_cmd(["git", "branch", "--show-current"])
    branch = branch_stdout.strip() if branch_rc == 0 else "unknown"

    # Get changed .fs files relative to dev
    diff_rc, diff_stdout, diff_stderr = run_cmd(
        ["git", "diff", "--name-only", "dev", "--", "*.fs"]
    )
    if diff_rc != 0:
        lines.append(f"ERROR: git diff failed: {diff_stderr}")
        lines.append("STATUS: ERROR")
        lines.append("")
        lines.append("--- DETAILED LOG ---")
        write_output_file(OUTPUT_FILE, lines)
        sys.exit(1)

    changed_files = [f for f in diff_stdout.strip().split("\n") if f]

    if not changed_files:
        lines.append("No modified .fs files detected relative to dev.")
        lines.append("STATUS: CLEAN")
        lines.append("")
        lines.append("--- DETAILED LOG ---")
        lines.append(f"Branch: {branch}")
        write_output_file(OUTPUT_FILE, lines)
        sys.exit(0)

    # Map files to projects
    all_projects = find_fsproj_paths()
    project_set: set[str] = set()
    file_to_project: dict[str, str] = {}
    unmapped: list[str] = []

    for fs_file in changed_files:
        proj = find_project_for_file(fs_file, all_projects)
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

    write_output_file(OUTPUT_FILE, lines)
    sys.exit(0)


if __name__ == "__main__":
    main()
