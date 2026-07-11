#!/usr/bin/env python3
"""Hard gate quality check: format + build + tests with coverage + lint on changed projects.

Sequence:
  1. Format: dotnet fantomas .
  2. Build: dotnet build FLPQ.slnx -c Debug
  3. Tests + Coverage: dotnet dotnet-coverage collect dotnet test FLPQ.slnx ...
  4. Coverage gate: per-project ≥75% line, total ≥80% line
  5. Lint: dotnet-fsharplint lint on changed projects only

Writes results to tmp/hard-gate.txt.
No console output. No timeout on subprocess calls.
"""

import subprocess
import os
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

OUTPUT_FILE = "tmp/hard-gate.txt"
SOLUTION = "FLPQ.slnx"

# Source packages (exclude *.Tests and TestUtilities)
SOURCE_PACKAGES = [
    "FLPQ.Cli",
    "FLPQ.GraphAnalysis",
    "FLPQ.Languages",
    "FLPQ.LinearAlgebra",
    "FLPQ.Printers",
    "FLPQ.RPQ",
]

PER_PROJECT_THRESHOLD = 75.0
TOTAL_THRESHOLD = 80.0

# FS project paths for lint detection
FS_PROJ_PATHS = {
    "FLPQ.Cli": "src/FLPQ.Cli/FLPQ.Cli.fsproj",
    "FLPQ.GraphAnalysis": "src/FLPQ.GraphAnalysis/FLPQ.GraphAnalysis.fsproj",
    "FLPQ.Languages": "src/FLPQ.Languages/FLPQ.Languages.fsproj",
    "FLPQ.LinearAlgebra": "src/FLPQ.LinearAlgebra/FLPQ.LinearAlgebra.fsproj",
    "FLPQ.Printers": "src/FLPQ.Printers/FLPQ.Printers.fsproj",
    "FLPQ.RPQ": "src/FLPQ.RPQ/FLPQ.RPQ.fsproj",
    "FLPQ.Cli.Tests": "tests/FLPQ.Cli.Tests/FLPQ.Cli.Tests.fsproj",
    "FLPQ.GraphAnalysis.Tests": "tests/FLPQ.GraphAnalysis.Tests/FLPQ.GraphAnalysis.Tests.fsproj",
    "FLPQ.Languages.Tests": "tests/FLPQ.Languages.Tests/FLPQ.Languages.Tests.fsproj",
    "FLPQ.LinearAlgebra.Tests": "tests/FLPQ.LinearAlgebra.Tests/FLPQ.LinearAlgebra.Tests.fsproj",
    "FLPQ.Printers.Tests": "tests/FLPQ.Printers.Tests/FLPQ.Printers.Tests.fsproj",
    "FLPQ.RPQ.Tests": "tests/FLPQ.RPQ.Tests/FLPQ.RPQ.Tests.fsproj",
    "FLPQ.TestUtilities": "tests/FLPQ.TestUtilities/FLPQ.TestUtilities.fsproj",
}


def run_cmd(cmd: list[str]) -> tuple[int, str, str]:
    """Run a command without timeout. Returns (exit_code, stdout, stderr)."""
    try:
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=None)
        return result.returncode, result.stdout, result.stderr
    except Exception as e:
        return -1, "", f"ERROR: {e}"


def get_dotnet_root() -> str:
    """Get DOTNET_ROOT from environment or fall back to /usr/lib/dotnet."""
    return os.environ.get("DOTNET_ROOT", "/usr/lib/dotnet")


def detect_changed_projects() -> list[str]:
    """Return list of changed .fsproj paths relative to dev, or empty list if none."""
    try:
        result = subprocess.run(
            ["git", "diff", "--name-only", "dev", "--", "*.fs"],
            capture_output=True, text=True, timeout=None
        )
        changed_files = [f for f in result.stdout.strip().split("\n") if f]
    except Exception:
        return []

    if not changed_files:
        return []

    projects: set[str] = set()
    for fs_file in changed_files:
        parts = Path(fs_file).parts
        for i in range(len(parts)):
            candidate = Path(*parts[: i + 1])
            for pkg_name, proj_path in FS_PROJ_PATHS.items():
                proj = Path(proj_path)
                if str(candidate) == str(proj.parent):
                    projects.add(proj_path)
    return sorted(projects)


def run_coverage_gate() -> tuple[list[str], list[str], bool]:
    """Parse coverage data and check thresholds.
    Returns (per_project_lines, under_threshold_list, total_pass).
    """
    per_project_lines: list[str] = []
    under_threshold: list[str] = []

    if not os.path.exists("tmp/coverage.cobertura"):
        per_project_lines.append("  ERROR: coverage.cobertura not found")
        return per_project_lines, ["NO_DATA"], False

    try:
        tree = ET.parse("tmp/coverage.cobertura")
        root = tree.getroot()
    except Exception as e:
        per_project_lines.append(f"  ERROR parsing coverage data: {e}")
        return per_project_lines, ["PARSE_ERROR"], False

    total_covered = 0
    total_valid = 0
    all_ok = True

    for pkg in root.findall(".//package"):
        name = pkg.attrib.get("name", "")
        if name not in SOURCE_PACKAGES:
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

        status = "PASS" if pct >= PER_PROJECT_THRESHOLD else f"BLOCKED (below {PER_PROJECT_THRESHOLD:.0f}%)"
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

    total_status = "PASS" if total_pct >= TOTAL_THRESHOLD else f"BLOCKED (below {TOTAL_THRESHOLD:.0f}%)"
    if total_pct < TOTAL_THRESHOLD:
        all_ok = False

    per_project_lines.append(
        f"  TOTAL: {total_pct:.1f}% ({total_covered}/{total_valid}) "
        f"(threshold {TOTAL_THRESHOLD:.0f}%) — {total_status}"
    )

    return per_project_lines, under_threshold, all_ok


def main() -> None:
    os.makedirs("tmp", exist_ok=True)

    lines: list[str] = []
    lines.append("HARD GATE SUMMARY")
    lines.append("")

    dotnet_root = get_dotnet_root()
    detailed_logs: list[str] = []
    overall_pass = True

    # --- Step 0: Detect changed projects (before format, to avoid false positives) ---
    changed_projects = detect_changed_projects()

    # --- Step 1: Format ---
    fmt_rc, fmt_stdout, fmt_stderr = run_cmd(["dotnet", "fantomas", "."])
    detailed_logs.append("--- STEP 1: FORMAT (dotnet fantomas .) ---")
    detailed_logs.append(fmt_stdout.strip() if fmt_stdout else "(no output)")
    if fmt_stderr.strip():
        detailed_logs.append(fmt_stderr.strip())
    if fmt_rc == 0:
        lines.append("Step 1 (Format): OK")
    else:
        lines.append(f"Step 1 (Format): FAILED (exit code {fmt_rc})")
        overall_pass = False
        lines.append("STATUS: BLOCKED (format failed)")
        lines.append("")
        lines.append("--- DETAILED LOG ---")
        lines.append("")
        for log in detailed_logs:
            lines.append(log)
            lines.append("")
        with open(OUTPUT_FILE, "w") as f:
            f.write("\n".join(lines))
        sys.exit(1)

    # --- Step 2: Build ---
    build_rc, build_stdout, build_stderr = run_cmd(
        ["dotnet", "build", SOLUTION, "-c", "Debug"]
    )
    build_output = build_stdout + build_stderr
    detailed_logs.append("--- STEP 2: BUILD (dotnet build) ---")
    detailed_logs.append(build_output.strip())

    build_ok = build_rc == 0 and "Build succeeded" in build_output
    if build_ok:
        lines.append("Step 2 (Build): OK (Build succeeded)")
    else:
        lines.append("Step 2 (Build): FAILED")
        overall_pass = False
        lines.append("STATUS: BLOCKED (build failed)")
        lines.append("")
        lines.append("--- DETAILED LOG ---")
        lines.append("")
        for log in detailed_logs:
            lines.append(log)
            lines.append("")
        with open(OUTPUT_FILE, "w") as f:
            f.write("\n".join(lines))
        sys.exit(1)

    # --- Step 3: Tests + Coverage ---
    test_rc, test_stdout, test_stderr = run_cmd([
        "dotnet", "dotnet-coverage", "collect",
        "dotnet", "test", SOLUTION,
        "-o", "tmp/coverage.cobertura",
        "-f", "cobertura",
        "--nologo",
    ])
    test_output = test_stdout + test_stderr
    detailed_logs.append("--- STEP 3: TESTS WITH COVERAGE ---")
    detailed_logs.append(test_output.strip())

    # Check test results
    failed_count = 0
    skipped_count = 0
    for line in test_output.split("\n"):
        if "Failed:" in line or "Failed!" in line:
            import re
            m = re.search(r"Failed[:\!]\s*(\d+)", line)
            if m:
                # take the last occurrence (total line)
                failed_count = max(failed_count, int(m.group(1)))
        if "Skipped:" in line:
            import re
            m = re.search(r"Skipped:\s*(\d+)", line)
            if m:
                skipped_count = max(skipped_count, int(m.group(1)))

    test_ok = failed_count == 0 and skipped_count == 0
    if test_ok:
        lines.append("Step 3 (Tests): OK (0 failed, 0 skipped)")
    else:
        lines.append(f"Step 3 (Tests): BLOCKED ({failed_count} failed, {skipped_count} skipped)")
        overall_pass = False

    # --- Step 4: Coverage Gate ---
    cov_lines, under_threshold, cov_ok = run_coverage_gate()
    detailed_logs.append("--- STEP 4: COVERAGE GATE ---")
    for cl in cov_lines:
        detailed_logs.append(cl)
    lines.append("Step 4 (Coverage):")
    for cl in cov_lines:
        lines.append(cl)
    if cov_ok:
        lines.append("  Coverage gate: PASS")
    else:
        lines.append("  Coverage gate: BLOCKED")
        overall_pass = False

    # --- Step 5: Lint on changed projects ---
    detailed_logs.append("--- STEP 5: LINT (on changed projects) ---")

    if not changed_projects:
        lines.append("Step 5 (Lint): SKIP (no changed .fs files)")
        detailed_logs.append("(no changed .fs files — lint skipped)")
    else:
        lint_all_ok = True
        per_project_lint: list[str] = []

        fsharplint_env = os.environ.copy()
        # Use system dotnet for runtime compatibility (fsharplint needs >=9.0)
        fsharplint_env["DOTNET_ROOT"] = "/usr/lib/dotnet"

        for proj in changed_projects:
            try:
                lint_result = subprocess.run(
                    ["dotnet", "fsharplint", "lint", proj],
                    capture_output=True, text=True, timeout=None,
                    env=fsharplint_env,
                )
                lint_output = lint_result.stdout + lint_result.stderr
            except Exception as e:
                lint_output = f"ERROR running fsharplint: {e}"

            detailed_logs.append(f"--- LINT: {proj} ---")
            detailed_logs.append(lint_output.strip())

            # Check for warnings
            import re
            warning_match = re.search(r"(\d+) warnings", lint_output)
            warn_count = int(warning_match.group(1)) if warning_match else 0
            if warn_count > 0:
                lint_all_ok = False
                per_project_lint.append(f"  {proj}: {warn_count} warnings — BLOCKED")
                overall_pass = False
            else:
                per_project_lint.append(f"  {proj}: 0 warnings — PASS")

        lines.append("Step 5 (Lint):")
        for pl in per_project_lint:
            lines.append(pl)
        if lint_all_ok:
            lines.append("  Lint gate: PASS")
        else:
            lines.append("  Lint gate: BLOCKED")

    # --- Final status ---
    lines.append("")
    if overall_pass:
        lines.append("STATUS: PASS")
        exit_code = 0
    else:
        lines.append("STATUS: BLOCKED")
        exit_code = 1

    lines.append("")
    lines.append("--- DETAILED LOG ---")
    lines.append("")
    for log in detailed_logs:
        lines.append(log)
        lines.append("")

    if not overall_pass:
        lines.append("HARD GATE FAILED. Exit code 1. DO NOT MERGE. Resolve ALL failures and re-run.")
        lines.append("")

    with open(OUTPUT_FILE, "w") as f:
        f.write("\n".join(lines))

    sys.exit(exit_code)


if __name__ == "__main__":
    main()
