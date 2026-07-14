# Task 180 — Fix RNGLR: Replace `rnglrAcceptsWithoutSppfValidation` with `rnglrAccepts`

## Subtasks

### S1: Replace all `rnglrAcceptsWithoutSppfValidation` with `rnglrAccepts` in tests

**Code:** No algorithm changes; test code only
**Tests:** Replace 5 usages in `tests/FLPQ.Languages.Tests/RnglrTests.fs` (lines 373, 377, 382, 388, 710). Run tests and record failures.
**Docs:** None

**Spec:**
- Search-and-replace `rnglrAcceptsWithoutSppfValidation` → `rnglrAccepts` in `RnglrTests.fs`
- Both functions have the same signature `(rsm: RSM<string, string>) -> (input: string list) -> bool`
- The only difference: `rnglrAccepts` validates SPPF and extracts derivation tree; `rnglrAcceptsWithoutSppfValidation` skips it
- After replacement, `grammar4` (S -> S a | a) and `grammarG8` (S -> a | S S | S S S) tests that were previously acceptance-only will now attempt SPPF validation and tree extraction
- Run tests and record failures

### S2: Fix RNGLR algorithm for failing tests

**Code:** Fix in `src/FLPQ.Languages/Rnglr.fs`, possibly `src/FLPQ.Languages/Sppf.fs`, `src/FLPQ.Languages/GLL.fs` (tree extraction)
**Tests:** Fix `RnglrTests.fs` failing tests to pass with `rnglrAccepts`
**Docs:** None

**Spec:**
- Analyze which tests fail after S1
- Fix the underlying issue: SPPF construction or tree extraction for left-recursive and ambiguous grammars
- Ensure all tests pass with full SPPF validation
- All existing RNGLR tests must pass (0 failures, 0 skipped)

### S3: Remove `rnglrAcceptsWithoutSppfValidation` from TestHelpers

**Code:** `tests/FLPQ.TestUtilities/TestHelpers.fs`
**Tests:** Ensure all tests still pass after removal
**Docs:** None

**Spec:**
- Remove the `rnglrAcceptsWithoutSppfValidation` function definition (lines 348-349)
- Remove the `validateSppf` parameter from `rnglrAcceptsWithSppfValidation` — always validate
- Rename `rnglrAcceptsWithSppfValidation` inline into `rnglrAccepts` or keep as `rnglrAccepts`
- Ensure tests compile and pass
