# Project Architecture

## Solution Structure

```
FLPQ.slnx
├── src/
│   └── FLPQ.Core/            # Core library with algorithms
│       ├── Library.fs        # Namespace placeholder
│       └── Matrix.fs         # Generic matrix type and operations
└── tests/
    └── FLPQ.Core.Tests/      # Tests for core library
        ├── Tests.fs          # Default test placeholder
        └── MatrixTests.fs    # Property-based and unit tests for Matrix
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

When adding a new module, create a corresponding `docs/<module>.md` file following the same structure.
