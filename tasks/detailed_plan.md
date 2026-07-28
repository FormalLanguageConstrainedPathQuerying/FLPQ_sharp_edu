# Task 204: Deduplicate GLL/RNGLR shared algorithm helpers

## Subtasks

- [x] S1 - Extract shared `linearIndex` formula into `GridIndex` module in `PathIndex.fs`. Update `GSS.linearIndex`, `RnglrGSS.linearIndex`, and `PathIndex.linearIndex` to delegate to it.
- [x] S2 - Extract generic `collectActiveGss` into `GraphHelpers` module (takes `Matrix<'a option>` parameter). Update Gll.fs and Rnglr.fs to use shared version.
- [x] S3 - Add `addWithTracking` function to `PathIndex` module. Replace local `addToIndex` in Gll.fs and Rnglr.fs with the shared function.
- [x] S4 - Build and run all tests. Verify zero failures (893 tests passed, 0 failed, 0 skipped).
