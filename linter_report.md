# FSharpLint Report

**Generated:** 2026-07-08
**Tool:** dotnet-fsharplint v0.27.0 (default configuration)
**Command:** `dotnet fsharplint lint FLPQ.slnx`
**Enabled rules:** 42 out of 98

---

## Summary

| Metric | Value |
|--------|-------|
| Total files linted | 82 |
| Files with zero warnings | 38 |
| Files with warnings | 44 |
| **Total warnings** | **1,323** |

### Warnings by Rule

| Rule | Count | Description |
|------|-------|-------------|
| FL0069 | 1,196 | Interface / Type Parameter Naming — type parameters should be PascalCase (`'T` not `'t`) |
| FL0039 | 111 | Record Field / Union Case Field Naming — fields must be PascalCase |
| FL0085 | 30 | Local Function Naming — inner function names use PascalCase (`Loop` not `loop`) |
| FL0034 | 6 | Lambda can be removed (eta reduction possible) |
| FL0045 | 5 | Member Naming — members must be PascalCase (e.g. `ToString` not `toString`) |
| FL0067 | 3 | Module-level Value Naming — values must be camelCase (`k` not `K`) |
| FL0058 | 2 | Tuple wildcard pattern simplification (`Foo(_, _)` → `Foo _`) |

---

## Rule Details

### FL0069 — Type Parameter Naming (1,196 warnings)

Type parameters in generic type annotations should use PascalCase naming convention (e.g., `'T` instead of `'t`, `'Nt` instead of `'nt`). This is by far the most common violation, appearing in virtually every source file.

**Top affected files:**

| Warnings | File |
|----------|------|
| 176 | `src/FLPQ.Languages/LRParser.fs` |
| 96 | `src/FLPQ.Languages/Valiant.fs` |
| 81 | `src/FLPQ.Languages/Grammar.fs` |
| 74 | `src/FLPQ.Languages/Automaton.fs` |
| 62 | `src/FLPQ.Languages/RSM.fs` |
| 59 | `src/FLPQ.GraphAnalysis/Graph.fs` |
| 56 | `src/FLPQ.Languages/FirstFollow.fs` |
| 55 | `src/FLPQ.Languages/LLParser.fs` |
| 49 | `src/FLPQ.Languages/Cyk.fs` |
| 41 | `src/FLPQ.LinearAlgebra/Matrix.fs` |

**Most common type parameter violations:**

| Parameter | Occurrences |
|-----------|-------------|
| `'nt` → `'Nt` | 534 |
| `'t` → `'T` | 471 |
| `'a` → `'A` | 51 |
| `'s` → `'S` | 34 |
| `'e` → `'E` | 31 |
| `'v` → `'V` | 26 |

**Rationale for codebase convention:** The project intentionally uses lowercase single-letter type parameters (`'t`, `'nt`, `'a`, `'s`, etc.) as an established F# community convention for generic type variables. This rule conflicts with the project's style and is not considered an actual issue.

---

### FL0039 — Record Field / Union Case Field Naming (111 warnings)

Record fields and discriminated union case fields must use PascalCase naming (e.g., `VertexMap` not `vertexMap`, `StartState` not `startState`).

**Top affected files:**

| Warnings | File |
|----------|------|
| 15 | `src/FLPQ.Languages/GllTypes.fs` |
| 13 | `src/FLPQ.Languages/LRParser.fs` |
| 12 | `src/FLPQ.Languages/Valiant.fs` |
| 12 | `src/FLPQ.Languages/RSM.fs` |
| 11 | `src/FLPQ.LinearAlgebra/Matrix.fs` |
| 10 | `src/FLPQ.Languages/RnglrTypes.fs` |
| 8 | `src/FLPQ.Languages/Automaton.fs` |
| 7 | `src/FLPQ.Languages/LLParser.fs` |

**Most common field name violations:**

| Field | Occurrences |
|-------|-------------|
| `item` | 18 |
| `sym` | 4 |
| `acc` | 4 |
| `table` | 3 |
| `rhs` | 3 |
| `lhs` | 3 |
| `input` | 3 |
| `finalStates` | 3 |

The project uses camelCase for record fields consistently as a deliberate code style choice.

---

### FL0085 — Local Function Naming (30 warnings)

Nested `let`-bound functions (inside other functions) should use PascalCase naming (`Loop` not `loop`, `Nullable` not `nullable`).

**Affected files:**

| Warnings | File |
|----------|------|
| 9 | `src/FLPQ.Languages/EbnfParser.fs` |
| 4 | `src/FLPQ.Languages/Grammar.fs` |
| 4 | `src/FLPQ.Languages/Valiant.fs` |
| 3 | `src/FLPQ.Languages/Gll.fs` |
| 3 | `src/FLPQ.Printers/DerivationTreeDot.fs` |
| 2 | `src/FLPQ.Languages/DerivationTree.fs` |
| 2 | `src/FLPQ.Languages/Rnglr.fs` |
| 1 | `src/FLPQ.Languages/FirstFollow.fs` |
| 1 | `src/FLPQ.Languages/LLParser.fs` |
| 1 | `src/FLPQ.RPQ/ArroyueloRPQ.fs` |

Typical patterns: `let rec loop ...`, `let rec nullable ...`, `let rec derive ...`, `let rec parseAtom ...`.

---

### FL0034 — Redundant Lambda (6 warnings)

Eta-reducible lambda expressions where the function can be partially applied directly.

| File | Line | Expression |
|------|------|------------|
| `src/FLPQ.Languages/Grammar.fs` | — | `(fun i j -> i <> j)` |
| `src/FLPQ.Languages/Valiant.fs` | — | `(fun acc x -> acc \|\| x)` |
| `src/FLPQ.Languages/Valiant.fs` | — | `(fun acc x -> acc \|\| x)` |
| `src/FLPQ.RPQ/BelyaninRPQ.fs` | 33 | `(fun acc x -> acc \|\| x)` |
| `src/FLPQ.RPQ/ArroyueloRPQ.fs` | — | `(fun acc x -> acc && x)` |
| `src/FLPQ.GraphAnalysis/Graph.fs` | — | `(fun i j -> i <> j)` |

These can be replaced with direct function references: `(<>)`, `(||)`, `(&&)`.

---

### FL0045 — Member Naming (5 warnings)

Member names must use PascalCase.

| File | Member |
|------|--------|
| `src/FLPQ.Languages/Automaton.fs` | `transitions` (×2) |
| `src/FLPQ.Languages/Automaton.fs` | `states` |
| `src/FLPQ.Languages/Automaton.fs` | `edges` |
| `src/FLPQ.Printers/SummaryTeX.fs` | `toString` |

---

### FL0067 — Module-level Value Naming (3 warnings)

Module-level `let`-bound values must use camelCase.

| File | Value |
|------|-------|
| `src/FLPQ.Languages/Gll.fs` | `K` (should be `k`) |
| `src/FLPQ.Languages/Rnglr.fs` | `K` (×2) |

---

### FL0058 — Tuple Wildcard Simplification (2 warnings)

Multi-wildcard patterns can be simplified.

| File | Pattern | Suggested |
|------|---------|-----------|
| `src/FLPQ.Languages/Gll.fs` | `SppfNodeInfo.SppfRange(_, _, _, _)` | `SppfNodeInfo.SppfRange _` |
| `src/FLPQ.Languages/Gll.fs` | `SppfNodeInfo.SppfIntermediate(_, _)` | `SppfNodeInfo.SppfIntermediate _` |

---

## Complete File List with Warning Counts

### Source files (src/)

| Warnings | File |
|----------|------|
| 189 | `src/FLPQ.Languages/LRParser.fs` |
| 108 | `src/FLPQ.Languages/Valiant.fs` |
| 87 | `src/FLPQ.Languages/Grammar.fs` |
| 86 | `src/FLPQ.Languages/Automaton.fs` |
| 74 | `src/FLPQ.Languages/RSM.fs` |
| 62 | `src/FLPQ.GraphAnalysis/Graph.fs` |
| 62 | `src/FLPQ.Languages/LLParser.fs` |
| 56 | `src/FLPQ.Languages/FirstFollow.fs` |
| 52 | `src/FLPQ.LinearAlgebra/Matrix.fs` |
| 51 | `src/FLPQ.Languages/Cyk.fs` |
| 49 | `src/FLPQ.Languages/RnglrTypes.fs` |
| 45 | `src/FLPQ.Languages/Gll.fs` |
| 41 | `src/FLPQ.Languages/GllTypes.fs` |
| 40 | `src/FLPQ.Languages/RnglrLR.fs` |
| 36 | `src/FLPQ.Languages/Rnglr.fs` |
| 30 | `src/FLPQ.Languages/EbnfParser.fs` |
| 28 | `src/FLPQ.Languages/DerivationTree.fs` |
| 28 | `src/FLPQ.Printers/LRAutomatonTikz.fs` |
| 21 | `src/FLPQ.Printers/LLTableTeX.fs` |
| 20 | `src/FLPQ.Printers/DerivationTreeDot.fs` |
| 19 | `src/FLPQ.Printers/LRTableTeX.fs` |
| 17 | `src/FLPQ.LinearAlgebra/LinearAlgebra.fs` |
| 13 | `src/FLPQ.Printers/AutomatonDot.fs` |
| 13 | `src/FLPQ.Printers/AutomatonTikz.fs` |
| 12 | `src/FLPQ.Printers/GrammarTeX.fs` |
| 8 | `src/FLPQ.Printers/LLStepVisualizer.fs` |
| 8 | `src/FLPQ.Printers/LRStepVisualizer.fs` |
| 7 | `src/FLPQ.Languages/Tokenizer.fs` |
| 6 | `src/FLPQ.LinearAlgebra/BooleanDecomposition.fs` |
| 6 | `src/FLPQ.Printers/SymbolTeX.fs` |
| 5 | `src/FLPQ.RPQ/BelyaninRPQ.fs` |
| 5 | `src/FLPQ.RPQ/ArroyueloRPQ.fs` |
| 5 | `src/FLPQ.RPQ/KroneckerRPQ.fs` |
| 4 | `src/FLPQ.Printers/CykTeX.fs` |
| 4 | `src/FLPQ.Printers/ExternalTools.fs` |
| 4 | `src/FLPQ.Printers/ValiantTeX.fs` |
| 4 | `src/FLPQ.Printers/ParsingTableTeX.fs` |
| 2 | `src/FLPQ.Printers/VisualizationTypes.fs` |
| 1 | `src/FLPQ.Printers/SummaryTeX.fs` |

### Test files (tests/)

| Warnings | File |
|----------|------|
| 6 | `tests/FLPQ.Languages.Tests/GllTests.fs` |
| 6 | `tests/FLPQ.Languages.Tests/GrammarTests.fs` |
| 3 | `tests/FLPQ.GraphAnalysis.Tests/GraphTests.fs` |
| 1 | `tests/FLPQ.Languages.Tests/EbnfParserTests.fs` |

### Files with zero warnings (38 files)

All CLI source files, most test files (AutomatonTests, CykTests, RnglrTests, ValiantTests, RPQ tests, Generators, golden tests, etc.), and all assembly info files produced zero warnings.

---

## Key Observation

Over 98% of warnings (1,307 out of 1,323) are naming convention violations (FL0069 + FL0039 + FL0085 + FL0045 + FL0067). These reflect a deliberate project convention of using lowercase for type parameters and camelCase for record fields, which is common and accepted in the F# community. The remaining 8 warnings (FL0034 × 6, FL0058 × 2) are minor style suggestions.
