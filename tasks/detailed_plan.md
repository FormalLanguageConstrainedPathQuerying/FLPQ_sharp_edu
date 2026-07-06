# Detailed Plan: Task 136 - Set test coverage calculation up

## Goal
Set up test coverage calculation using `dotnet-coverage` tool. Add CI check for >= 80% line coverage. Analyze current coverage report. Do NOT improve coverage.

## Sub-tasks

### 1. Install and configure dotnet-coverage [DONE]
- [x] Installed `dotnet-coverage` v18.8.0 as a local tool in `dotnet-tools.json`
- [x] All test projects already have `coverlet.collector` — no changes needed there

### 2. Create coverage script [DONE]
- [x] Created `scripts/run-coverage.sh` that:
  - Runs `dotnet dotnet-coverage collect dotnet test ...` to produce Cobertura XML
  - Outputs to `coverage/coverage.cobertura`
  - Includes inline Python script to parse and display per-project coverage summary

### 3. Run local coverage and analyze [DONE]
- [x] Ran the coverage script locally
- [x] Parsed the Cobertura XML report

**Coverage Results (FLPQ source projects only):**

| Package | Covered | Valid | Rate |
|---------|---------|-------|------|
| FLPQ.LinearAlgebra | 198 | 198 | 100.0% |
| FLPQ.GraphAnalysis | 100 | 102 | 98.0% |
| FLPQ.Languages | 3056 | 3206 | 95.3% |
| FLPQ.RPQ | 264 | 284 | 93.0% |
| FLPQ.Printers | 1326 | 1704 | 77.8% |
| FLPQ.Cli | 298 | 566 | 52.7% |
| **TOTAL** | **5242** | **6060** | **86.5%** |

**Low-coverage classes (< 50%):**
- `FLPQ.Printers.ExternalTools` — 0/258 = 0.0% (requires external Graphviz/TeX tools)
- `FLPQ.Printers.SummaryTeX.SummaryKind` — 0/8 = 0.0% (summary generation types)
- `FLPQ.Cli.Summary` — 10/126 = 7.9% (summary workflow, exercised by Summary-category tests excluded from CI)
- `FLPQ.Cli.LRRunner.dotContent@37-7` — 0/2 = 0.0% (DOT fallback path)
- `FLPQ.Cli.ValiantRunner` — 44/94 = 46.8% (Valiant algorithm CLI runner)
- `FLPQ.Cli.Helpers` — 36/100 = 36.0% (CLI helper utilities)

**Low-coverage files in FLPQ.Printers:**
- `ExternalTools.fs` — 0/258 = 0.0%
- `SummaryTeX.fs` — 90/148 = 60.8%
- `ParsingTableTeX.fs` — 16/24 = 66.7%

**Low-coverage files in FLPQ.Cli:**
- `Summary.fs` — 10/126 = 7.9%
- `Helpers.fs` — 36/100 = 36.0%
- `Program.fs` — 34/62 = 54.8%
- `ValiantRunner.fs` — 44/94 = 46.8%

### 4. Add CI coverage check [DONE]
- [x] Added `coverage-check` job to `.github/workflows/ci.yml`:
  - Runs on ubuntu-latest, depends on `build-and-test`
  - Restores local tools (dotnet-coverage)
  - Collects Cobertura XML via `dotnet dotnet-coverage collect`
  - Parses XML with inline Python, checks FLPQ source coverage >= 80%
  - Fails CI if threshold not met

### 5. Documentation [DONE]
- [x] Updated `tasks/knowledge_base.md` with dotnet-coverage section
- [x] Added `coverage/` to `.gitignore`

## Files Created/Modified
- **Created**: `scripts/run-coverage.sh` — local coverage script
- **Modified**: `.github/workflows/ci.yml` — added coverage-check job
- **Modified**: `.gitignore` — added `coverage/` ignore
- **Modified**: `dotnet-tools.json` — added dotnet-coverage tool entry
- **Modified**: `tasks/knowledge_base.md` — added dotnet-coverage knowledge

## Constraints
- Did NOT write additional tests to improve coverage
- Did NOT modify source code to improve coverage
- Only set up infrastructure and analyzed current state
