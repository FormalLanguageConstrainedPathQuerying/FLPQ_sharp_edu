#!/usr/bin/env python3
"""Hard gate quality check: format + build + tests (per project) + coverage + lint on changed projects.

Sequence:
  1. Format: dotnet fantomas . --check
  2. Build: dotnet build FLPQ.slnx -c Debug
  3. Tests: dotnet test per project with per-project coverage collection, then merge
  4. Coverage gate: per-project >= 75% line, total >= 80% line
  5. Lint: dotnet-fsharplint lint on changed projects only

Writes results to tmp/hard-gate.txt.
No console output. No timeout on subprocess calls.
"""

import os
import re
import subprocess
import sys
import time
import traceback
import xml.etree.ElementTree as ET
from datetime import datetime

from common import (
    run_cmd,
    find_fsproj_paths,
    find_project_for_file,
    find_source_packages,
    find_test_packages,
    ensure_output_dir,
    remove_output_file,
    write_output_file,
)

OUTPUT_FILE = "tmp/hard-gate.txt"
SOLUTION = "FLPQ.slnx"

PER_PROJECT_THRESHOLD = 85.0
TOTAL_THRESHOLD = 90.0


def _now() -> str:
    return datetime.now().strftime("%H:%M:%S")

def flush_log(lines: list[str], detailed_logs: list[str], status: str = "IN_PROGRESS") -> None:
    output = lines.copy()
    output.append("")
    output.append(f"STATUS: {status}")
    output.append("")
    output.append("--- DETAILED LOG ---")
    output.append("")
    for log in detailed_logs:
        output.append(log)
        output.append("")
    if status == "BLOCKED":
        output.append("HARD GATE FAILED. Exit code 1. DO NOT MERGE. Resolve ALL failures and re-run.")
        output.append("")
    elif status == "IN_PROGRESS":
        output.append("HARD GATE IN PROGRESS. DO NOT MERGE. Await completion.")
        output.append("")
    write_output_file(OUTPUT_FILE, output)


def detect_changed_projects() -> list[str]:
    """Return list of changed .fsproj paths relative to dev, or empty list if none."""
    try:
        result = subprocess.run(
            ["git", "diff", "--name-only", "dev", "--", "*.fs"],
            capture_output=True,
            text=True,
            timeout=None,
        )
        changed_files = [f for f in result.stdout.strip().split("\n") if f]
    except Exception:
        return []

    if not changed_files:
        return []

    all_projects = find_fsproj_paths()
    projects: set[str] = set()
    for fs_file in changed_files:
        proj = find_project_for_file(fs_file, all_projects)
        if proj:
            projects.add(proj)
    return sorted(projects)


def run_coverage_gate() -> tuple[list[str], list[str], bool]:
    """Parse coverage data and check thresholds.
    Returns (per_project_lines, under_threshold_list, total_pass).
    """
    source_packages = find_source_packages()
    per_project_lines: list[str] = []
    under_threshold: list[str] = []

    if not os.path.exists("tmp/coverage.cobertura"):
        per_project_lines.append("  ERROR: coverage.cobertura not found")
        return per_project_lines, ["NO_DATA"], False

    try:
        tree = ET.parse("tmp/coverage.cobertura")
        root = tree.getroot()
    except ET.ParseError as e:
        per_project_lines.append(f"  ERROR: corrupt coverage data ({e})")
        try:
            size = os.path.getsize("tmp/coverage.cobertura")
            per_project_lines.append(f"  coverage.cobertura size: {size} bytes")
        except Exception:
            pass
        return per_project_lines, ["PARSE_ERROR"], False

    total_covered = 0
    total_valid = 0
    all_ok = True

    for pkg in root.findall(".//package"):
        name = pkg.attrib.get("name", "")
        if name not in source_packages:
            continue

        pkg_covered = 0
        pkg_valid = 0
        for cls in pkg.findall(".//class"):
            for line in cls.findall(".//lines/line"):
                if int(line.attrib.get("hits", 0)) > 0:
                    pkg_covered += 1
                pkg_valid += 1

        total_covered += pkg_covered
        total_valid += pkg_valid

        if pkg_valid > 0:
            pct = pkg_covered / pkg_valid * 100
        else:
            pct = 0.0

        status = (
            "PASS"
            if pct >= PER_PROJECT_THRESHOLD
            else f"BLOCKED (below {PER_PROJECT_THRESHOLD:.0f}%)"
        )
        if pct < PER_PROJECT_THRESHOLD:
            all_ok = False
            under_threshold.append(name)

        per_project_lines.append(
            f"  {name}: {pct:.1f}% ({pkg_covered}/{pkg_valid}) — {status}"
        )

    if total_valid > 0:
        total_pct = total_covered / total_valid * 100
    else:
        total_pct = 0.0

    total_status = (
        "PASS"
        if total_pct >= TOTAL_THRESHOLD
        else f"BLOCKED (below {TOTAL_THRESHOLD:.0f}%)"
    )
    if total_pct < TOTAL_THRESHOLD:
        all_ok = False

    per_project_lines.append(
        f"  TOTAL: {total_pct:.1f}% ({total_covered}/{total_valid}) "
        f"(threshold {TOTAL_THRESHOLD:.0f}%) — {total_status}"
    )

    return per_project_lines, under_threshold, all_ok


def parse_test_output(test_output: str) -> tuple[int, int]:
    failed_count = 0
    skipped_count = 0
    for line in test_output.split("\n"):
        if "Failed:" in line or "Failed!" in line:
            m = re.search(r"Failed[:\!]\s*(\d+)", line)
            if m:
                failed_count = max(failed_count, int(m.group(1)))
        if "Skipped:" in line:
            m = re.search(r"Skipped:\s*(\d+)", line)
            if m:
                skipped_count = max(skipped_count, int(m.group(1)))
    return failed_count, skipped_count


def run_tests_per_project(
    lines: list[str],
    detailed_logs: list[str],
    next_step,
    total_steps: int,
    test_start_step: int,
) -> bool:
    """Run dotnet test with coverage per project, flushing progress after each project.
    Returns True if all projects pass (0 failed, 0 skipped)."""
    test_packages = find_test_packages()
    all_projects = find_fsproj_paths()
    cov_files: list[str] = []
    all_ok = True

    detailed_logs.append("--- STEP 3: TESTS (per project) ---")
    test_end_step = test_start_step + len(test_packages) - 1
    lines.append(f"Step {test_start_step}-{test_end_step}/{total_steps} (Tests):")

    for pkg_name in test_packages:
        proj_path = all_projects[pkg_name]
        cov_file = f"tmp/coverage_{pkg_name}.cobertura"
        cov_files.append(cov_file)

        s = next_step()
        starting_line = f"[{_now()}] Step {s}/{total_steps} {pkg_name}: starting..."
        lines.append(starting_line)
        detailed_logs.append(starting_line)
        flush_log(lines, detailed_logs)

        test_rc, test_stdout, test_stderr = run_cmd(
            [
                "dotnet",
                "dotnet-coverage",
                "collect",
                "-o",
                cov_file,
                "-f",
                "cobertura",
                "--",
                "dotnet",
                "test",
                proj_path,
                "--nologo",
            ]
        )
        test_output = test_stdout + test_stderr
        failed, skipped = parse_test_output(test_output)

        if test_rc != 0 or failed > 0 or skipped > 0:
            all_ok = False
            result_line = f"[{_now()}] Step {s}/{total_steps} {pkg_name}: FAILED ({failed} failed, {skipped} skipped)"
        else:
            result_line = f"[{_now()}] Step {s}/{total_steps} {pkg_name}: OK (0 failed, 0 skipped)"

        lines.append(result_line)
        detailed_logs.append(result_line)
        flush_log(lines, detailed_logs)

    if cov_files:
        run_cmd(
            ["dotnet", "dotnet-coverage", "merge", *cov_files,
             "-o", "tmp/coverage.cobertura", "-f", "cobertura"]
        )
        for f in cov_files:
            remove_output_file(f)

    if all_ok:
        lines.append("  Test gate: PASS")
    else:
        lines.append("  Test gate: BLOCKED")

    return all_ok


def main() -> None:
    ensure_output_dir("tmp")
    remove_output_file(OUTPUT_FILE)

    lines: list[str] = []
    lines.append("HARD GATE SUMMARY")
    lines.append(f"PID: {os.getpid()}  Started: {datetime.now().strftime('%H:%M:%S')}")
    lines.append("")
    detailed_logs: list[str] = []
    overall_pass = True

    flush_log(lines, detailed_logs)

    # --- Step 0: Detect changed projects (before format, to avoid false positives) ---
    changed_projects = detect_changed_projects()

    test_projects = find_test_packages()
    total_steps = 1 + 1 + len(test_projects) + 1 + len(changed_projects)
    current_step = 0

    def next_step() -> int:
        nonlocal current_step
        current_step += 1
        return current_step

    # --- Step 1: Format ---
    detailed_logs.append(f"[{datetime.now().strftime('%H:%M:%S')}] Step 1 (Format) started")
    flush_log(lines, detailed_logs)
    fmt_rc, fmt_stdout, fmt_stderr = run_cmd(["dotnet", "fantomas", ".", "--check"])
    detailed_logs.append("--- STEP 1: FORMAT (dotnet fantomas . --check) ---")
    detailed_logs.append(fmt_stdout.strip() if fmt_stdout else "(no output)")
    if fmt_stderr.strip():
        detailed_logs.append(fmt_stderr.strip())
    s = next_step()
    if fmt_rc == 0:
        lines.append(f"[{_now()}] Step {s}/{total_steps} (Format): OK")
    else:
        lines.append(f"[{_now()}] Step {s}/{total_steps} (Format): BLOCKED (files need formatting, exit code {fmt_rc})")
        overall_pass = False

    flush_log(lines, detailed_logs)

    # --- Step 2: Build ---
    detailed_logs.append(f"[{datetime.now().strftime('%H:%M:%S')}] Step 2 (Build) started")
    flush_log(lines, detailed_logs)
    build_rc, build_stdout, build_stderr = run_cmd(
        ["dotnet", "build", SOLUTION, "-c", "Debug"]
    )
    build_output = build_stdout + build_stderr
    detailed_logs.append("--- STEP 2: BUILD (dotnet build) ---")
    detailed_logs.append(build_output.strip())

    build_ok = build_rc == 0 and "Build succeeded" in build_output
    s = next_step()
    if build_ok:
        lines.append(f"[{_now()}] Step {s}/{total_steps} (Build): OK (Build succeeded)")
    else:
        lines.append(f"[{_now()}] Step {s}/{total_steps} (Build): FAILED")
        overall_pass = False

    flush_log(lines, detailed_logs)

    if not build_ok:
        flush_log(lines, detailed_logs, "BLOCKED")
        sys.exit(1)

    # --- Step 3: Tests (per project) ---
    test_start_step = current_step + 1
    detailed_logs.append(f"[{datetime.now().strftime('%H:%M:%S')}] Step 3 (Tests) started")
    flush_log(lines, detailed_logs)
    test_all_ok = run_tests_per_project(
        lines, detailed_logs, next_step, total_steps, test_start_step
    )
    if not test_all_ok:
        overall_pass = False

    flush_log(lines, detailed_logs)

    # --- Step 4: Coverage Gate ---
    detailed_logs.append(f"[{datetime.now().strftime('%H:%M:%S')}] Step 4 (Coverage) started")
    flush_log(lines, detailed_logs)
    cov_lines, _under_threshold, cov_ok = run_coverage_gate()
    detailed_logs.append("--- STEP 4: COVERAGE GATE ---")
    for cl in cov_lines:
        detailed_logs.append(cl)
    s = next_step()
    lines.append(f"[{_now()}] Step {s}/{total_steps} (Coverage):")
    for cl in cov_lines:
        lines.append(cl)
    if cov_ok:
        lines.append("  Coverage gate: PASS")
    else:
        lines.append("  Coverage gate: BLOCKED")
        overall_pass = False

    flush_log(lines, detailed_logs)

    # --- Step 5: Lint on changed projects ---
    detailed_logs.append("--- STEP 5: LINT (on changed projects) ---")
    detailed_logs.append(f"[{datetime.now().strftime('%H:%M:%S')}] Step 5 (Lint) started")
    flush_log(lines, detailed_logs)

    if not changed_projects:
        lines.append(f"[{_now()}] Lint: SKIP (no changed .fs files)")
        detailed_logs.append("(no changed .fs files — lint skipped)")
    else:
        lint_start = current_step + 1
        lint_end = current_step + len(changed_projects)
        lines.append(f"[{_now()}] Step {lint_start}-{lint_end}/{total_steps} (Lint):")
        lint_all_ok = True

        lint_env = os.environ.copy()
        lint_env["DOTNET_ROOT"] = "/usr/lib/dotnet"
        dotnet_exe = "/usr/lib/dotnet/dotnet"

        for proj in changed_projects:
            lint_rc = -1
            lint_output = ""
            try:
                lint_result = subprocess.run(
                    [dotnet_exe, "fsharplint", "lint", proj],
                    capture_output=True,
                    text=True,
                    timeout=None,
                    env=lint_env,
                )
                lint_rc = lint_result.returncode
                lint_output = lint_result.stdout + lint_result.stderr
            except Exception as e:
                lint_output = f"ERROR running fsharplint: {e}"
                lint_rc = -1

            detailed_logs.append(f"--- LINT: {proj} ---")
            detailed_logs.append(lint_output.strip())

            s = next_step()
            if lint_rc != 0:
                lint_all_ok = False
                overall_pass = False
                if "ERROR running fsharplint" in lint_output:
                    lint_line = f"[{_now()}] Step {s}/{total_steps} {proj}: TOOL FAILED — see detailed log"
                else:
                    summary_match = re.search(r"Summary:\s*(\d+)\s+warnings?", lint_output)
                    warn_count = int(summary_match.group(1)) if summary_match else 0
                    if warn_count > 0:
                        lint_line = f"[{_now()}] Step {s}/{total_steps} {proj}: {warn_count} warnings — BLOCKED"
                    else:
                        lint_line = f"[{_now()}] Step {s}/{total_steps} {proj}: TOOL FAILED (exit code {lint_rc})"
            else:
                warning_match = re.search(r"(\d+) warnings?", lint_output)
                warn_count = int(warning_match.group(1)) if warning_match else 0
                if warn_count > 0:
                    lint_all_ok = False
                    overall_pass = False
                    lint_line = f"[{_now()}] Step {s}/{total_steps} {proj}: {warn_count} warnings — BLOCKED"
                else:
                    lint_line = f"[{_now()}] Step {s}/{total_steps} {proj}: 0 warnings — PASS"

            lines.append(lint_line)
            detailed_logs.append(lint_line)
            flush_log(lines, detailed_logs)

        if lint_all_ok:
            lines.append("  Lint gate: PASS")
        else:
            lines.append("  Lint gate: BLOCKED")

    if overall_pass:
        flush_log(lines, detailed_logs, "PASS")
    else:
        flush_log(lines, detailed_logs, "BLOCKED")

    sys.exit(0 if overall_pass else 1)


if __name__ == "__main__":
    try:
        main()
    except Exception:
        crash = traceback.format_exc()
        try:
            with open(OUTPUT_FILE, "a") as f:
                f.write(f"\n--- CRASH TRACEBACK ---\n{crash}\nSTATUS: BLOCKED\n")
        except Exception:
            pass
        sys.stderr.write(f"Hard gate crashed:\n{crash}")
        sys.stderr.flush()
        sys.exit(1)
