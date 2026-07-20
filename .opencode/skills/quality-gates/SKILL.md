---
name: quality-gates
description: Use when running quality checks: format check, lint, build, test (with coverage), coverage verification — either per-subtask or pre-merge. Covers tool output capture rules, the full gates sequence, verification commands, and re-running logic.
---

# Quality Gates

Every quality gate MUST pass before a commit or merge. A single failing test, lint warning, or coverage drop is a blocker. Zero means absolute zero. Non-zero exit code from any gate tool means STOP — no exceptions, no partial passes.

## Tools

The following Python scripts in `tools/` automate quality gate execution. **Always prefer these tools over manual shell commands.**

See `tools/README.md` for the full tool list and output conventions. See `docs/developer/guides/tools.md` for per-tool details (steps, thresholds, output format examples).

## Tool Output Capture

**Mandatory**: every invocation of `dotnet build`, `dotnet test`, `dotnet-fsharplint lint`, `dotnet dotnet-coverage`, or similar expensive CLI tools MUST redirect all output (stdout + stderr) to a file in `tmp/`.

**NEVER use pipes** (`|`), `head`, `tail`, `grep`, `rg`, or any other shell filtering on tool output. Write raw output to `tmp/` first, then use the Grep or Read tools to analyze the captured file.

Command pattern:
```bash
dotnet test > tmp/test-output.txt 2>&1
```

After capturing, analyze the output file with the Grep or Read tools — do NOT re-run the tool with a different grep/filter.

### File naming convention

| Command | Output file |
|---------|------------|
| `dotnet build` | `tmp/build-output.txt` |
| `dotnet test` | `tmp/test-output.txt` |
| `dotnet fantomas .` | (in-place, no captured output) |
| `dotnet fsharplint lint` | `tmp/fsharplint-output.txt` |
| `dotnet dotnet-coverage` | `tmp/coverage-output.txt` |

Coverage data file: `tmp/coverage.cobertura`

### Re-running rules

Before running any expensive CLI tool, you MUST:

1. Check if the corresponding `tmp/<output-file>` already exists: `ls -la tmp/<file>`
2. If it exists and source files haven't changed since capture, read the existing file — NEVER re-run
3. Only re-run if the output file is missing, empty/truncated, or source files have changed since the last capture

**Sub-agent output is output.** Data returned by the Task tool (sub-agents) is equivalent to a captured output file. If a sub-agent already ran a tool and returned its results, do not re-run that tool.

**Timed-out output is valid.** Even if a tool times out or produces partial output, check the output file first. Do NOT re-run with a longer timeout unless source files changed AND the captured content is insufficient for decision-making.

### Output capture check

Every command below MUST use `> tmp/<file> 2>&1` redirection. Commands with pipes (`|`) or inline filtering are forbidden.

## Commit Gate

Run before every commit. Both steps must pass.

The format step is **check-only**: it verifies formatting but does NOT modify files. You must run fantomas manually before the gate:

```bash
dotnet fantomas .
```

Then stage any formatted files (`git add`), then run the gate:

```bash
python3 tools/quality_check.py
```

Then read and analyze `tmp/quality-check.txt`. If STATUS: BLOCKED — fix all problems and re-run.

## Task Verification

Run once when all subtasks are done. The full hard gate runs format → build → tests → coverage → lint and may take tens of minutes depending on project size and how many projects changed. Run it **asynchronously** with periodic status polling.

### Steps and Timing

Times are baseline estimates for the current codebase and will grow as the project grows.

| Step | What | Notes |
|------|------|-------|
| 1. Format | `dotnet fantomas . --check` | Negligible |
| 2. Build | `dotnet build FLPQ.slnx` | Builds all projects |
| 3. Tests | Per-project `dotnet test` with coverage | Printers.Tests is the slowest (TeX compilation) |
| 4. Coverage | Per-project + total threshold check | Negligible |
| 5. Lint | `fsharplint lint` on changed projects | The slowest step; time proportional to number of changed projects |

### File Structure of `tmp/hard-gate.txt`

The output has two sections separated by `--- DETAILED LOG ---`:

- **Top (~15 lines)**: step summary — shows `Step N/M` counters and per-project results. **Always read this first** to see overall progress.
- **Bottom (after the separator)**: raw command output for the currently running or most recently completed step.

**During polling, read BOTH sections:**

```bash
head -15 tmp/hard-gate.txt   # step summary
tail -20 tmp/hard-gate.txt   # current detailed output
```

### Starting the Gate

```bash
python3 tools/hard_gate.py > tmp/hard-gate-stderr.txt 2>&1 &
echo $! > tmp/hard-gate.pid
```

The gate runs in background and writes progress incrementally to `tmp/hard-gate.txt` after each step.

### Polling Status

**Poll interval: every 5 minutes.** Set a timer — do not poll more frequently as the gate file may not be updated between flushes.

```bash
grep "STATUS:" tmp/hard-gate.txt
```

- **`STATUS: PASS`** — gate finished successfully (exit code 0). Proceed to merge.
- **`STATUS: BLOCKED`** — gate finished with exit code non-zero. Read the step summary at the top of `tmp/hard-gate.txt` to identify which step(s) failed, then read the detailed log to identify the failure, fix all problems, and **re-run the gate from the start**.
- **`STATUS: IN_PROGRESS`** — gate is still running. Read `head -15` for the step summary: if step numbers advanced since the last poll, the gate is making progress — continue waiting. If the step counter has not advanced after 3+ polling cycles (>15 minutes with no progress), check `tail -20` for the currently executing command and verify it hasn't hung. **Keep polling** until `IN_PROGRESS` changes to `PASS` or `BLOCKED`.

**Never interpret `IN_PROGRESS` as a pass or failure.** Only `PASS` and `BLOCKED` are terminal states.

### Verifying Failures

When a step reports `FAILED` but shows `0 failed, 0 skipped`, the tool itself may be misreading the test output. **Verify manually by running the project directly:**

```bash
dotnet test tests/<Project>/<Project>.fsproj 2>&1
```

If the manual run passes (0 failed, 0 skipped), the gate tool has a false positive — report it and proceed. If the manual run also fails, fix the failing tests and re-run the full gate.

### After Completion

When the gate finishes, check the exit status:
- If any step shows `BLOCKED` in the summary, read the detailed log to identify the failure, fix all problems, and re-run from the start.
- Exit code 0 and `STATUS: PASS` means proceed to merge.

The hard gate runs: format → build → tests with coverage → coverage verification → lint on changed projects. See `docs/developer/guides/tools.md` for detailed step descriptions, thresholds, and output format examples.

### Lint Verification

The hard gate's Step 5 runs `dotnet-fsharplint lint` on each project with modified `.fs` files (detected by `tools/detect_changes.py`). Output appears in `tmp/hard-gate.txt`.

To run lint manually on a specific project:

```bash
DOTNET_ROOT=/usr/lib/dotnet dotnet fsharplint lint <project.fsproj> > tmp/fsharplint-output.txt 2>&1
```

Zero warnings policy: any warning in a modified project is a blocker, including pre-existing warnings.

Report format for each affected project:
```
src/FLPQ.Languages/FLPQ.Languages.fsproj: 0 warnings — PASS
tests/FLPQ.Languages.Tests/FLPQ.Languages.Tests.fsproj: 3 warnings — BLOCKED
```
A single `BLOCKED` means go back and fix. Continue only when every line says `PASS`.

**Pre-merge full-solution lint** (run once before merging to `dev`):

```bash
DOTNET_ROOT=/usr/lib/dotnet dotnet fsharplint lint FLPQ.slnx > tmp/fsharplint-output.txt 2>&1
```
Timeout: 1800000 (30 min). Must show `Summary: 0 warnings`.

## Hard Gate

A subtask is committed ONLY when the commit gate passes. A task is complete ONLY when task verification passes. A single build failure, test failure, lint warning, or coverage drop is a blocker.

If you encounter a problem you cannot resolve to 100% correctness, STOP. Do not commit. Do not merge. Do not comment out or weaken failing tests. See the blocked work protocol in AGENTS.md.

## FSharpLint Configuration

The project uses a custom `fsharplint.json` at the repository root.

**Critical**: FSharpLint 0.27.0 does **not** support partial configs — the file must contain the **full** default configuration with only the desired rules changed. The reference default config lives at:

```
https://raw.githubusercontent.com/fsprojects/FSharpLint/master/src/FSharpLint.Core/fsharplint.json
```

### Tuned Rules

The following rules were changed from FSharpLint defaults to match the project code style (as documented in `AGENTS.md`):

| Rule | Setting | Default | Reason |
|------|---------|---------|--------|
| `genericTypesNames` (FL0069) | `naming: CamelCase` | PascalCase | Project uses `'t`, `'nt`, `'a`, `'s`, `'v` |
| `nestedFunctionNames` (FL0085) | `enabled: true`, `naming: CamelCase` | disabled, PascalCase | Project uses camelCase for `let rec loop`/`derive` etc. |
| `recordFieldNames` (FL0039) | `naming: PascalCase` | PascalCase | Per AGENTS.md "PascalCase for record fields and union case fields" |

### FL0085 Dual Behavior

When `nestedFunctionNames` is enabled, FL0085 reports **two** categories of warnings:

1. **Naming**: local function names not matching the configured convention (`CamelCase`). Resolved — project already uses camelCase.
2. **Tail-call diagnostics**: any `let rec` function (local or top-level) missing `[<TailCall>]` attribute. This is from the `ensureTailCallDiagnosticsInRecursiveFunctions` rule.

To suppress only the tail-call diagnostics while keeping naming checks, set `ensureTailCallDiagnosticsInRecursiveFunctions` to `false` in the config.

## Debugging Test Hangs

When a test hangs, times out, or produces wrong results, use the `debugging` skill for print-trace debugging.
