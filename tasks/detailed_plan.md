# Detailed Plan: Task 53 — RSM (Recursive State Machine) Type

## Overview
Implement the RSM type from Book Chapter 6, `03_RecursiveAutomata.tex`. An RSM is a collection of deterministic finite automata (blocks), one per nonterminal, where transitions are labeled by terminals (read input) or start states of other blocks (recursive call).

## Design Decisions

### 1. RSM Transition Symbol Type
`RsmSymbol<'t, 'nt>` — discriminated union: either `RsmTerm of Terminal<'t>` or `RsmNonterm of Nonterminal<'nt>`.
- Must support `comparison` (required by DFA type constraint)
- Nonterminal references map to start states of other blocks via the RSM's block mapping

### 2. Block Type
Reuse existing `DFA<'t, 's>` type. For each block:
- `'t` = `RsmSymbol<'t, 'nt>` (transition labels)
- `'s` = `int` (simple state indices)
- Block wraps the DFA with its corresponding nonterminal

### 3. RSM Type
Contains list of blocks, start block nonterminal, terminal set, nonterminal set, and start state set Q_S.

### 4. Accessor Functions
- `blocks`: list all blocks
- `blockOf`: get block by nonterminal
- `startBlock`: get the start block
- `terminals`: list all terminals
- `nonterminals`: list all nonterminals
- `startStates`: set of all block start states (Q_S)

## Files to Create/Modify
1. `src/FLPQ.Languages/RSM.fs` — new module with RSM type and accessors
2. `src/FLPQ.Languages/FLPQ.Languages.fsproj` — add Compile entry
3. `tests/FLPQ.Languages.Tests/RSMTests.fs` — tests
4. `docs/rsm.md` — documentation

## Implementation Steps
1. Define `RsmSymbol` type
2. Define `RsmBlock` type
3. Define `RSM` type
4. Implement accessor functions
5. Write tests (construction, accessors)
6. Update documentation
