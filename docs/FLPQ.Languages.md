# FLPQ.Languages

Languages library providing grammar types, parsing algorithms, finite automata, and visualization. Depends on `FLPQ.LinearAlgebra`.

## Project

- **Type**: F# class library (`net10.0`)
- **Path**: `src/FLPQ.Languages/`
- **Dependencies**: `FLPQ.LinearAlgebra`, FSharpPlus

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

### Derivation Tree

| Module | Source | Documentation |
|--------|--------|---------------|
| `DerivationTree` | `DerivationTree.fs` | [DerivationTree module design and logic](derivation-tree.md) |

### Visualization

| Module | Source | Documentation |
|--------|--------|---------------|
| `VisualizationTypes` | `VisualizationTypes.fs` | [LL and LR steps visualization](visualization-types.md) |
| `AutomatonVisualizer` | `AutomatonVisualizer.fs` | [AutomatonVisualizer module design and logic](automaton-viz.md) |
| `DerivationTreeVisualizer` | `DerivationTreeVisualizer.fs` | [DerivationTreeVisualizer module design and logic](derivation-tree-viz.md) |
| `LLVisualizer` | `LLVisualizer.fs` | (see [visualization-types.md](visualization-types.md)) |
| `LRVisualizer` | `LRVisualizer.fs` | (see [visualization-types.md](visualization-types.md)) |

## Role

Central library for formal language processing:
- **Grammar types** — BNF grammar, CNF transformation, generic over terminal/nonterminal types
- **Parsing** — CYK, Valiant (standard and modified), LL(k), LR(0)/SLR(1)/CLR(1) with derivation tree construction
- **Lexing** — first_k, follow_k computations, tokenizer
- **Automata** — NFA/DFA (deterministic/non-deterministic separated at type level), RSM (Recursive State Machine)
- **EBNF** — EBNF grammar parsing via FParsec, DFA construction via Brzozowski derivatives, RSM to BNF conversion
- **Visualization** — dot (Graphviz) for automata and derivation trees, TeX (nicematrix) for parsing tables and step-by-step algorithm execution

## Book References

- Chapter 5: Finite automata, linear grammars
- Chapter 6: EBNF, Recursive State Machines
- Chapter 7: CYK, Valiant, LL(k), LR parsing
