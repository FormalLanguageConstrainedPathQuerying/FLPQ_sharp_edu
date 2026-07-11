# Tools

Auxiliary Python scripts for project quality control in `tools/`.

## Conventions

1. All tools run **without timeout** — let each command take as long as it needs.
2. All output is written to `tmp/<tool-name>.txt`. **Nothing** is written to console.
3. Output files are overwritten on each run (no append).
4. Each output file follows a structured format:
   - **Header**: short summary of findings (PASS/BLOCKED, key counts)
   - **Separator**: `--- DETAILED LOG ---`
   - **Body**: detailed output of each executed command
5. Workflow: run tool → analyze output file → fix all problems → re-run tool.

## Tools

### `detect_changes.py`

Detects which projects have modified `.fs` files relative to the `dev` branch.

**Output:** `tmp/detect-changes.txt`

**Exit codes:**
- 0 — clean or modified (both are valid states)
- 1 — error running git commands

**Output format:**
```
DETECT CHANGES SUMMARY
Branch: <branch>
Found N modified .fs file(s) in M project(s):
  src/FLPQ.Languages/FLPQ.Languages.fsproj
STATUS: MODIFIED (or CLEAN if no changes)
--- DETAILED LOG ---
Changed files (file -> project mapping):
  src/FLPQ.Languages/Sppf.fs -> src/FLPQ.Languages/FLPQ.Languages.fsproj
```

### `quality_check.py`

Inter-subtask quality check: format then build. Used as the commit gate.

**Output:** `tmp/quality-check.txt`

**Steps:**
1. Format: `dotnet fantomas .`
2. Build: `dotnet build FLPQ.slnx -c Debug`

**Exit codes:**
- 0 — STATUS: PASS
- 1 — STATUS: BLOCKED (format or build failed)

**Output format:**
```
QUALITY CHECK SUMMARY
Step 1: Format (dotnet fantomas .)
Step 2: Build (dotnet build)
  Format: OK
  Build: OK (Build succeeded)
STATUS: PASS
--- DETAILED LOG ---
[per-command stdout/stderr]
```

### `hard_gate.py`

Full quality gate for task verification: format → build → tests with coverage → coverage verification → lint on changed projects.

**Output:** `tmp/hard-gate.txt`

**Steps:**
1. Format: `dotnet fantomas .`
2. Build: `dotnet build FLPQ.slnx -c Debug`
3. Tests + Coverage: `dotnet dotnet-coverage collect dotnet test FLPQ.slnx -o tmp/coverage.cobertura -f cobertura --nologo`
4. Coverage gate: parse `tmp/coverage.cobertura` XML
   - Per-project threshold: **75%** line coverage minimum
   - Total threshold: **80%** line coverage minimum
   - Filters to FLPQ source packages only (excludes `*.Tests` and `FLPQ.TestUtilities`)
5. Lint: `dotnet-fsharplint lint` on each project with modified `.fs` files (detected via `detect_changes.py` logic). Uses `DOTNET_ROOT` from environment or `/usr/lib/dotnet`.

**Exit codes:**
- 0 — all steps PASS
- 1 — any step BLOCKED

**Output format:**
```
HARD GATE SUMMARY
Step 1 (Format): OK
Step 2 (Build): OK (Build succeeded)
Step 3 (Tests): OK (0 failed, 0 skipped)
Step 4 (Coverage):
  FLPQ.Languages: 89.8% (4376/4874) — PASS
  FLPQ.Cli: 55.0% (438/796) — BLOCKED (below 75%)
  TOTAL: 84.2% (7056/8380) (threshold 80%) — PASS
  Coverage gate: BLOCKED
Step 5 (Lint):
  src/FLPQ.Languages/FLPQ.Languages.fsproj: 0 warnings — PASS
  Lint gate: PASS
STATUS: BLOCKED
--- DETAILED LOG ---
[per-step detailed output]
```

If a step fails early (e.g., build failure), subsequent steps are not executed and STATUS is reported immediately.

## Skill Integration

The skills that reference these tools:

- `.opencode/skills/quality-gates/SKILL.md` — commit gate and task verification sections reference `quality_check.py` and `hard_gate.py`
- `.opencode/skills/subtask-loop/SKILL.md` — pre-commit check step references `quality_check.py`; task completion verification references `hard_gate.py`
