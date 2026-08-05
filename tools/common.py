#!/usr/bin/env python3
"""Shared utilities for project tooling scripts.

Provides common functions: command execution, project discovery,
and output file management. No timeout on subprocess calls.
"""

import subprocess
import os
import tempfile
from pathlib import Path
from typing import Optional


def run_cmd(cmd: list[str]) -> tuple[int, str, str]:
    """Run a command without timeout. Returns (exit_code, stdout, stderr).

    Uses temp files instead of capture_output=True (pipes) to avoid
    deadlocks when the command spawns subprocesses that inherit pipe fds.
    """
    stdout_path: str = ""
    stderr_path: str = ""
    try:
        with (
            tempfile.NamedTemporaryFile(mode="w+", suffix=".stdout", delete=False) as stdout_f,
            tempfile.NamedTemporaryFile(mode="w+", suffix=".stderr", delete=False) as stderr_f,
        ):
            stdout_path = stdout_f.name
            stderr_path = stderr_f.name

        with open(stdout_path, "w") as stdout_f, open(stderr_path, "w") as stderr_f:
            result = subprocess.run(
                cmd,
                stdout=stdout_f,
                stderr=stderr_f,
                text=True,
                timeout=None,
            )

        with open(stdout_path, "r") as stdout_f:
            stdout_text = stdout_f.read()
        with open(stderr_path, "r") as stderr_f:
            stderr_text = stderr_f.read()

        os.unlink(stdout_path)
        os.unlink(stderr_path)

        return result.returncode, stdout_text, stderr_text
    except Exception as e:
        for p in [stdout_path, stderr_path]:
            try:
                os.unlink(p)
            except (OSError, NameError):
                pass
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
