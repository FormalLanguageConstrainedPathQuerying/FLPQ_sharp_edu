# Detailed Plan: Task 240 — Fix toCNF Function

## Overview

Add non-generating and unreachable nonterminal cleanup to the CNF conversion pipeline. Currently `toCNF` does `binarize → eliminateEpsilon → eliminateUnit → replaceTerminals` without removing useless nonterminals, leaving unreachable fresh nonterminals (e.g., `N_2 -> a` from `S -> S S S`).

## Subtasks

### S1: Add non-generating and unreachable removal functions to Grammar module

**Code:** `src/FLPQ.Languages/Grammar.fs` — add `removeNonGenerating` and `removeUnreachable` private functions
**Tests:** None yet (tested in S3)
**Docs:** None (internal helpers, existing `toCNF` doc will be updated in S3)

**Spec:**
- `removeNonGenerating`: removes nonterminals that cannot derive a terminal-only string
  - Fixed-point computation similar to `computeNullable`
  - Start: nonterminals with at least one rule where ALL RHS symbols are terminals (no nonterminals)
  - Expand: add nonterminals whose rules have RHS containing only terminals and already-known-generating nonterminals
  - Remove all rules involving non-generating nonterminals
- `removeUnreachable`: removes nonterminals not reachable from the start symbol
  - BFS/DFS from start symbol, following nonterminal references in RHS
  - Remove rules for unreachable nonterminals
- Order in pipeline: `removeNonGenerating` first, then `removeUnreachable`
  - Reasoning: non-generating removal may create new unreachable nonterminals; the opposite order (unreachable first) would leave those behind

### S2: Add grammar-cleanliness check functions

**Code:** `src/FLPQ.Languages/Grammar.fs` — add `allNonterminalsReachable` and `allNonterminalsGenerating` public functions
**Tests:** Tested in S3
**Docs:** None (public API functions, usage clear from naming)

**Spec:**
- `allNonterminalsReachable (g: Grammar<'t,'nt>) : bool` — returns true iff all nonterminals in `g` are reachable from `g.Start` via rule RHS references
- `allNonterminalsGenerating (g: Grammar<'t,'nt>) : bool` — returns true iff every nonterminal in `g` can derive at least one string of terminals (or epsilon)
- Both work on any BNF grammar, not just CNF

### S3: Integrate cleanup into toCNF and add tests

**Code:**
- `src/FLPQ.Languages/Grammar.fs` — add `removeNonGenerating` and `removeUnreachable` calls to `toCNF` pipeline
- `tests/FLPQ.Languages.Tests/GrammarTests.fs` or new test module — add test facts and property tests
**Tests:** 
- `[<Fact>]` test: grammar `S -> a; S -> S S; S -> S S S` after CNF has no unreachable `N_2` (all nonterminals reachable)
- `[<Fact>]` test: specific grammars from LanguageRegistry with known useful nonterminals in CNF
- `[<Property>]` test (or `[<Fact>]` with iteration): for all grammars in LanguageRegistry, `toCNF` produces grammars where `allNonterminalsReachable` and `allNonterminalsGenerating` are both true
**Docs:** None (existing `toCNF` behavior is extended, not a new module or API)
