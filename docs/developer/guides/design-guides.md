# Design Guides

**Tags:** guide, design, architecture, separation-of-concerns, modularity, compile-time-safety
**Kind:** guide

> **Abstract:** Defines the project's architecture and design principles: separation of data from presentation (algorithms produce F# data, printers render it), one algorithm per file, project structure mapping to book chapters, variant algorithms as thin layers over shared infrastructure, avoidance of code duplication, and compile-time safety over runtime checks. Ensures algorithms stay testable, readable alongside the book, and reusable across chapters.

## Contents

- [Why design principles matter](#why-design-principles-matter)
- [What our design principles are](#what-our-design-principles-are)

## See Also

- [Coding Conventions](coding-conventions.md) | [Quality Standards](quality-standards.md)
- [Documentation Conventions](documentation-conventions.md)

## Why design principles matter

This project implements a family of interrelated algorithms across multiple domains (linear algebra, parsing, automata, RPQ). Without consistent architecture boundaries, algorithms become entangled with rendering logic, cross-domain coupling increases, and the code drifts from the book's structure.

## What our design principles are

### Separation of data from presentation

Algorithms collect trace, result, and intermediate data **exclusively as F# data structures** (records, discriminated unions, matrices). They must never call rendering, printing, or file I/O functions.

All conversion to output formats (TeX, dot, plain text) lives in `src/FLPQ.Printers`.

```fsharp
let result, trace = algorithm.run input
let texOutput    = TraceVisualizer.toTex trace
```

**Why**: The same algorithm must produce both TeX output (for the book) and dot output (for interactive exploration). Separating computation from rendering allows adding new output formats without touching algorithm code. It also keeps algorithms testable without requiring Graphviz or lualatex.

### One algorithm, one file

Each module file implements a single algorithm or family of closely related algorithms. The file name matches the algorithm name in the book.

**Why**: A reader who sees a reference to "Algorithm `algo:MS-BFS_linal`" in the book can open `src/FLPQ.GraphAnalysis/MsBfs.fs` and find the corresponding code without searching. File-per-algorithm also limits merge conflicts when multiple contributors work on different algorithms.

### Project structure maps to book chapters

| Project | Book content |
|---------|-------------|
| `FLPQ.LinearAlgebra` | Chapters 1, 3, 7 — matrices, Kronecker product, Boolean decomposition |
| `FLPQ.GraphAnalysis` | Chapters 3, 11 — MS-BFS, semiring operations on graphs |
| `FLPQ.Languages` | Chapters 5, 6, 7 — automata, grammars, parsing algorithms |
| `FLPQ.RPQ` | Chapters 3, 11, 12 — regular path querying |
| `FLPQ.Printers` | All chapters — visualization |

**Why**: The project structure mirrors the book's organization. A reader working through Chapter 7 (parsing) knows to look in `FLPQ.Languages`. This reduces cognitive overhead for both readers and contributors.

### Variants as thin layers

When implementing a variant of an existing algorithm (e.g., modified Valiant), maximize reuse of shared infrastructure. Write the variant as a thin layer over common functions, not a full rewrite.

**Why**: Variants differ in specific algorithmic choices (e.g., layer construction vs. recursive quartering), not in the entire pipeline. Rewriting everything obscures the difference. A thin layer makes the variant's unique contribution visible and ensures equivalence tests catch real regressions.

### Avoid code duplication

If you copy-paste more than 3 non-trivial lines, extract a shared function. After finishing a module, scan the codebase for duplication and consolidate.

**Why**: Duplicated logic diverges over time. When a bug is fixed in one copy but not another, equivalence tests (which are mandatory for all variants) catch the inconsistency, but the root cause — duplication — should have been eliminated earlier.

### Tuples limited to two items

Tuples must have at most two items. If you need more fields, declare a named type — a record, a struct record, or a discriminated union. Never use tuples-of-tuples, nested pairs, or anonymous grouping to circumvent this limit.

```fsharp
// Allowed — two items, positions are self-documenting
let (grammar, input) = parseArgs args
let result = List.fold (fun (count, sum) x -> (count + 1, sum + x)) (0, 0) items

// Prohibited — positions are opaque
let config = (42, true, "path", 3.14)
let value = ((a, b), (c, d))  // tuple-of-tuples workaround

// Correct — named type makes each field's meaning explicit
type Config = { Port: int; UseSsl: bool; RootPath: string; Timeout: float }
let config = { Port = 42; UseSsl = true; RootPath = "path"; Timeout = 3.14 }
```

**Why**: A tuple with three or more items loses positional meaning — readers must count positions to understand what each value represents. A named type (record or DU) gives each field a name, documents intent in the type signature, and is checked by the compiler (misspelled field names fail at build time, not silently at runtime). The FSharpLint rule `maxNumberOfItemsInTuple` enforces the limit automatically. Workarounds like tuples-of-tuples defeat the purpose — the positions are still anonymous, just nested.

### Compile-time safety over runtime checks

Prefer types that make illegal states unrepresentable. Use discriminated unions for mutually exclusive cases. Use `NonEmptyList`/`NonEmptySet` for non-empty collections. Use the type system to enforce constraints rather than `if`/`raise` guards.

**Why**: A constraint enforced by the type system is checked at compile time and documented in function signatures. A runtime check may be missed, may have the wrong error message, and is invisible in the type. For reference implementations that must be provably correct, compile-time enforcement is strictly stronger.

## See Also

- [Coding Conventions](coding-conventions.md)
- [Quality Standards](quality-standards.md)
- [Documentation Conventions](documentation-conventions.md)
