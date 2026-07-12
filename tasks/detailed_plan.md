# Task 167: SPPF Range Node Children Check

Add SPPF invariant check to all GLL and RNGLR tests: on accepted strings, every SppfRange node in the SPPF must have at least one child (outgoing PackedAlternative edge).

## Subtasks

### S1: Add SPPF validation helpers

**Code:** `src/FLPQ.Languages/Sppf.fs` — add `validateRangeNodesHaveChildren` function. `tests/FLPQ.TestUtilities/TestHelpers.fs` — add `assertSppfInvariant`, `gllAcceptsWithSppfCheck`, `rnglrAcceptsWithSppfCheck`.
**Tests:** None (test utility code)
**Docs:** None (internal invariant, no public API change)

**Spec:**
- Add `Sppf.validateRangeNodesHaveChildren : SPPF<'t,'nt> -> Result<unit, string list>` — iterates all vertices, for each SppfRange, checks it has at least one outgoing edge with label `Some PackedAlternative`. Returns Error with descriptions of violating nodes.
- Add `assertSppfInvariant : SPPF<'t,'nt> -> unit` to TestHelpers — calls validateRangeNodesHaveChildren and fails with descriptive message.
- Add `gllAcceptsWithSppfCheck : Grammar -> string list -> bool` — builds PathIndex, checks acceptance, if accepted builds SPPF via `Sppf.buildSppfFromIndex` and validates.
- Add `rnglrAcceptsWithSppfCheck : Grammar -> string list -> bool` — same for RNGLR.

### S2: Modify GLL tests to add SPPF validation

**Code:** `tests/FLPQ.Languages.Tests/GllTests.fs`
**Tests:** All existing GLL tests (Fact + Property) — add SPPF invariant check.
**Docs:** None

**Spec:**
- GllAcceptance module: replace `TestHelpers.gllAccepts` with `TestHelpers.gllAcceptsWithSppfCheck` in all Fact tests.
- GllTreeExtraction module: in `gllTree`, after confirming acceptance and before tree extraction, build SPPF via common flow and validate. Keep tree extraction from path index (unchanged), add parallel SPPF check.
- GllCykEquivalence: restructure property tests to use `gllAcceptsWithSppfCheck` for GLL side.
- GllRegexEquivalence: restructure to validate SPPF when GLL accepts.
- GllGrammarAcceptanceAndTree: replace `gllAccepts` → `gllAcceptsWithSppfCheck`. Add SPPF check to tree tests.
- GllGrammar159A/B/C/D: add SPPF check to tree tests.
- GllPropertyTreeYield: add SPPF check when tree is Some.

### S3: Modify RNGLR tests to add SPPF validation

**Code:** `tests/FLPQ.Languages.Tests/RnglrTests.fs`
**Tests:** All existing RNGLR tests (Fact + Property) — add SPPF invariant check.
**Docs:** None

**Spec:**
- RnglrAcceptance: replace `TestHelpers.rnglrAccepts` → `TestHelpers.rnglrAcceptsWithSppfCheck`.
- RnglrEquivalence: restructure to validate SPPF when RNGLR accepts.
- RnglrRightNullable, RnglrReductionCascade: replace with SPPF-checking variant.
- RnglrRegexEquivalence: add SPPF check to `rnglrAcceptsRegex`.
- RnglrGrammarAcceptanceAndTree: replace acceptance helpers, add SPPF check to tree tests.
- CrossAlgorithmEquivalence: add SPPF check to RNGLR side.
- RnglrGrammar159A/B/C/D: add SPPF check to tree tests.
- RnglrPropertyTreeYield: add SPPF check when tree is Some.
- SppfDotTests: add `assertSppfInvariant` call after building SPPF.
