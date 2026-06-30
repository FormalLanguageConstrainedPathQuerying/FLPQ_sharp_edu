# Detailed Plan: Task 56 — EBNF Refactoring

## Overview

Refactoring of EBNF parsing tests and infrastructure:
1. Add `isDeterministic` function for DFA and use it in existing RSM builder tests
2. Add whitespace-handling EBNF parsing tests
3. Add property-based parsing equivalence tests
4. Fix `a* a*` grammar test with exact structure verification and explanation

## Sub-tasks

### 56.1 — Add `isDeterministic` to Dfa module and integrate into tests

**Problem:** The docs mention `isDeterministic` but it doesn't exist. RsmBuilderTests need to verify that built blocks are deterministic.

**Implementation:**
- Add `Dfa.isDeterministic: DFA<'t,'s> -> bool` to Automaton.fs
  - Check: for each (state, symbol) pair, `move` produces at most one target
  - Equivalent: for each row i, for each symbol sym in alphabet, count distinct columns j where `matrix[i,j]` contains `sym` — must be ≤ 1
- In every test in RsmBuilderTests that builds an RSM, add assertion that all blocks are deterministic
- Fix the no-op test `RSM built from EBNF has deterministic blocks` to actually assert determinism
- Update `docs/automaton.md` to document the new function

### 56.2 — Add whitespace handling EBNF parsing tests

Tokens already ignore all whitespace. Add tests verifying:
- `S -> a (a | b)` ≡ `S -> a (a|b)` ≡ `S -> a(a |b)` — produce same DFA
- `S -> a S | (eps)` ≡ `S -> a S | ((eps))` ≡ `S -> a S |(eps)` ≡ `S -> a S |eps` — produce same DFA
- `S -> a (a ( a | b))` ≡ `S -> a(a (a | b))` ≡ `S -> a (a ( a |     b))` — produce same DFA

Compare resulting RSM blocks (identical state count, transitions, etc.).

### 56.3 — Add property-based EBNF parsing equivalence tests

- `S -> a S | eps` must accept/reject same strings as `S -> (a*) (a*)`
- Use FsCheck `[<Property>]` with `AStringGenerators`
- Parse both as EBNF, build RSM, convert to BNF, compare CYK results

### 56.4 — Fix `Build RSM for a* a* grammar` test

**Parser analysis:** `a* a*` parses as `RSeq(RStar(RTerm "a"), RStar(RTerm "a"))` = `(a*) (a*)`. Parser precedence is correct.

**State count analysis:** Brzozowski derivatives produce a non-minimal DFA because:
- Derive `(a*) (a*)` w.r.t `a` → `RAlt(RSeq(RStar(a),RStar(a)), RStar(a))` — distinct from original
- No algebraic simplification of `RSeq(RStar(r), RStar(r))` ≡ `RStar(r)` or `RAlt(RStar(r), RStar(r))` ≡ `RStar(r)`
- This is the expected behavior: derivatives operate syntactically, not semantically

**Action:**
- Determine exact state count (by running code)
- Assert exact state count
- Add explanatory comment about why it's not 1 (Brzozowski derivatives are syntactic, no algebraic simplification beyond basic `mkAlt` merges)
- Also verify: start state is NOT final (unlike bare `a*`)
