# Detailed Plan: Task 183 — Deep GLL refactoring

## Overview

Refactor GLL/RNGLR shared infrastructure: move `isAccepted` to PathIndex, switch GLL to ExtendedRSM, parameterize test helpers, remove `Gll.extractDerivationTree`.

### S1: Move `isAccepted` from Rnglr.fs to PathIndex.fs

**Code:**
- `src/FLPQ.Languages/PathIndex.fs` — add `isAccepted` function
- `src/FLPQ.Languages/Rnglr.fs` — remove `isAccepted`, replace calls with `PathIndex.isAccepted`

**Tests:** No new tests. Existing tests must continue to pass.
**Docs:** None.

**Spec:**
- Copy `isAccepted` from Rnglr.fs (lines 322-335) to PathIndex.fs module
- Signature: `PathIndex<'t, 'nt> -> ExtendedRSM<'t, 'nt> -> int -> bool`
- Remove from Rnglr.fs
- Update any internal references in Rnglr.fs (none expected — it's only called externally)

### S2: Refactor GLL to use ExtendedRSM internally

**Code:**
- `src/FLPQ.Languages/Gll.fs` — change `ersm.OriginalRsm` to `ersm.ExtendedRsm` in `buildPathIndex`; remove `isAccepted` function; update all derived values

**Tests:** No new tests. All existing GLL and RNGLR tests must pass.
**Docs:** None.

**Spec:**
- Line 78: `let rsm = ersm.OriginalRsm` → `let rsm = ersm.ExtendedRsm`
- All derived values (stateCount, StateInfo, BlockStart, termTrans, nontermTrans) now come from ExtendedRSM
- GLL starts from S' block (fresh start), which has one nonterminal transition to original start — processed as a regular call
- Remove `Gll.isAccepted` (lines 385-401)

### S3: Update GllRunner.fs for ExtendedRSM-based path index

**Code:**
- `src/FLPQ.Cli/GllRunner.fs` — replace `GLL.isAccepted` with `PathIndex.isAccepted`; fix root range computation to use fresh start block (S')

**Tests:** No new tests.
**Docs:** None.

**Spec:**
- Replace `GLL.isAccepted pathIndex ersm vertexCount` → `PathIndex.isAccepted pathIndex ersm vertexCount`
- Root ranges: compute from fresh start block (S' start → S' final), same as RNGLR
- Remove manual computation of `startGlobalState`, `startBlockOffset`, and the loop over `startBlock.Dfa.FinalStates`
- Use `ersm.ExtendedRsm` for blockStart/blockFinalsMap in SPPF construction

### S4: Parameterize `accepts` and `checkReject` in TestHelpers.fs; remove obsolete helpers

**Code:**
- `tests/FLPQ.TestUtilities/TestHelpers.fs` — parameterize `accepts`/`checkReject`; remove `gllAcceptsRsm`, `gllAcceptsWithSppfCheck`, `gllAcceptsAndCheckTree`, `buildPathIndexForRsm`

**Tests:** No new tests. All existing tests must still compile and pass after updating call sites (S5, S6).
**Docs:** None.

**Spec:**
- New `accepts` signature: `(Nonterminal<string> -> ExtendedRSM<_,_> -> Graph<_,_> -> PathIndex<_,_>) -> (PathIndex<_,_> -> ExtendedRSM<_,_> -> int -> bool) -> RSM<string,string> -> string list -> bool`
- New `checkReject` signature: `(Nonterminal<string> -> ExtendedRSM<_,_> -> Graph<_,_> -> PathIndex<_,_>) -> (PathIndex<_,_> -> ExtendedRSM<_,_> -> int -> bool) -> Grammar<string,string> -> string list -> bool`
- Shared pipeline: create ExtendedRSM → buildPI freshStart ersm graph → isAcc pi ersm vc → SPPF from fresh start block → enumerateTrees → validate leaves = input
- Remove obsolete helpers

### S5: Update GllTests.fs with local bindings

**Code:**
- `tests/FLPQ.Languages.Tests/GllTests.fs` — add local `accepts`/`checkReject` bindings; replace all call sites

**Tests:** All existing GLL tests must pass.
**Docs:** None.

**Spec:**
- Add at module level:
  ```
  let private accepts = TestHelpers.accepts Gll.buildPathIndex PathIndex.isAccepted
  let private checkReject = TestHelpers.checkReject Gll.buildPathIndex PathIndex.isAccepted
  ```
- Replace all `TestHelpers.accepts` → `accepts` (~20 calls)
- Replace all `TestHelpers.gllAcceptsAndCheckTree` patterns with `Assert.True(accepts rsm input)` (since `accepts` already validates tree leaves internally)

### S6: Update RnglrTests.fs with local bindings

**Code:**
- `tests/FLPQ.Languages.Tests/RnglrTests.fs` — add local bindings; replace all call sites; fix `checkRsmAccepts`/`checkRsmRejects`; inline ExtendedRSM in `buildSppf`

**Tests:** All existing RNGLR tests must pass.
**Docs:** None.

**Spec:**
- Add at module level:
  ```
  let private accepts = TestHelpers.accepts Rnglr.buildPathIndex PathIndex.isAccepted
  let private checkReject = TestHelpers.checkReject Rnglr.buildPathIndex PathIndex.isAccepted
  ```
- Replace `TestHelpers.accepts` → `accepts`, `TestHelpers.checkReject` → `checkReject` (~35 calls)
- In `checkRsmAccepts`/`checkRsmRejects`: replace `Rnglr.isAccepted` → `PathIndex.isAccepted`, inline ExtendedRSM creation (since `buildPathIndexForRsm` is removed)
- In `buildSppf`: inline ExtendedRSM creation

### S7: Remove `Gll.extractDerivationTree`

**Code:**
- `src/FLPQ.Languages/Gll.fs` — remove `extractDerivationTree` (~82 lines, lines 403-488)

**Tests:** No new tests. All tests must pass (tree extraction now goes through SPPF only).
**Docs:** None.

**Spec:**
- Remove the entire `extractDerivationTree` function from Gll.fs
- Verify no remaining references to `Gll.extractDerivationTree` in the codebase

## Execution Order

S1 → S2 → S3 → S4 → S5 → S6 → S7

S1-S3 are source changes that can be done first. S4-S6 are test infrastructure changes that depend on S1-S3. S7 is a cleanup that depends on S5 (no more references to `Gll.extractDerivationTree`).
