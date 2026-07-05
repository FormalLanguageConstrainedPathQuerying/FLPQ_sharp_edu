# Detailed Plan: Task 115 — RPQ Cross-Algorithm Property Tests with Complex Regex

## Goal
Extend RPQ property tests to cover complex regex patterns (RStar, RAlt, RSeq, epsilon, multi-symbol chains). Use FsCheck to generate random regex patterns and prove Belyanin ≡ Arroyuelo ≡ Kronecker.

## Steps

### 1. Add regex generator to Generators.fs
Add `RegexGenerators` class to `Generators.fs` that generates random `Regexp<string, string>` patterns (terminal-only, max depth 3-4).

### 2. Add regexToDfa helper to RPQTests.fs
Build a DFA from a regex using Brzozowski derivative-based construction (same algorithm as `RsmBuilder.buildBlockDfa` but outputs `DFA<string, int>` instead of `RsmBlock`).

### 3. Extend RPQGenerators to include regex
Add a combined generator `RegexAndGraph` that generates a regex + graph pair.

### 4. Add property tests
Three property tests comparing each pair of algorithms:
- Belyanin(DFA, NFA) ≡ Arroyuelo(regex, NFA)
- Belyanin(DFA, NFA) ≡ Kronecker(DFA, NFA)
- Arroyuelo(regex, NFA) ≡ Kronecker(DFA, NFA)

Test for multiple sources (not just single source).

### 5. Verify
- `dotnet build`
- `dotnet test`
- `dotnet fantomas .`

## Design Decisions

### Regex generator bounds
- Depth: 0-3 (controls recursion depth)
- Terminals: alphabet {a, b, c} for multi-symbol testing
- Full coverage: RStar, RAlt, RSeq, REps, RTerm (no RNonterm for RPQ)

### DFA construction
Use Brzozowski derivatives: `Regexp.derive` already exists and is tested. States are regexes (modulo REmpty). Start state is the original regex. For each alphabet symbol, compute derivative → transition. Final states are nullable regexes.
