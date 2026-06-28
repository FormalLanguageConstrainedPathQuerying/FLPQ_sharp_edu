# Project Architecture

## Solution Structure

```
FLPQ.slnx
├── src/
│   └── FLPQ.Core/            # Core library with algorithms
│       ├── Library.fs         # Namespace placeholder
│       ├── Matrix.fs          # Generic matrix type and operations
│       ├── LinearAlgebra.fs   # Matrix multiplication and Kronecker product
│       ├── Grammar.fs              # Grammar types, BNF parser, and CNF transformation
│       ├── Cyk.fs                  # CYK parsing algorithm
│       ├── BooleanDecomposition.fs # Boolean decomposition of set-valued matrices
│       ├── FirstFollow.fs          # First_k and follow_k computations
│       ├── Automaton.fs            # Generic finite automaton type and operations
│       ├── LLParser.fs             # LL(k) parsing table and parser
│       ├── LRParser.fs             # LR(0)/SLR(1)/CLR(1) automata, tables, and parser
│       └── Library.fs              # Namespace placeholder
└── tests/
    └── FLPQ.Core.Tests/      # Tests for core library
        ├── TestGrammars.fs               # Shared pre-parsed grammars and generators
        ├── Tests.fs                      # Default test placeholder
        ├── MatrixTests.fs                # Property-based and unit tests for Matrix
        ├── LinearAlgebraTests.fs         # Property-based and unit tests for LinearAlgebra
        ├── GrammarTests.fs               # Unit tests for Grammar and CNF
        ├── CykTests.fs                   # Unit tests for CYK algorithm
        ├── BooleanDecompositionTests.fs  # Unit tests for BooleanDecomposition
        ├── FirstFollowTests.fs           # Tests for FirstFollow
        ├── AutomatonTests.fs             # Tests for Automaton
        ├── LLParserTests.fs              # Tests for LL parser
        ├── LRParserTests.fs              # Tests for LR parser
        └── ValiantTests.fs               # Unit and property tests for Valiant
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
| [`docs/boolean-decomposition.md`](boolean-decomposition.md) | Boolean decomposition of set-valued matrices |
| [`docs/cyk.md`](cyk.md) | CYK parsing algorithm |
| [`docs/valiant.md`](valiant.md) | Valiant parsing algorithm |
| [`docs/lr-parser.md`](lr-parser.md) | LR automata, table construction, and parser |

When adding a new module, create a corresponding `docs/<module>.md` file following the same structure.
