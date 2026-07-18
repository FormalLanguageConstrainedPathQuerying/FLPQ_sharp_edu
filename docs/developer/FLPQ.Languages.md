# FLPQ.Languages

**Tags:** grammar, parsing, automaton, cfg, regular, derivation-tree, visualization, ll, lr, cyk, valiant, gll, rnglr, cfpq, tokenizer, sppf, path-index, gss, rsm, ebnf
**Kind:** hub
**Source:** `src/FLPQ.Languages/`
**Depends on:** FLPQ.LinearAlgebra, FLPQ.GraphAnalysis, FSharpPlus
**Used by:** FLPQ.RPQ, FLPQ.Printers, FLPQ.Cli
**Book reference:** Chapters 5, 6, 7

> **Abstract:** Central library for formal language processing: grammar types (BNF parsing, CNF transformation), parsing algorithms (CYK, Valiant standard/modified, LL(k), LR(0)/SLR(1)/CLR(1), GLL for CFPQ on RSMs, RNGLR), finite automata (NFA/DFA wrapping Graph, RSM), EBNF parsing (FParsec-based, Brzozowski derivatives), derivation trees, and parsing visualization data types. Also provides tokenizer, first/follow computation, path index, and SPPF.

## Contents

- [Project](#project)
- [Modules](#modules)
- [Role](#role)
- [Book References](#book-references)
- [See Also](#see-also)

## Project

- **Type**: F# class library (`net10.0`)
- **Path**: `src/FLPQ.Languages/`
- **Dependencies**: `FLPQ.LinearAlgebra`, `FLPQ.GraphAnalysis`, FSharpPlus

## Modules

### Grammar and Lexing

| Module | Source | Documentation |
|--------|--------|---------------|
| `Grammar` | `Grammar.fs` | [Grammar module design and logic](grammar.md) |
| `Tokenizer` | `Tokenizer.fs` | [Tokenizer module design and logic](tokenizer.md) |

### First/Follow

| Module | Source | Documentation |
|--------|--------|---------------|
| `FirstFollow` | `FirstFollow.fs` | [FirstFollow module design and logic](first-follow.md) |

### Automata

| Module | Source | Documentation |
|--------|--------|---------------|
| `Automaton` | `Automaton.fs` | [Automaton module design and logic](automaton.md) |
| `RSM` | `RSM.fs` | [RSM module design and logic](rsm.md) |

### EBNF Parsing

| Module | Source | Documentation |
|--------|--------|---------------|
| `EbnfParser` | `EbnfParser.fs` | [EBNF Parser module design and logic](ebnf-parser.md) |
| `RsmToGrammar` | `RsmToGrammar.fs` | [RSM to Grammar module design and logic](rsm-to-grammar.md) |

### Parsing Algorithms

| Module | Source | Documentation |
|--------|--------|---------------|
| `Cyk` | `Cyk.fs` | [CYK algorithm module design and logic](cyk.md) |
| `Valiant` | `Valiant.fs` | [Valiant algorithm module design and logic](valiant.md) |
| `LLParser` | `LLParser.fs` | [LL parser module design and logic](ll-parser.md) |
| `LRParser` | `LRParser.fs` | [LR parser module design and logic](lr-parser.md) |

### GLL / CFPQ

| Module | Source | Documentation |
|--------|--------|---------------|
| `GLL` | `GLL.fs` | [GLL parsing](gll.md) |
| `Sppf` | `Sppf.fs` | [SPPF module design and logic](sppf.md) |
| `PathIndex` | `PathIndex.fs` | [PathIndex module design and logic](path-index.md) |
| `Rnglr` | `Rnglr.fs` | [RNGLR parsing](rnglr.md) |

### Derivation Tree

| Module | Source | Documentation |
|--------|--------|---------------|
| `DerivationTree` | `DerivationTree.fs` | [DerivationTree module design and logic](derivation-tree.md) |

### Visualization

| Module | Source | Documentation |
|--------|--------|---------------|
| `VisualizationTypes` | `VisualizationTypes.fs` | [LL and LR steps visualization](visualization-types.md) |

## Role

Central library for formal language processing:
- **Grammar types** — BNF grammar, CNF transformation, generic over terminal/nonterminal types
- **Parsing** — CYK, Valiant (standard and modified), LL(k), LR(0)/SLR(1)/CLR(1) with derivation tree construction, GLL for CFPQ on RSMs
- **Lexing** — first_k, follow_k computations, tokenizer
- **Automata** — NFA/DFA (deterministic/non-deterministic separated at type level, wrapping `Graph` from `FLPQ.GraphAnalysis`), RSM (Recursive State Machine)
- **EBNF** — EBNF grammar parsing via FParsec, DFA construction via Brzozowski derivatives, RSM to BNF conversion
- **Path Index** — K×K matrix for recording recognized ranges during GLL parsing

## Book References

- Chapter 5: Finite automata, linear grammars
- Chapter 6: EBNF, Recursive State Machines
- Chapter 7: CYK, Valiant, LL(k), LR parsing

## See Also

- [FLPQ.Printers](FLPQ.Printers.md) — visualization output (TeX, Dot, Tikz)
- [FLPQ.RPQ](FLPQ.RPQ.md) — regular path querying algorithms
- [FLPQ.Cli](FLPQ.Cli.md) — CLI application for running algorithms
