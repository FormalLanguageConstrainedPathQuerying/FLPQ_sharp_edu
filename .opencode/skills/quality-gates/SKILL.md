---
name: quality-gates
description: Use when running quality checks: format check, lint, build, test (with coverage), coverage verification — either per-subtask or pre-merge. Covers tool output capture rules, the full gates sequence, verification commands, and re-running logic.
---

# Quality Gates

Every quality gate MUST pass before a commit or merge. A single failing test, lint warning, or coverage drop is a blocker. Zero means absolute zero — pre-existing warnings are not exempt.

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
| `dotnet-fsharplint lint` | `tmp/fsharplint-output.txt` |
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

### 1. Format

```bash
dotnet fantomas .
```

Applies formatting in-place across the entire solution. If files were modified, re-stage them (`git add`) before committing.

### 2. Build

```bash
dotnet build FLPQ.slnx -c Debug > tmp/build-output.txt 2>&1
```

Verify:
```bash
grep "Build succeeded" tmp/build-output.txt
grep "Error(s)" tmp/build-output.txt
```
Must match `Build succeeded` and show `0 Error(s)`.

## Task Verification

Run once when all subtasks are done. Every gate must pass.

### 1. Lint

Lint only projects that contain modified `.fs` files. Zero warnings policy: any warning in a modified project is a blocker, including pre-existing warnings.

**Step 1 — find affected projects:**

```bash
git diff --cached --name-only | grep '\.fs$' | xargs -n1 dirname | sort -u | while read d; do
  ls "$d"/*.fsproj 2>/dev/null
done | sort -u > tmp/lint-projects.txt
```

If no `.fs` files are modified, skip lint.

**Step 2 — lint each affected project:**

```bash
while read proj; do
  DOTNET_ROOT=/usr/lib/dotnet dotnet-fsharplint lint "$proj" >> tmp/fsharplint-output.txt 2>&1
done < tmp/lint-projects.txt
```
Timeout: 900000 (15 min) per project.

**Step 3 — verify zero warnings:**

```bash
grep "Summary:" tmp/fsharplint-output.txt
```
Must show `Summary: 0 warnings` for every project linted. Any non-zero count is a blocker.

**Step 4 — remediate:**

Fix all warnings shown in `tmp/fsharplint-output.txt`, re-lint until `Summary: 0 warnings` on every affected project.

**Pre-merge full-solution lint** (run once before merging to `dev`):

```bash
DOTNET_ROOT=/usr/lib/dotnet dotnet-fsharplint lint FLPQ.slnx > tmp/fsharplint-output.txt 2>&1
```
Timeout: 1800000 (30 min). Must show `Summary: 0 warnings`.

### 2. Tests with Coverage

Run tests while collecting coverage. Always collect coverage — never run `dotnet test` without `dotnet dotnet-coverage`:

```bash
dotnet dotnet-coverage collect \
  dotnet test FLPQ.slnx \
  -o tmp/coverage.cobertura \
  -f cobertura \
  --nologo \
  > tmp/coverage-output.txt 2>&1
```
Timeout: 1800000 (30 min).

Verify tests:
```bash
grep "Failed\|Skipped" tmp/coverage-output.txt
```
Every test passes, zero failures, `Skipped: 0`. Any `Failed:` with non-zero count or `Skipped:` with non-zero count is a blocker.

### 3. Coverage Gate

After tests pass, verify total FLPQ source coverage is above the threshold.

Parse coverage from Cobertura XML:

```python
import xml.etree.ElementTree as ET

tree = ET.parse("tmp/coverage.cobertura")
root = tree.getroot()

source_packages = ["FLPQ.LinearAlgebra", "FLPQ.GraphAnalysis", "FLPQ.Languages",
                   "FLPQ.Printers", "FLPQ.RPQ", "FLPQ.Cli"]

total_covered, total_valid = 0, 0
for pkg in root.findall(".//package"):
    name = pkg.attrib.get("name", "")
    if name not in source_packages:
        continue
    for cls in pkg.findall(".//class"):
        for line in cls.findall(".//lines/line"):
            if int(line.attrib.get("hits", 0)) > 0:
                total_covered += 1
            total_valid += 1

rate = (total_covered / total_valid * 100) if total_valid > 0 else 0
print(f"FLPQ source line coverage: {rate:.1f}% ({total_covered}/{total_valid})")
if rate < 80:
    raise SystemExit(f"Coverage {rate:.1f}% below 80% threshold")
```

**Threshold: total FLPQ source line coverage MUST be > 80%.** Coverage drop is a blocker, same as a failing test.

Note: coverage instruments ALL assemblies, including F# core, test frameworks, and Microsoft internals. Filter to only `FLPQ.*` source packages (excluding `*.Tests`).

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
