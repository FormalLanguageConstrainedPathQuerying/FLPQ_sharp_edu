# Detailed Plan: Task 128 — Property-Based Equivalence Tests

## Goal
Add property-based equivalence tests for:
1. `toCnf` language preservation
2. `FirstFollow` correctness against brute-force derivation
3. NFA→DFA language preservation
4. `AutomatonDot` output parseability
5. `RsmBuilder` output computability
6. Fix `BooleanDecompositionTests` property test

## Steps

### 1. toCnf language preservation (GrammarTests.fs)
- Generate random grammars using FsCheck
- Convert to CNF using `Grammar.toCnf`
- Generate random strings up to length N
- Check same strings are accepted by both original and CNF grammars
- Use CYK for checking acceptance (ensures correctness)

### 2. FirstFollow correctness (FirstFollowTests.fs)
- Generate random grammars
- Compute FIRST sets using `FirstFollow.firstK`
- Verify against brute-force: enumerate all derivable prefixes up to length k
- Verify FOLLOW: enumerate all strings where nonterminal appears in sentential form

### 3. NFA→DFA language preservation (AutomatonTests.fs)
- Generate random NFAs using FsCheck
- Convert to DFA using `Automaton.toDfa`
- Generate random strings over NFA alphabet
- Check both NFA and DFA give same accept/reject results

### 4. AutomatonDot output parseability (AutomatonVisualizationTests.fs)
- Generate random NFAs/DFAs using FsCheck
- Render to DOT using `AutomatonDot`
- Verify DOT output is syntactically valid (balanced braces, correct keyword usage)

### 5. RsmBuilder output computability (RSMTests.fs)
- Generate random EBNF-like RSM text
- Build RSM using `RsmBuilder.buildRSMFromText`
- Verify structure: blocks match input, DFAs are deterministic, etc.

### 6. Fix BooleanDecompositionTests property test
- Add assertion that non-empty matrix input produces non-empty decomposition Map

## Files to modify
- `tests/FLPQ.Languages.Tests/GrammarTests.fs` — toCnf preservation
- `tests/FLPQ.Languages.Tests/FirstFollowTests.fs` — FirstFollow correctness
- `tests/FLPQ.Languages.Tests/AutomatonTests.fs` — NFA→DFA preservation
- `tests/FLPQ.Printers.Tests/AutomatonVisualizationTests.fs` — AutomatonDot parseability
- `tests/FLPQ.Languages.Tests/RSMTests.fs` — RsmBuilder computability
- `tests/FLPQ.LinearAlgebra.Tests/BooleanDecompositionTests.fs` — fix property test
