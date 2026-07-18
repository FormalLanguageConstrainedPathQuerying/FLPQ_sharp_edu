# Tools

Auxiliary Python scripts for project quality control in `tools/`.

See `tools/README.md` for output conventions (no timeout, file structure, STATUS values, incremental flushing).

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

**Incremental output:** the output file is flushed after each step. While the gate is running, the file shows `STATUS: IN_PROGRESS`. The final `STATUS: PASS` or `STATUS: BLOCKED` is only written when all steps complete. The exit code is the authoritative signal.

**Exit codes:**
- 0 — STATUS: PASS (all steps passed)
- 1 — STATUS: BLOCKED (any step failed)

**Merge-blocking messages** appended after the detailed log:

| Status | Message |
|--------|---------|
| `IN_PROGRESS` | `HARD GATE IN PROGRESS. DO NOT MERGE. Await completion.` |
| `BLOCKED` | `HARD GATE FAILED. Exit code 1. DO NOT MERGE. Resolve ALL failures and re-run.` |
| `PASS` | (none) |

**Output format (mid-execution, after Step 2):**
```
HARD GATE SUMMARY
Step 1 (Format): OK
Step 2 (Build): OK (Build succeeded)

STATUS: IN_PROGRESS

--- DETAILED LOG ---

--- STEP 1: FORMAT (dotnet fantomas . --check) ---
(no output)

--- STEP 2: BUILD (dotnet build) ---
Build succeeded.
...

HARD GATE IN PROGRESS. DO NOT MERGE. Await completion.
```

**Output format (final — PASS):**
```
HARD GATE SUMMARY
Step 1 (Format): OK
Step 2 (Build): OK (Build succeeded)
Step 3 (Tests): OK (0 failed, 0 skipped)
Step 4 (Coverage):
  FLPQ.Languages: 89.8% (4376/4874) — PASS
  TOTAL: 84.2% (7056/8380) (threshold 80%) — PASS
  Coverage gate: PASS
Step 5 (Lint):
  src/FLPQ.Languages/FLPQ.Languages.fsproj: 0 warnings — PASS
  Lint gate: PASS

STATUS: PASS

--- DETAILED LOG ---
[per-step detailed output]
```

**Output format (final — BLOCKED):**
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

HARD GATE FAILED. Exit code 1. DO NOT MERGE. Resolve ALL failures and re-run.
```

If a step fails early (e.g., build failure), subsequent steps are not executed and `STATUS: BLOCKED` is written immediately.

## Skill Integration

The skills that reference these tools:

- `.opencode/skills/quality-gates/SKILL.md` — commit gate and task verification sections reference `quality_check.py` and `hard_gate.py`
- `.opencode/skills/subtask-loop/SKILL.md` — pre-commit check step references `quality_check.py`; task completion verification references `hard_gate.py`
