#!/usr/bin/env python3
"""Inter-subtask quality check: format + build.

Runs dotnet fantomas . then dotnet build FLPQ.slnx.
Writes results to tmp/quality-check.txt.
No console output. No timeout on subprocess calls.
"""

import sys
from common import run_cmd, ensure_output_dir, remove_output_file, write_output_file

OUTPUT_FILE = "tmp/quality-check.txt"
SOLUTION = "FLPQ.slnx"


def main() -> None:
    ensure_output_dir("tmp")
    remove_output_file(OUTPUT_FILE)

    lines: list[str] = []
    lines.append("QUALITY CHECK SUMMARY")
    lines.append("")
    lines.append("Step 1: Format (dotnet fantomas .)")
    lines.append("Step 2: Build (dotnet build)")

    statuses: list[str] = []
    detailed_logs: list[str] = []

    # --- Step 1: Format ---
    fmt_rc, fmt_stdout, fmt_stderr = run_cmd(["dotnet", "fantomas", ".", "--check"])
    if fmt_rc == 0:
        lines.append("  Format: OK")
        statuses.append("PASS")
    else:
        lines.append(f"  Format: FAILED (exit code {fmt_rc} - files need formatting)")
        statuses.append("BLOCKED")

    detailed_logs.append("--- FORMAT (dotnet fantomas .) ---")
    if fmt_stdout:
        detailed_logs.append(fmt_stdout.strip())
    if fmt_stderr:
        detailed_logs.append(fmt_stderr.strip())
    if not fmt_stdout and not fmt_stderr:
        detailed_logs.append("(no output)")

    # --- Step 2: Build ---
    build_rc, build_stdout, build_stderr = run_cmd(
        ["dotnet", "build", SOLUTION, "-c", "Debug"]
    )
    build_output = build_stdout + build_stderr
    if build_rc == 0 and "Build succeeded" in build_output:
        lines.append("  Build: OK (Build succeeded)")
        statuses.append("PASS")
    else:
        error_lines = [l for l in build_output.split("\n") if "error " in l.lower()]
        lines.append(f"  Build: FAILED ({len(error_lines)} error(s))")
        statuses.append("BLOCKED")

    detailed_logs.append("--- BUILD (dotnet build) ---")
    detailed_logs.append(build_output.strip())

    # --- Final status ---
    if "BLOCKED" in statuses:
        lines.append("")
        lines.append("STATUS: BLOCKED")
        exit_code = 1
    else:
        lines.append("")
        lines.append("STATUS: PASS")
        exit_code = 0

    lines.append("")
    lines.append("--- DETAILED LOG ---")
    lines.append("")
    for log in detailed_logs:
        lines.append(log)
        lines.append("")

    write_output_file(OUTPUT_FILE, lines)
    sys.exit(exit_code)


if __name__ == "__main__":
    main()
