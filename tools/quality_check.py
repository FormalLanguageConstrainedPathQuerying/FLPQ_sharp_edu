#!/usr/bin/env python3
"""Inter-subtask quality check: format + build.

Runs dotnet fantomas . then dotnet build FLPQ.slnx.
Writes results to tmp/quality-check.txt.
No console output. No timeout on subprocess calls.
"""

import subprocess
import os
import sys

OUTPUT_FILE = "tmp/quality-check.txt"
SOLUTION = "FLPQ.slnx"


def run_cmd(cmd: list[str], label: str) -> tuple[int, str, str]:
    """Run a command without timeout. Returns (exit_code, stdout, stderr)."""
    try:
        result = subprocess.run(
            cmd, capture_output=True, text=True, timeout=None
        )
        return result.returncode, result.stdout, result.stderr
    except Exception as e:
        return -1, "", f"ERROR running {' '.join(cmd)}: {e}"


def main() -> None:
    os.makedirs("tmp", exist_ok=True)

    lines: list[str] = []
    lines.append("QUALITY CHECK SUMMARY")
    lines.append("")
    lines.append("Step 1: Format (dotnet fantomas .)")
    lines.append("Step 2: Build (dotnet build)")

    statuses: list[str] = []
    detailed_logs: list[str] = []

    # --- Step 1: Format ---
    fmt_rc, fmt_stdout, fmt_stderr = run_cmd(["dotnet", "fantomas", "."], "fantomas")
    if fmt_rc == 0:
        lines.append("  Format: OK")
        statuses.append("PASS")
    else:
        lines.append("  Format: FAILED (exit code {})".format(fmt_rc))
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
        ["dotnet", "build", SOLUTION, "-c", "Debug"], "build"
    )
    build_output = build_stdout + build_stderr
    if build_rc == 0 and "Build succeeded" in build_output:
        lines.append("  Build: OK (Build succeeded)")
        statuses.append("PASS")
    elif "Build succeeded" in build_output:
        lines.append("  Build: OK (Build succeeded)")
        statuses.append("PASS")
    else:
        # Count errors
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

    with open(OUTPUT_FILE, "w") as f:
        f.write("\n".join(lines))

    sys.exit(exit_code)


if __name__ == "__main__":
    main()
