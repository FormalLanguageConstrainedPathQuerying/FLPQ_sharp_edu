# Project Architecture

## Solution Structure

```
FLPQ.slnx
├── tools/                       # Auxiliary Python scripts for quality control
│   ├── detect_changes.py            # Detect projects with modified .fs files
│   ├── quality_check.py             # Commit gate: format + build
│   └── hard_gate.py                 # Full gate: format + build + tests + coverage + lint
├── src/
│   ├── FLPQ.LinearAlgebra/     # Linear algebra library
│   │   ├── Matrix.fs               # Generic matrix type and operations
│   │   ├── LinearAlgebra.fs        # Matrix multiplication and Kronecker product
│   │   └── BooleanDecomposition.fs # Boolean decomposition of set-valued matrices
│   ├── FLPQ.GraphAnalysis/     # Graph analysis library (depends on FLPQ.LinearAlgebra)
│   │   ├── Graph.fs                # Generic graph type and operations
│   │   └── MsBfs.fs                # MS-BFS and Boolean/Mask semiring operations
│   ├── FLPQ.Languages/         # Languages library (depends on FLPQ.LinearAlgebra, FLPQ.GraphAnalysis)
│   │   ├── Grammar.fs              # Grammar types, BNF parser, and CNF transformation
│   │   ├── Tokenizer.fs            # Common tokenizer for all parsing algorithms
│   │   ├── FirstFollow.fs          # First_k and follow_k computations
│   │   ├── Automaton.fs            # Generic finite automaton type and operations
│   │   ├── RSM.fs                  # Recursive State Machine type
│   │   ├── EbnfParser.fs           # EBNF parser and RSM construction via Brzozowski derivatives
│   │   ├── RsmToGrammar.fs         # RSM to BNF grammar conversion
│   │   ├── DerivationTree.fs       # Derivation tree type and operations
│   │   ├── VisualizationTypes.fs   # Shared visualization types
│   │   ├── GllTypes.fs             # GLL types (SPPF, GSS, path index entries)
│   │   ├── PathIndex.fs            # Path index (K×K matrix) for GLL/RNGLR recognized ranges
│   │   ├── Sppf.fs                 # SPPF construction from path index
│   │   ├── Gll.fs                  # GLL parsing algorithm for CFPQ on RSMs
│   │   ├── RnglrTypes.fs           # RNGLR types (GSS, LR table, items)
│   │   ├── RnglrLR.fs              # RNGLR LR(0) table construction from RSM
│   │   ├── Rnglr.fs                # RNGLR parsing algorithm for CFPQ on RSMs
│   │   ├── Cyk.fs                  # CYK parsing algorithm
│   │   ├── Valiant.fs              # Valiant parsing algorithm
│   │   ├── LLParser.fs             # LL(k) parsing table and parser
│   │   └── LRParser.fs             # LR(0)/SLR(1)/CLR(1) automata, tables, and parser
│   ├── FLPQ.Printers/          # Printers library (depends on FLPQ.LinearAlgebra, FLPQ.Languages)
│   │   ├── MatrixTeX.fs            # TeX rendering for matrices using nicematrix
│   │   ├── TeXRenderer.fs          # Shared TeX rendering for parser stacks/input
│   │   ├── DerivationTreeDot.fs    # Dot rendering for derivation trees
│   │   ├── InputGraphDot.fs        # Dot rendering for input graph
│   │   ├── BasicSppfDot.fs         # DOT rendering for basic (Rekers-style) SPPF
│   │   ├── BasicSppfTikz.fs        # TikZ rendering for basic (Rekers-style) SPPF
│   │   ├── AutomatonDot.fs         # Dot rendering for finite automata
│   │   ├── CykTeX.fs               # TeX rendering for CYK tables
│   │   ├── ValiantTeX.fs           # TeX rendering for Valiant trace steps
│   │   ├── LLTableTeX.fs           # TeX rendering for LL parsing tables
│   │   ├── LRTableTeX.fs           # TeX rendering for LR parsing tables
│   │   ├── LLStepVisualizer.fs     # LL parser step-by-step visualization
│   │   ├── LRStepVisualizer.fs     # LR parser step-by-step visualization
│   │   └── ExternalTools.fs        # Graphviz and lualatex wrappers (shared by CLI and tests)
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
    │   ├── RandomGraphGenerators.fs      # Random graph generators
    │   ├── GraphTests.fs                 # Tests for Graph module
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
    └── FLPQ.Printers.Tests/       # Tests for printers
        ├── ExternalToolsTests.fs        # Tests for Graphviz/lualatex wrappers
        ├── MatrixTeXTests.fs             # Tests for matrix TeX rendering
        ├── AutomatonVisualizationTests.fs # Tests for automata dot rendering
        ├── DerivationTreeVisualizationTests.fs # Tests for tree dot rendering
        ├── LLVisualizerTests.fs          # Tests for LL step visualization
        ├── LRVisualizerTests.fs          # Tests for LR step visualization
        ├── TexCompilationTests.fs        # TeX compilation tests for all printers
        └── CliSummaryTests.fs            # End-to-end tests for CLI summary generation
```

## Projects

- **FLPQ.LinearAlgebra** — F# class library (net10.0). Generic matrix operations, linear algebra, and boolean decomposition.
- **FLPQ.GraphAnalysis** — F# class library (net10.0). Generic graph type, MS-BFS, and Boolean/Mask semiring operations for graph traversal. Depends on `FLPQ.LinearAlgebra`.
- **FLPQ.Languages** — F# class library (net10.0). Grammar types, CNF transformation, parsing algorithms (CYK, Valiant, LL, LR), and finite automata. Depends on `FLPQ.LinearAlgebra` and `FLPQ.GraphAnalysis`.
- **FLPQ.RPQ** — F# class library (net10.0). Regular Path Querying algorithms (Belyanin, Arroyuelo, Kronecker) and graph reader. All accept graph as NFA. Depends on `FLPQ.LinearAlgebra`, `FLPQ.GraphAnalysis`, and `FLPQ.Languages`.
- **FLPQ.Printers** — F# class library (net10.0). TeX and Dot printing/visualization for matrices, automata, parsing tables, and algorithm steps. Also wraps Graphviz `dot` and `lualatex` invocations via `ExternalTools`. Depends on `FLPQ.LinearAlgebra` and `FLPQ.Languages`.
- **FLPQ.Cli** — F# console application (net10.0). Command-line interface for running parsing algorithms with optional summary PDF generation (`--summary`). Depends on `FLPQ.Languages` and `FLPQ.Printers`.
- **FLPQ.LinearAlgebra.Tests** — xUnit test project for linear algebra. Uses FsCheck for property-based testing.
- **FLPQ.GraphAnalysis.Tests** — xUnit test project for graph analysis. Uses FsCheck for property-based testing.
- **FLPQ.Languages.Tests** — xUnit test project for languages. Uses FsCheck for property-based testing. Depends on `FLPQ.Languages` (and transitively on `FLPQ.LinearAlgebra`).
- **FLPQ.RPQ.Tests** — xUnit test project for RPQ. Uses FsCheck for property-based testing. Depends on `FLPQ.RPQ`, `FLPQ.Languages`, `FLPQ.LinearAlgebra`, and `FLPQ.GraphAnalysis`.
- **FLPQ.Printers.Tests** — xUnit test project for printers. Depends on `FLPQ.Printers`, `FLPQ.Languages`, `FLPQ.LinearAlgebra`, and `FLPQ.Cli`.

## Dependencies

- [FsCheck](https://fscheck.github.io/FsCheck/) — property-based testing
- [xUnit](https://xunit.net/) — unit testing framework
- [fantomas](https://fsprojects.github.io/fantomas/) — F# code formatter (local tool)
- [nicematrix](https://ctan.org/pkg/nicematrix) — LaTeX package for matrix rendering

## Project Documentation

Each project has a hub documentation file grouping its modules. Design and logic of individual modules is documented in dedicated files linked from these hubs.

| Project | Hub Document |
|---------|-------------|
| FLPQ.LinearAlgebra | [FLPQ.LinearAlgebra.md](../developer/FLPQ.LinearAlgebra.md) |
| FLPQ.GraphAnalysis | [FLPQ.GraphAnalysis.md](../developer/FLPQ.GraphAnalysis.md) |
| FLPQ.Languages | [FLPQ.Languages.md](../developer/FLPQ.Languages.md) |
| FLPQ.RPQ | [FLPQ.RPQ.md](../developer/FLPQ.RPQ.md) |
| FLPQ.Printers | [FLPQ.Printers.md](../developer/FLPQ.Printers.md) |
| FLPQ.Cli | [FLPQ.Cli.md](../developer/FLPQ.Cli.md) |

See [main.md](../main.md) for the full documentation index including the flat module listing.

When adding a new module, create a corresponding `docs/developer/<module>.md` file and update the respective project hub.
