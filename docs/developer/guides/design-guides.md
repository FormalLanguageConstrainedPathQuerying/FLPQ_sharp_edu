# Design Guides

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

### Compile-time safety over runtime checks

Prefer types that make illegal states unrepresentable. Use discriminated unions for mutually exclusive cases. Use `NonEmptyList`/`NonEmptySet` for non-empty collections. Use the type system to enforce constraints rather than `if`/`raise` guards.

**Why**: A constraint enforced by the type system is checked at compile time and documented in function signatures. A runtime check may be missed, may have the wrong error message, and is invisible in the type. For reference implementations that must be provably correct, compile-time enforcement is strictly stronger.
