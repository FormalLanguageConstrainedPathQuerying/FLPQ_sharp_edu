# Task 248: Fix Valiant SPPF SplitPoint - Detailed Plan

## S1: Fix mxmSetSppf to use absolute SplitPoint

**Code:** `src/FLPQ.Languages/Valiant.fs` — modify `mxmSetSppf` and `doMultiplicationsSppf`
**Tests:** None (verified by existing `checkCykValiantEquivalence` tests)
**Docs:** None

**Spec:**
- `mxmSetSppf` currently sets `SplitPoint = k` where `k` is the local inner-dimension index from `Matrix.mxmi`
- The left submatrix slice is extracted from global table starting at column `m1.Col`
- Fix: add `leftColOffset: int` parameter to `mxmSetSppf`, use `SplitPoint = leftColOffset + k`
- Update call site in `doMultiplicationsSppf`: pass `m1.Col` as the offset
- After fix, Valiant SPPF tables must be byte-identical to CYK SPPF tables
