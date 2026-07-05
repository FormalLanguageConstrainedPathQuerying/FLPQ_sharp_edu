# Detailed Plan: Task 129 — Golden Test Gaps

## Goal
Add golden tests for modules lacking them, plus TeX/DOT runtime compilation checks.

## Changes

### 1. LL table TeX golden tests (new file)
- `tests/FLPQ.Printers.Tests/LLTableTeXGoldenTests.fs`
- Generate LL table TeX for grammar1 and grammar8
- Compare with golden reference files

### 2. Matrix TeX golden tests (add to MatrixTeXTests.fs)
- Generate Matrix TeX for known matrices
- Compare with golden reference files

### 3. Automaton dot/Tikz golden tests (add to AutomatonVisualizationTests.fs)
- Generate dot/Tikz for known automata
- Compare with golden reference files

### 4. Derivation tree dot golden tests (add to DerivationTreeVisualizationTests.fs)
- Generate dot for known derivation trees
- Compare with golden reference files

### 5. Valiant trace TeX golden tests (new file)
- `tests/FLPQ.Printers.Tests/ValiantTraceGoldenTests.fs`
- Generate Valiant trace TeX for grammar1
- Compare with golden reference files

### 6. TeX/DOT compilation checks (add to TexCompilationTests.fs)
- LL table TeX compilation check
- Matrix TeX compilation check  
- Valiant trace TeX compilation check
- Derivation tree dot compilation check
