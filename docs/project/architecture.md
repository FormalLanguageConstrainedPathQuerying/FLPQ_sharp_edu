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
│   ├── FLPQ.Printers/          # Printers library (depends on FLPQ.LinearAlgebra, FLPQ.Languages, FLPQ.RPQ, FLPQ.CFPQ)
│   │   ├── MatrixTeX.fs            # TeX rendering for matrices using nicematrix
│   │   ├── TeXRenderer.fs          # Shared TeX rendering for parser stacks/input
│   │   ├── DerivationTreeDot.fs    # Dot rendering for derivation trees
│   │   ├── InputGraphDot.fs        # Dot rendering for input graph
│   │   ├── BasicSppfDot.fs         # DOT rendering for basic (Rekers-style) SPPF
│   │   ├── BasicSppfTikz.fs        # TikZ rendering for basic (Rekers-style) SPPF
│   │   ├── AutomatonDot.fs         # Dot rendering for finite automata
│   │   ├── RegexpTreeDot.fs        # DOT rendering of the RPQ regexp AST tree (vertical)
│   │   ├── RegexpTreeTikz.fs       # TikZ rendering of the RPQ regexp AST tree (vertical)
│   │   ├── RpqGraphDot.fs          # RPQ-dedicated DOT rendering of the input graph
│   │   ├── RpqGraphTikz.fs         # RPQ-dedicated TikZ rendering of the input graph
│   │   ├── RpqGraphViz.fs          # Shared RPQ input-graph rendering with path highlights
│   │   ├── StepTeX.fs              # Generic RPQ step TeX helpers (adjustbox, formula)
│   │   ├── ArroyueloStepCommon.fs  # Shared Arroyuelo step record, tree model, pipeline
│   │   ├── ArroyueloSimplePathStepVisualizer.fs  # Arroyuelo simple-path step visualization
│   │   ├── ArroyueloReachabilityStepVisualizer.fs  # Arroyuelo Boolean reachability step visualization
│   │   ├── BelyaninStepCommon.fs   # Shared Belyanin step record types and helpers
│   │   ├── BelyaninSimplePathStepVisualizer.fs  # Belyanin simple-path step visualization
│   │   ├── BelyaninReachabilityStepVisualizer.fs  # Belyanin Boolean reachability step visualization
│   │   ├── CfpqMatrixTeX.fs        # TeX rendering of Azimov's per-nonterminal CFPQ matrices
│   │   ├── CykTeX.fs               # TeX rendering for CYK tables
│   │   ├── ValiantTeX.fs           # TeX rendering for Valiant trace steps
│   │   ├── LLTableTeX.fs           # TeX rendering for LL parsing tables
│   │   ├── LRTableTeX.fs           # TeX rendering for LR parsing tables
│   │   ├── LLStepVisualizer.fs     # LL parser step-by-step visualization
│   │   ├── LRStepVisualizer.fs     # LR parser step-by-step visualization
│   │   └── ExternalTools.fs        # Graphviz and lualatex wrappers (shared by CLI and tests)
│   └── FLPQ.RPQ/               # RPQ algorithms (depends on FLPQ.LinearAlgebra, FLPQ.GraphAnalysis, FLPQ.Languages)
 │       ├── GraphReader.fs          # Graph file reading, returns graph as NFA
 │       ├── RpqInput.fs             # RPQ query regexp parsing from EBNF
 │       ├── PathSemiring.fs         # Path semiring over sets of vertex sequences
  │       ├── BelyaninRPQ.fs          # Belyanin's LARPQ algorithm (BFS-based RPQ)
  │       ├── ArroyueloRPQ.fs         # Arroyuelo's matrix-based RPQ algorithm
  │       └── KroneckerRPQ.fs         # Kronecker product-based RPQ with MS-BFS filtering
  │   ├── FLPQ.CFPQ/              # CFPQ algorithms (depends on FLPQ.LinearAlgebra, FLPQ.Languages)
  │   │   ├── CfpqInput.fs            # MatrixMarket graph, .cnf grammar, and results.mtx parsing
  │   │   ├── CfpqSemiring.fs         # Reachability, singlePath, and allPaths semirings
  │   │   └── AzimovCfpq.fs           # Azimov's matrix-based CFPQ (generic fixed point, path extraction)
  │   └── FLPQ.Cli/                   # Console application (depends on FLPQ.Languages, FLPQ.Printers, FLPQ.RPQ, FLPQ.CFPQ)
 │       ├── Program.fs              # Entry point and algorithm dispatch
 │       ├── AlgorithmTypes.fs       # Algorithm DU and Argu argument type
 │       ├── Helpers.fs              # Output file writing and template lookup
 │       ├── CykRunner.fs            # CYK CLI runner
 │       ├── ValiantRunner.fs        # Valiant (standard/modified) CLI runner
 │       ├── LLRunner.fs             # LL(k) CLI runner
 │       ├── LRRunner.fs             # LR(0)/SLR(1)/CLR(1) CLI runner
 │       ├── GllRunner.fs            # GLL CLI runner
 │       ├── RnglrRunner.fs          # RNGLR CLI runner
  │       ├── ArroyueloSimplePathRunner.fs  # Arroyuelo RPQ simplePath-semantics CLI runner
  │       ├── ArroyueloReachabilityRunner.fs # Arroyuelo RPQ reachability-semantics CLI runner (default)
   │       ├── BelyaninSimplePathRunner.fs  # Belyanin RPQ simplePath-semantics CLI runner
   │       ├── BelyaninReachabilityRunner.fs # Belyanin RPQ reachability-semantics CLI runner (default)
   │       ├── AzimovCfpqReachabilityRunner.fs # Azimov CFPQ reachability-semantics CLI runner (default)
   │       ├── AzimovCfpqSinglePathRunner.fs # Azimov CFPQ simplePath-semantics CLI runner
   │       ├── AzimovCfpqAllPathsRunner.fs # Azimov CFPQ allPaths-semantics CLI runner
   │       └── Summary.fs              # Merged TeX summary generation (--summary)
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
     │   ├── RegexpToDfaTests.fs           # Tests for Regexp.toDfa (derivative DFA construction)
     │   ├── RsmToGrammarTests.fs          # Tests for RSM to grammar conversion
    │   ├── LLParserTests.fs              # Tests for LL parser
    │   └── LRParserTests.fs              # Tests for LR parser
     ├── FLPQ.RPQ.Tests/            # Tests for RPQ algorithms
         ├── RPQTests.fs                   # Tests for RPQ algorithms (Belyanin, Arroyuelo, Kronecker)
         ├── RpqInputTests.fs              # Tests for RPQ query regexp parsing
         └── PathSemiringTests.fs          # Tests for the path semiring
     ├── FLPQ.CFPQ.Tests/           # Tests for CFPQ algorithms
         ├── AzimovCfpqBookExampleTests.fs # Golden test of the book's worked example (T0..T13)
         ├── CfpqInputTests.fs             # Tests for graph/.cnf/results parsing
         ├── CfpqSemiringTests.fs          # Semiring unit tests
         ├── CfpqVsRpqPropertyTests.fs     # CFPQ reachability equals all-pairs RPQ
         ├── CfpqVsValiantPropertyTests.fs # Azimov equals Valiant's full parsing table
         ├── CfpqReferenceTests.fs         # Reference tests against the flpq-data export
         └── CfpqTestHelpers.fs            # Shared test helpers
    ├── FLPQ.Printers.Tests/       # Tests for printers
        ├── ExternalToolsTests.fs        # Tests for Graphviz/lualatex wrappers
        ├── MatrixTeXTests.fs             # Tests for matrix TeX rendering
        ├── AutomatonVisualizationTests.fs # Tests for automata dot rendering
        ├── DerivationTreeVisualizationTests.fs # Tests for tree dot rendering
        ├── LLVisualizerTests.fs          # Tests for LL step visualization
        ├── LRVisualizerTests.fs          # Tests for LR step visualization
        ├── TexCompilationTests.fs        # TeX compilation tests for all printers
        └── CliSummaryTests.fs            # End-to-end tests for CLI summary generation
     ├── FLPQ.Cli.Tests/            # Tests for the CLI runners and dispatch
         ├── AlgorithmTypesTests.fs          # Algorithm/semantics DU and Argu parsing
         ├── CykRunnerTests.fs               # CYK runner tests
         ├── ValiantRunnerTests.fs           # Valiant runner tests
         ├── LLRunnerTests.fs                # LL(k) runner tests
         ├── LRRunnerTests.fs                # LR runner tests
         ├── GllRunnerTests.fs               # GLL runner tests
         ├── RnglrRunnerTests.fs             # RNGLR runner tests
         ├── ArroyueloSimplePathRunnerTests.fs  # Arroyuelo RPQ simplePath runner tests
         ├── ArroyueloReachabilityRunnerTests.fs # Arroyuelo RPQ reachability runner tests
         ├── BelyaninSimplePathRunnerTests.fs  # Belyanin RPQ simplePath runner tests
         ├── BelyaninReachabilityRunnerTests.fs # Belyanin RPQ reachability runner tests
         ├── AzimovCfpqCliTests.fs           # Azimov CFPQ runner tests (all three semantics)
         ├── ProgramDispatchTests.fs         # Program dispatch tests
         ├── SummaryTests.fs                 # Merged summary generation tests
         ├── CliSummaryTests.fs              # End-to-end CLI summary tests
         ├── ErrorPathTests.fs               # Error-path tests
         ├── HelpersTests.fs                 # Helpers tests
         ├── RunnerTestHelpers.fs            # Shared runner test helpers
         └── TestGrammarFiles.fs             # Test grammar file fixtures
     └── FLPQ.TestUtilities/        # Shared test utilities (generators, fixtures, helpers)
         ├── Generators.fs                   # FsCheck generators
         ├── LanguageRegistry.fs             # Catalog of test languages, grammars, strings
         ├── LrConflictFixture.fs            # LR conflict fixture
         ├── ParsingTestCases.fs             # Shared parsing test cases
         ├── PathIndexFixtures.fs            # Path index fixtures
         ├── RsmFixtures.fs                  # RSM fixtures
         └── TestHelpers.fs                  # Shared test helpers (dot/TeX compilation)
```

## Projects

- **FLPQ.LinearAlgebra** — F# class library (net10.0). Generic matrix operations, linear algebra, and boolean decomposition.
- **FLPQ.GraphAnalysis** — F# class library (net10.0). Generic graph type, MS-BFS, and Boolean/Mask semiring operations for graph traversal. Depends on `FLPQ.LinearAlgebra`.
- **FLPQ.Languages** — F# class library (net10.0). Grammar types, CNF transformation, parsing algorithms (CYK, Valiant, LL, LR), and finite automata. Depends on `FLPQ.LinearAlgebra` and `FLPQ.GraphAnalysis`.
- **FLPQ.RPQ** — F# class library (net10.0). Regular Path Querying algorithms (Belyanin, Arroyuelo, Kronecker) and graph reader. All accept graph as NFA. Depends on `FLPQ.LinearAlgebra`, `FLPQ.GraphAnalysis`, and `FLPQ.Languages`.
- **FLPQ.CFPQ** — F# class library (net10.0). Context-Free Path Querying: Azimov's matrix-based algorithm as a generic semiring-parametrized core (reachability, singlePath, allPaths), plus parsing of the flpq-data input formats (MatrixMarket graph directories, `.cnf` grammars, reference results). Depends on `FLPQ.LinearAlgebra` and `FLPQ.Languages`.
- **FLPQ.Printers** — F# class library (net10.0). TeX and Dot printing/visualization for matrices, automata, parsing tables, and algorithm steps. Also wraps Graphviz `dot` and `lualatex` invocations via `ExternalTools`. Depends on `FLPQ.LinearAlgebra`, `FLPQ.Languages`, `FLPQ.RPQ`, and `FLPQ.CFPQ`.
- **FLPQ.Cli** — F# console application (net10.0). Command-line interface for running parsing algorithms, RPQ algorithms (Arroyuelo, Belyanin), and CFPQ (Azimov) with optional summary PDF generation (`--summary`). The path-query algorithms select their semantics with `--semantics`: Arroyuelo/Belyanin RPQ take `reachability` (default) or `simplePath`; Azimov CFPQ takes `reachability` (default), `simplePath`, or `allPaths` (CFPQ-only). Depends on `FLPQ.Languages`, `FLPQ.Printers`, `FLPQ.RPQ`, and `FLPQ.CFPQ`.
- **FLPQ.LinearAlgebra.Tests** — xUnit test project for linear algebra. Uses FsCheck for property-based testing.
- **FLPQ.GraphAnalysis.Tests** — xUnit test project for graph analysis. Uses FsCheck for property-based testing.
- **FLPQ.Languages.Tests** — xUnit test project for languages. Uses FsCheck for property-based testing. Depends on `FLPQ.Languages` (and transitively on `FLPQ.LinearAlgebra`).
- **FLPQ.RPQ.Tests** — xUnit test project for RPQ. Uses FsCheck for property-based testing. Depends on `FLPQ.RPQ`, `FLPQ.Languages`, `FLPQ.LinearAlgebra`, and `FLPQ.GraphAnalysis`.
- **FLPQ.CFPQ.Tests** — xUnit test project for CFPQ. Uses FsCheck for property-based testing (CFPQ-vs-RPQ, Azimov-vs-Valiant equivalence) plus golden and flpq-data reference tests. Depends on `FLPQ.CFPQ`, `FLPQ.Languages`, `FLPQ.RPQ`, `FLPQ.LinearAlgebra`, and `FLPQ.TestUtilities`.
- **FLPQ.Printers.Tests** — xUnit test project for printers. Depends on `FLPQ.Printers`, `FLPQ.Languages`, `FLPQ.LinearAlgebra`, and `FLPQ.Cli`.
- **FLPQ.Cli.Tests** — xUnit test project for the CLI runners, dispatch, and summary generation. Depends on `FLPQ.Cli`, `FLPQ.Languages`, `FLPQ.Printers`, `FLPQ.RPQ`, `FLPQ.CFPQ`, and `FLPQ.TestUtilities`.
- **FLPQ.TestUtilities** — shared test utilities: FsCheck generators, the language registry (test languages/grammars/strings), and fixtures.

## Dependencies

- [FsCheck](https://fscheck.github.io/FsCheck/) — property-based testing
- [xUnit](https://xunit.net/) — unit testing framework
- [fantomas](https://fsprojects.github.io/fantomas/) — F# code formatter (local tool)
- [nicematrix](https://ctan.org/pkg/nicematrix) — LaTeX package for matrix rendering

## Project Documentation

Each project has a hub documentation file grouping its modules. Design and logic of individual modules is documented in dedicated files linked from these hubs.

| Project | Hub Document |
| --- | --- |
| FLPQ.LinearAlgebra | [FLPQ.LinearAlgebra.md](../developer/FLPQ.LinearAlgebra.md) |
| FLPQ.GraphAnalysis | [FLPQ.GraphAnalysis.md](../developer/FLPQ.GraphAnalysis.md) |
| FLPQ.Languages | [FLPQ.Languages.md](../developer/FLPQ.Languages.md) |
| FLPQ.RPQ | [FLPQ.RPQ.md](../developer/FLPQ.RPQ.md) |
| FLPQ.CFPQ | [FLPQ.CFPQ.md](../developer/FLPQ.CFPQ.md) |
| FLPQ.Printers | [FLPQ.Printers.md](../developer/FLPQ.Printers.md) |
| FLPQ.Cli | [FLPQ.Cli.md](../developer/FLPQ.Cli.md) |

See [main.md](../main.md) for the full documentation index including the flat module listing.

When adding a new module, create a corresponding `docs/developer/<module>.md` file and update the respective project hub.
