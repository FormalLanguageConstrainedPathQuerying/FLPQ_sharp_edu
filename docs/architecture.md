# Project Architecture

## Solution Structure

```
FLPQ.slnx
├── src/
│   └── FLPQ.Core/            # Core library with algorithms
│       ├── Library.fs         # Namespace placeholder
│       ├── Matrix.fs          # Generic matrix type and operations
│       ├── LinearAlgebra.fs   # Matrix multiplication and Kronecker product
│       ├── Grammar.fs         # Grammar types, BNF parser, and CNF transformation
│       └── Cyk.fs             # CYK parsing algorithm
└── tests/
    └── FLPQ.Core.Tests/      # Tests for core library
        ├── Tests.fs               # Default test placeholder
        ├── MatrixTests.fs         # Property-based and unit tests for Matrix
        ├── LinearAlgebraTests.fs  # Property-based and unit tests for LinearAlgebra
        ├── GrammarTests.fs        # Unit tests for Grammar and CNF
        └── CykTests.fs            # Unit tests for CYK algorithm
```

## Projects

- **FLPQ.Core** — F# class library (net10.0). Implements algorithms from the book as reference implementations.
- **FLPQ.Core.Tests** — xUnit test project using FsCheck for property-based testing.

## Dependencies

- [FsCheck](https://fscheck.github.io/FsCheck/) — property-based testing
- [xUnit](https://xunit.net/) — unit testing framework
- [fantomas](https://fsprojects.github.io/fantomas/) — F# code formatter (local tool)
- [nicematrix](https://ctan.org/pkg/nicematrix) — LaTeX package for matrix rendering

## Module Documentation

Design and logic of each implemented module is documented in a dedicated file in `docs/`:

| File | Module |
|------|--------|
| [`docs/matrix.md`](matrix.md) | Matrix type, operations, and TeX printing |
| [`docs/linear-algebra.md`](linear-algebra.md) | Matrix multiplication and Kronecker product |
| [`docs/grammar.md`](grammar.md) | Grammar types, BNF parser, and CNF transformation |
| [`docs/cyk.md`](cyk.md) | CYK parsing algorithm |

When adding a new module, create a corresponding `docs/<module>.md` file following the same structure.
