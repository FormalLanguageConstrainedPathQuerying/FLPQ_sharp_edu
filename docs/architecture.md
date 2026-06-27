# Project Architecture

## Solution Structure

```
FLPQ.slnx
├── src/
│   └── FLPQ.Core/            # Core library with algorithms
│       └── Matrix.fs         # Generic matrix type and operations
└── tests/
    └── FLPQ.Core.Tests/      # Tests for core library
        └── MatrixTests.fs    # Property-based and unit tests for Matrix
```

## Projects

- **FLPQ.Core** — F# class library (net10.0). Implements algorithms from the book as reference implementations.
- **FLPQ.Core.Tests** — xUnit test project using FsCheck for property-based testing.

## Dependencies

- [FsCheck](https://fscheck.github.io/FsCheck/) — property-based testing
- [xUnit](https://xunit.net/) — unit testing framework
- [fantomas](https://fsprojects.github.io/fantomas/) — F# code formatter (local tool)
