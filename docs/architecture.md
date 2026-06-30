# Project Architecture

## Solution Structure

```
FLPQ.slnx
├── src/
│   ├── FLPQ.LinearAlgebra/     # Linear algebra library
│   │   ├── Matrix.fs               # Generic matrix type and operations
│   │   ├── LinearAlgebra.fs        # Matrix multiplication and Kronecker product
│   │   └── BooleanDecomposition.fs # Boolean decomposition of set-valued matrices
│   ├── FLPQ.GraphAnalysis/     # Graph analysis library (depends on FLPQ.LinearAlgebra)
│   │   └── MsBfs.fs                # MS-BFS and Boolean/Mask semiring operations
│   ├── FLPQ.Languages/         # Languages library (depends on FLPQ.LinearAlgebra)
│   │   ├── Grammar.fs              # Grammar types, BNF parser, and CNF transformation
│   │   ├── Tokenizer.fs            # Common tokenizer for all parsing algorithms
│   │   ├── FirstFollow.fs          # First_k and follow_k computations
│   │   ├── Automaton.fs            # Generic finite automaton type and operations
│   │   ├── RSM.fs                  # Recursive State Machine type
│   │   ├── EbnfParser.fs           # EBNF parser and RSM construction via Brzozowski derivatives
│   │   ├── RsmToGrammar.fs         # RSM to BNF grammar conversion
│   │   ├── DerivationTree.fs       # Derivation tree type and operations
│   │   ├── VisualizationTypes.fs   # Shared visualization types
│   │   ├── DerivationTreeVisualizer.fs
│   │   ├── Cyk.fs                  # CYK parsing algorithm
│   │   ├── Valiant.fs              # Valiant parsing algorithm
│   │   ├── LLParser.fs             # LL(k) parsing table and parser
│   │   └── LRParser.fs             # LR(0)/SLR(1)/CLR(1) automata, tables, and parser
│   └── FLPQ.RPQ/               # RPQ algorithms (depends on FLPQ.LinearAlgebra, FLPQ.GraphAnalysis, FLPQ.Languages)
│       ├── GraphReader.fs          # Graph file reading, returns graph as NFA
│       ├── BelyaninRPQ.fs          # Belyanin's LARPQ algorithm (BFS-based RPQ)
│       ├── ArroyueloRPQ.fs         # Arroyuelo's matrix-based RPQ algorithm
│       └── KroneckerRPQ.fs         # Kronecker product-based RPQ with MS-BFS filtering
└── tests/
    ├── FLPQ.LinearAlgebra.Tests/  # Tests for linear algebra
    │   ├── MatrixTests.fs                # Property-based and unit tests for Matrix
    │   ├── LinearAlgebraTests.fs         # Property-based and unit tests for LinearAlgebra
    │   └── BooleanDecompositionTests.fs  # Unit and property tests for BooleanDecomposition
    ├── FLPQ.GraphAnalysis.Tests/  # Tests for graph analysis
    │   └── MsBfsTests.fs                 # MS-BFS and Boolean/Mask semiring tests
    ├── FLPQ.Languages.Tests/      # Tests for languages
    │   ├── TestUtils.fs                  # Shared test utilities (dot/TeX compilation)
    │   ├── TestGrammars.fs               # Shared pre-parsed grammars and generators
    │   ├── GrammarTests.fs               # Unit tests for Grammar and CNF
    │   ├── CykTests.fs                   # Unit tests for CYK algorithm
    │   ├── ValiantTests.fs               # Unit and property tests for Valiant
    │   ├── FirstFollowTests.fs           # Tests for FirstFollow
    │   ├── AutomatonTests.fs             # Tests for Automaton
    │   ├── RSMTests.fs                   # Tests for RSM type
    │   ├── EbnfParserTests.fs            # Tests for EBNF parser
    │   ├── RsmToGrammarTests.fs          # Tests for RSM to grammar conversion
    │   ├── LLParserTests.fs              # Tests for LL parser
    │   └── LRParserTests.fs              # Tests for LR parser
    └── FLPQ.RPQ.Tests/            # Tests for RPQ algorithms
        └── RPQTests.fs                   # Tests for RPQ algorithms (Belyanin, Arroyuelo, Kronecker)
```

## Projects

- **FLPQ.LinearAlgebra** — F# class library (net10.0). Generic matrix operations, linear algebra, and boolean decomposition.
- **FLPQ.GraphAnalysis** — F# class library (net10.0). MS-BFS and Boolean/Mask semiring operations for graph traversal. Depends on `FLPQ.LinearAlgebra`.
- **FLPQ.Languages** — F# class library (net10.0). Grammar types, CNF transformation, parsing algorithms (CYK, Valiant, LL, LR), and finite automata. Depends on `FLPQ.LinearAlgebra`.
- **FLPQ.RPQ** — F# class library (net10.0). Regular Path Querying algorithms (Belyanin, Arroyuelo, Kronecker) and graph reader. All accept graph as NFA. Depends on `FLPQ.LinearAlgebra`, `FLPQ.GraphAnalysis`, and `FLPQ.Languages`.
- **FLPQ.LinearAlgebra.Tests** — xUnit test project for linear algebra. Uses FsCheck for property-based testing.
- **FLPQ.GraphAnalysis.Tests** — xUnit test project for graph analysis. Uses FsCheck for property-based testing.
- **FLPQ.Languages.Tests** — xUnit test project for languages. Uses FsCheck for property-based testing. Depends on `FLPQ.Languages` (and transitively on `FLPQ.LinearAlgebra`).
- **FLPQ.RPQ.Tests** — xUnit test project for RPQ. Uses FsCheck for property-based testing. Depends on `FLPQ.RPQ`, `FLPQ.Languages`, `FLPQ.LinearAlgebra`, and `FLPQ.GraphAnalysis`.

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
| [`docs/boolean-decomposition.md`](boolean-decomposition.md) | Boolean decomposition of set-valued matrices |
| [`docs/msbfs.md`](msbfs.md) | MS-BFS and Boolean/Mask semiring operations |
| [`docs/grammar.md`](grammar.md) | Grammar types, BNF parser, and CNF transformation |
| [`docs/tokenizer.md`](tokenizer.md) | Common tokenizer for all parsing algorithms |
| [`docs/first-follow.md`](first-follow.md) | First_k and follow_k computations |
| [`docs/automaton.md`](automaton.md) | Generic finite automaton type and operations |
| [`docs/rsm.md`](rsm.md) | Recursive State Machine type |
| [`docs/ebnf-parser.md`](ebnf-parser.md) | EBNF parser and RSM construction |
| [`docs/rsm-to-grammar.md`](rsm-to-grammar.md) | RSM to BNF grammar conversion |
| [`docs/derivation-tree.md`](derivation-tree.md) | Derivation tree type and leaf collection |
| [`docs/cyk.md`](cyk.md) | CYK parsing algorithm |
| [`docs/valiant.md`](valiant.md) | Valiant parsing algorithm |
| [`docs/ll-parser.md`](ll-parser.md) | LL(k) parsing table construction and parser |
| [`docs/lr-parser.md`](lr-parser.md) | LR automata, table construction, and parser |
| [`docs/graph-reader.md`](graph-reader.md) | Graph file reading |
| [`docs/belyanin-rpq.md`](belyanin-rpq.md) | Belyanin's RPQ algorithm |
| [`docs/arroyuelo-rpq.md`](arroyuelo-rpq.md) | Arroyuelo's RPQ algorithm |
| [`docs/kronecker-rpq.md`](kronecker-rpq.md) | Kronecker-based RPQ algorithm |

When adding a new module, create a corresponding `docs/<module>.md` file following the same structure.
