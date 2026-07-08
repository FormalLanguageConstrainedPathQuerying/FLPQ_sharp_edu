# FSharpLint Report

**Generated:** 2026-07-08
**Tool:** dotnet-fsharplint v0.27.0 (project config: `fsharplint.json`)
**Command:** `DOTNET_ROOT=/usr/lib/dotnet dotnet-fsharplint lint <project>.fsproj` (per project)
**Config changes from default:**
- `genericTypesNames`: `"naming": "CamelCase"` (was PascalCase) — matches project convention of lowercase type parameters (`'t`, `'nt`, `'a`)
- `nestedFunctionNames`: `"enabled": true`, `"naming": "CamelCase"` (was disabled, PascalCase) — matches project convention of camelCase local functions

**Enabled rules:** 43 out of 98 (44 after enabling `nestedFunctionNames`)

---

## Summary

| Metric | Value |
|--------|-------|
| Total projects linted | 12 (6 source + 6 test) |
| **Total warnings** | **~162** |

### Comparison with Default Config

| Rule | Default Config | Project Config | Change |
|------|---------------|----------------|--------|
| FL0069 (Type Parameter Naming) | 1,196 warnings | **0** | Resolved — naming set to CamelCase matches project convention |
| FL0039 (Record Field Naming) | 111 | ~110 | Unchanged — project uses camelCase fields deliberately |
| FL0085 (Local Function Naming / TailCall) | 30 (naming only) | ~50 (tail call diagnostics only) | Now enabled; checks for `[<TailCall>]` on recursive functions. Naming violations resolved via CamelCase setting. |
| FL0034 (Redundant Lambda) | 6 | ~5 | Unchanged |
| FL0045 (Member Naming) | 5 | ~5 | Unchanged |
| FL0067 (Module-level Value Naming) | 3 | ~3 | Unchanged |
| FL0058 (Tuple Wildcard) | 2 | ~2 | Unchanged |
| FL0065 (Hint suggestions) | 0 | ~3 | New — triggered by enabled hints in test files |
| FL0035 (Lambda → composition) | 0 | ~2 | New |
| FL0087 (Interpolated string) | 0 | ~1 | New |

### Warnings by Rule

| Rule | Count | Description |
|------|-------|-------------|
| FL0039 | ~110 | Record Field / Union Case Field Naming — fields must be PascalCase |
| FL0085 | ~50 | Local function recursive with no `[<TailCall>]` attribute |
| FL0045 | ~5 | Member Naming — members must be PascalCase |
| FL0034 | ~5 | Redundant Lambda (eta reduction possible) |
| FL0067 | ~3 | Module-level Value Naming — values must be camelCase |
| FL0065 | ~3 | Hint suggestions (e.g., `List.isEmpty` instead of `x = []`) |
| FL0035 | ~2 | Lambda can be replaced with composition |
| FL0058 | ~2 | Tuple wildcard pattern simplification |
| FL0087 | ~1 | Interpolated string without interpolation |

---

## Rule Details

### FL0039 — Record Field / Union Case Field Naming (~110 warnings)

Record fields and discriminated union case fields must use PascalCase naming (e.g., `VertexMap` not `vertexMap`, `StartState` not `startState`). This is a deliberate project code style choice.

**Top affected files:**

| Warnings | File |
|----------|------|
| 92 | `src/FLPQ.Languages/` (Grammar, Automaton, RSM, GllTypes, RnglrTypes, etc.) |
| 6 | `src/FLPQ.Printers/` (VisualizationTypes, ExternalTools) |
| 2 | `src/FLPQ.GraphAnalysis/Graph.fs` |

The project uses camelCase for record fields consistently as a deliberate code style choice.

---

### FL0085 — Local Function Naming / Tail Call Diagnostics (~50 warnings)

Now enabled with `"naming": "CamelCase"`. All naming violations from the default config are resolved — the project already uses camelCase for local functions.

**Remaining warnings**: recursive local functions without `[<TailCall>]` attribute. This is triggered by the `ensureTailCallDiagnosticsInRecursiveFunctions` rule (part of FL0085).

**Affected files:**

| Warnings | File |
|----------|------|
| 26 | `src/FLPQ.Languages/` (Grammar, FirstFollow, EbnfParser, Gll, Rnglr, Valiant, etc.) |
| 3 | `src/FLPQ.Printers/DerivationTreeDot.fs` |
| 1 | `src/FLPQ.RPQ/ArroyueloRPQ.fs` |

Typical pattern: `let rec loop ...`, `let rec nullable ...`, `let rec derive ...` — recursive functions that could benefit from `[<TailCall>]` annotation.

---

### FL0034 — Redundant Lambda (~5 warnings)

Eta-reducible lambda expressions where the function can be partially applied directly.

| File | Expression |
|------|------------|
| `src/FLPQ.GraphAnalysis/Graph.fs` | `(fun e k -> e && k)` → `(&&)` |
| `src/FLPQ.GraphAnalysis/MsBfs.fs` | `(fun acc x -> acc \|\| x)` → `(\|\|)` |
| `src/FLPQ.RPQ/BelyaninRPQ.fs` | `(fun acc x -> acc \|\| x)` → `(\|\|)` |
| `tests/FLPQ.Languages.Tests/GllTests.fs` | `(fun c -> string c)` → `string` |

---

### FL0045 — Member Naming (~5 warnings)

Member names must use PascalCase.

| File | Member |
|------|--------|
| `src/FLPQ.Languages/Automaton.fs` | `transitions` (×2), `states` (×2) |
| `src/FLPQ.Printers/SummaryTeX.fs` | `toString` |

---

### Other Rules

**FL0067 (Module-level Value Naming, ~3 warnings):**
- `src/FLPQ.Languages/Gll.fs`: `K` → `k`
- `src/FLPQ.Languages/Rnglr.fs`: `K` (×2)

**FL0065 (Hint suggestions, ~3 warnings):**
- `tests/FLPQ.Languages.Tests/TokenizerTests.fs`: `x = []` → `List.isEmpty x`
- `tests/FLPQ.Languages.Tests/AutomatonTests.fs`: `List.map id x` → `id x`

**FL0035 (Lambda → composition, ~2 warnings):**
- `tests/FLPQ.Languages.Tests/GllTests.fs`: `fun s -> Terminal(RsmSymbol.RTerm(Terminal s))` can be replaced with composition
- `tests/FLPQ.Languages.Tests/RnglrTests.fs`: same pattern

**FL0087 (Interpolated string, ~1 warning):**
- `tests/FLPQ.Languages.Tests/StressTests.fs`: `sprintf "E1"` → `"E1"` (no interpolation)

**FL0058 (Tuple wildcard, ~2 warnings):**
- `src/FLPQ.Languages/Gll.fs`: `SppfNodeInfo.SppfRange(_, _, _, _)` → `SppfNodeInfo.SppfRange _`

---

## Complete File List with Warning Counts

### Source files (src/)

| Warnings | File |
|----------|------|
| 127 | `src/FLPQ.Languages/` (16 files) |
| 11 | `src/FLPQ.LinearAlgebra/` (3 files) |
| 10 | `src/FLPQ.Printers/` (14 files) |
| 4 | `src/FLPQ.GraphAnalysis/` (2 files) |
| 2 | `src/FLPQ.RPQ/` (4 files) |
| 0 | `src/FLPQ.Cli/` (8 files) |

### Test files (tests/)

| Warnings | File |
|----------|------|
| 8 | `tests/FLPQ.Languages.Tests/` (14 files) |
| 0 | `tests/FLPQ.Printers.Tests/` |
| 0 | `tests/FLPQ.Cli.Tests/` |
| 0 | `tests/FLPQ.GraphAnalysis.Tests/` |
| 0 | `tests/FLPQ.LinearAlgebra.Tests/` |
| 0 | `tests/FLPQ.TestUtilities/` |

---

## Key Observations

1. **FL0069 (1,196 warnings) completely eliminated** — changing `genericTypesNames.naming` to `CamelCase` resolved all type parameter naming violations. The project's convention of lowercase type parameters (`'t`, `'nt`, `'a`) is now recognized as valid by the linter.

2. **FL0085 now reports tail call diagnostics** — enabling `nestedFunctionNames` with `CamelCase` naming resolved all naming violations (the project already uses camelCase for local functions). The remaining warnings are from `ensureTailCallDiagnosticsInRecursiveFunctions`, which suggests adding `[<TailCall>]` attributes to recursive local functions.

3. **FL0039 remains the largest category** (~110 warnings) — record fields and union case fields using camelCase. This is a deliberate project convention.

4. **Total warnings reduced from 1,323 to ~162** (88% reduction). The two tuning changes addressed the largest category (FL0069, 90% of all warnings) while enabling FL0085 to report tail call diagnostics.
