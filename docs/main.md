# Documentation

**Tags:** hub, entry-point, navigation
**Kind:** hub

> **Abstract:** Root entry point for all project documentation. Links to project architecture, developer guides (coding conventions, design guides, quality standards, documentation conventions, reusing principles, tools), project hubs (FLPQ.LinearAlgebra, FLPQ.GraphAnalysis, FLPQ.Languages, FLPQ.RPQ, FLPQ.Printers, FLPQ.Cli), module index (all algorithm, data structure, utility, and visualization docs), and user documentation (CLI usage). Every documentation page in the project is reachable from here.

## Contents

- [Project Documentation](#project-documentation)
- [Developer Documentation](#developer-documentation)
- [User Documentation](#user-documentation)

## Project Documentation

How the project is organized, where each part is located, and why.

- [Project architecture](project/architecture.md) — solution structure, project organization, dependency graph
- [Third-party libraries](project/technologies.md) — external dependencies and their roles

## Developer Documentation

### Technical Guides

What our standards and principles are, and why we chose them.

- [Coding conventions](developer/guides/coding-conventions.md) — naming, genericity, functional style
- [Design guides](developer/guides/design-guides.md) — architecture principles, separation of concerns
- [Quality standards](developer/guides/quality-standards.md) — coverage, lint, formatting, equivalence testing
- [Documentation conventions](developer/guides/documentation-conventions.md) — module doc structure, decision docs, mapping table, review criteria
- [Reusing principles](developer/guides/reusing.md) — no duplicates, one source of truth, reuse checklist, patterns
- [Tools](developer/guides/tools.md) — auxiliary Python scripts for quality control

### Project Hubs

- [FLPQ.LinearAlgebra](developer/FLPQ.LinearAlgebra.md) — generic matrix types and linear algebra
- [FLPQ.GraphAnalysis](developer/FLPQ.GraphAnalysis.md) — MS-BFS and semiring operations
- [FLPQ.Languages](developer/FLPQ.Languages.md) — grammar, parsing, automata, visualization
- [FLPQ.RPQ](developer/FLPQ.RPQ.md) — regular path querying algorithms
- [FLPQ.Printers](developer/FLPQ.Printers.md) — TeX and Dot printers/visualizers
- [FLPQ.Cli](developer/FLPQ.Cli.md) — CLI console application

### Module Index

- [Matrix module](developer/matrix.md)
- [LinearAlgebra module](developer/linear-algebra.md)
- [BooleanDecomposition module](developer/boolean-decomposition.md)
- [Graph module](developer/graph.md)
- [MS-BFS and matrix operations module](developer/msbfs.md)
- [Grammar module](developer/grammar.md)
- [Tokenizer module](developer/tokenizer.md)
- [CYK algorithm](developer/cyk.md)
- [Valiant algorithm](developer/valiant.md)
- [FirstFollow module](developer/first-follow.md)
- [Automaton module](developer/automaton.md)
- [RSM module](developer/rsm.md)
- [EBNF Parser module](developer/ebnf-parser.md)
- [RSM to Grammar module](developer/rsm-to-grammar.md)
- [GLL parsing](developer/gll.md)
- [SPPF module](developer/sppf.md)
- [PathIndex module](developer/path-index.md)
- [RNGLR parsing](developer/rnglr.md)
- [Graph Reader module](developer/graph-reader.md)
- [Belyanin RPQ module](developer/belyanin-rpq.md)
- [Arroyuelo RPQ module](developer/arroyuelo-rpq.md)
- [Kronecker RPQ module](developer/kronecker-rpq.md)
- [DerivationTree module](developer/derivation-tree.md)
- [LL parser module](developer/ll-parser.md)
- [LR parser module](developer/lr-parser.md)
- [Automaton visualization: Dot, Tikz, LR automata Tikz](developer/automaton-viz.md)
- [DerivationTreeDot module](developer/derivation-tree-viz.md)
- [LL and LR steps visualization](developer/visualization-types.md)
- [GrammarTeX module](developer/grammar-tex.md)
- [InputGraphDot module](developer/input-graph-dot.md)
- [SummaryTeX module](developer/summary-tex.md)
- [ExternalTools module](developer/external-tools.md)

## User Documentation

What you need to know to use this project.

- [CLI console application](user/cli.md)
