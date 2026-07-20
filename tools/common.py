#!/usr/bin/env python3
"""Shared utilities for project tooling scripts.

Provides common functions: command execution, project discovery,
and output file management. No timeout on subprocess calls.
"""

import subprocess
import os
from pathlib import Path
from typing import Optional


def run_cmd(cmd: list[str]) -> tuple[int, str, str]:
    """Run a command without timeout. Returns (exit_code, stdout, stderr)."""
    try:
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=None)
        return result.returncode, result.stdout, result.stderr
    except Exception as e:
        return -1, "", f"ERROR: {e}"


def find_fsproj_paths(root_dir: Optional[str] = None) -> dict[str, str]:
    """Scan for all .fsproj files under root_dir (default: current directory).
    Returns dict mapping project name (stem) to relative file path."""
    base = Path(root_dir) if root_dir else Path.cwd()
    projects: dict[str, str] = {}
    for fsproj in base.rglob("*.fsproj"):
        rel = fsproj.relative_to(base)
        projects[fsproj.stem] = str(rel)
    return projects


def find_source_packages(root_dir: Optional[str] = None) -> list[str]:
    """Return list of source project names (exclude *.Tests and TestUtilities)."""
    all_projects = find_fsproj_paths(root_dir)
    return sorted(
        name
        for name in all_projects
        if not name.endswith(".Tests") and name != "FLPQ.TestUtilities"
    )


def find_test_packages(root_dir: Optional[str] = None) -> list[str]:
    """Return list of test project names (end with .Tests, exclude TestUtilities)."""
    all_projects = find_fsproj_paths(root_dir)
    return sorted(
        name
        for name in all_projects
        if name.endswith(".Tests") and name != "FLPQ.TestUtilities"
    )


def find_project_for_file(fs_file: str, all_projects: dict[str, str]) -> str | None:
    """Map a changed .fs file to its .fsproj by finding the first matching
    parent directory that contains a .fsproj file."""
    parts = Path(fs_file).parts
    for i in range(len(parts)):
        candidate = Path(*parts[: i + 1])
        for _proj_name, proj_path in all_projects.items():
            proj_p = Path(proj_path)
            if str(candidate) == str(proj_p.parent):
                return proj_path
    return None


def ensure_output_dir(dirpath: str = "tmp") -> None:
    """Create output directory if it does not exist."""
    os.makedirs(dirpath, exist_ok=True)


def remove_output_file(filepath: str) -> None:
    """Remove output file if it exists, ensuring a clean start."""
    try:
        os.remove(filepath)
    except OSError:
        pass


def write_output_file(filepath: str, lines: list[str]) -> None:
    """Write lines to output file."""
    with open(filepath, "w") as f:
        f.write("\n".join(lines))
