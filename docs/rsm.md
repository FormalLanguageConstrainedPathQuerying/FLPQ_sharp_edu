# RSM Module

## Module Purpose

Implements the Recursive State Machine (RSM) type as defined in the book (Chapter 6, `03_RecursiveAutomata.tex`). An RSM is a collection of deterministic finite automata (blocks), one per nonterminal, where transitions are labeled by either terminals (read input) or nonterminals (recursive call to another block).

## Type Definitions

### `RsmSymbol<'t, 'nt>`
```fsharp
[<RequireQualifiedAccess>]
type RsmSymbol<'t, 'nt when 't: comparison and 'nt: comparison> =
    | RTerm of Terminal<'t>
    | RNonterm of Nonterminal<'nt>
```
A transition label in an RSM block. Either a terminal (consuming an input character) or a nonterminal (recursive call to the block for that nonterminal).

### `RsmBlock<'t, 'nt>`
```fsharp
type RsmBlock<'t, 'nt when 't: comparison and 'nt: comparison> =
    { nonterminal: Nonterminal<'nt>
      dfa: DFA<RsmSymbol<'t, 'nt>, int> }
```
A single block in the RSM — a deterministic finite automaton for one nonterminal. The DFA's alphabet is `RsmSymbol<'t, 'nt>`, representing `Σ ∪ Q_S` from the book definition. States are simple integer indices.

### `RSM<'t, 'nt>`
```fsharp
type RSM<'t, 'nt when 't: comparison and 'nt: comparison> =
    { blocks: RsmBlock<'t, 'nt> list
      startBlock: Nonterminal<'nt> }
```
The Recursive State Machine tuple `⟨N, Σ, B, B_S, Q, Q_S⟩` from the book. `blocks` contains all blocks (one per nonterminal). `startBlock` identifies which block is the entry point.

## Function Signatures

### `blocks`
```fsharp
val blocks: RSM<'t, 'nt> -> RsmBlock<'t, 'nt> list
```
Returns all blocks in the RSM.

### `blockOf`
```fsharp
val blockOf: Nonterminal<'nt> -> RSM<'t, 'nt> -> RsmBlock<'t, 'nt> option
```
Finds a block by its nonterminal. Returns `None` if no such block exists.

### `startBlock`
```fsharp
val startBlock: RSM<'t, 'nt> -> RsmBlock<'t, 'nt>
```
Returns the start block of the RSM.

### `nonterminals`
```fsharp
val nonterminals: RSM<'t, 'nt> -> Nonterminal<'nt> list
```
Returns all nonterminals (one per block).

### `terminals`
```fsharp
val terminals: RSM<'t, 'nt> -> Terminal<'t> list
```
Returns all terminal symbols appearing in transitions across all blocks.

### `startStates`
```fsharp
val startStates: RSM<'t, 'nt> -> Set<int>
```
Returns `Q_S` — the set of start states across all blocks.

### `stateCount`
```fsharp
val stateCount: RSM<'t, 'nt> -> int
```
Returns the total number of states across all blocks.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Reuse existing `DFA<'t, 's>` type for blocks | Avoids duplicating automaton infrastructure; DFA already provides `alphabet`, `move`, `stateCount` |
| `RsmSymbol` as discriminated union | Cleanly represents `Σ ∪ Q_S` alphabet; comparison constraint enables DFA usage |
| Simple `int` states | Block states are simple indices; no need for named states |
| `startBlock` stored as `Nonterminal<'nt>` | Sufficient to identify the entry block; start block is found via `blockOf` |
| Accessors in `RSM` module | Consistent with project convention (types + module at same level) |
| Generic over terminal and nonterminal types | Enables reuse with different symbol types (not just strings) |

## Book Reference

Chapter 6, `03_RecursiveAutomata.tex`: Definition `def:rsm` — RSM is a tuple `⟨N, Σ, B, B_S, Q, Q_S⟩` where each block `B_{N_i}` is a deterministic finite automaton over alphabet `Σ ∪ Q_S`.
