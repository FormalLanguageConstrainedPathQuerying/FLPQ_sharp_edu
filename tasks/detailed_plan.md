# Detailed Plan for Task 251

## Task

Add more SPPF invariant checking to `checkCykValiantEquivalence`:
1. Single root invariant: only one vertex without incoming edges, labelled with start nonterminal, leftPos=0, rightPos=input length
2. SplitPoint consistency: for any Production vertex with two children, splitPoint = lCH.RightPos = rCH.LeftPos

## Subtasks

### S1: Add invariant functions to BasicSppf module

**Code:** `src/FLPQ.Languages/BasicSppf.fs` — add `validateSingleRoot` and `validateProductionSplitConsistency`
**Tests:** Existing tests cover via `checkCykValiantEquivalence` call in `CrossParserEquivalenceTests.fs`
**Docs:** None — internal invariant functions don't change public API

**Spec:**
- Add `validateSingleRoot : BasicSPPF<'t,'nt> -> Nonterminal<'nt> -> int -> Result<unit, string list>` 
  - Parameters: sppf, expected start nonterminal, input length n
  - Count vertices with zero incoming edges in the graph
  - Must be exactly 1
  - That vertex must be a Nonterminal node with leftPos=0, rightPos=n, and the given start nonterminal
- Add `validateProductionSplitConsistency : BasicSPPF<'t,'nt> -> Result<unit, string list>`
  - For each Production vertex with exactly 2 outgoing edges (children):
    - Get both children's node info
    - Compute leftPos/rightPos for each child type: Terminal(l,r) → (l,r), Nonterminal(_,l,r) → (l,r), Epsilon(p) → (p,p)
    - Identify left child (child with smaller leftPos) and right child
    - Verify production.splitPoint = leftChild.rightPos AND production.splitPoint = rightChild.leftPos

### S2: Call new validators in checkCykValiantEquivalence

**Code:** `tests/FLPQ.TestUtilities/TestHelpers.fs` — add calls to new validators
**Tests:** Existing tests cover
**Docs:** None

**Spec:**
- After existing SPPF invariants in `checkCykValiantEquivalence`, call `validateSingleRoot` and `validateProductionSplitConsistency` for all three SPPFs (CYK, Valiant, Modified Valiant)
- Use `failwithf` on errors

### S3: Run all tests

**Spec:**
- Run `dotnet test` to verify all tests pass
- Ensure zero regressions
