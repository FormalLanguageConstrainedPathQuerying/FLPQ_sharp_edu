# Task 250: Complete Invariant Checks - Detailed Plan

## S1: Add LanguageRegistry.allCompatibleGrammars

**Code:** `src/FLPQ.Languages/LanguageRegistry.fs`
**Tests:** None (S3 uses it)
**Docs:** None

**Spec:**
- Add `allCompatibleGrammars : AnnotatedGrammar list` to LanguageRegistry module
- Filters `allLanguages |> List.collect (_.Grammars) |> List.filter isCykValiantCompatible`
- `isCykValiantCompatible g = not g.Properties.IsRsmDerived && not g.Properties.DoesNotCoverFullLanguage`

## S2: Extend checkCykValiantEquivalence with full invariants

**Code:** `tests/FLPQ.TestUtilities/TestHelpers.fs`
**Tests:** Implicitly tested by CrossParserEquivalenceTests
**Docs:** None

**Spec:**
- Replace SPPF nonterminal-only comparison with full cell-by-cell equality:
  `cykSppfTable.[i,j] = valSppfTable.[i,j] = modSppfTable.[i,j]` (including SplitPoint/ProdIdx)
- For accepted strings:
  1. Build BasicSPPF from each SPPF table via `fromParsingTable`
  2. Extract derivation tree via `extractDerivationTree`
  3. Verify `DerivationTree.leaves` = input string (for each algorithm)
  4. Verify `validateProductionChildren sppf cnf` passes
  5. Verify all three SPPFs have same number of SCCs
- Guard: skip SPPF checks when not accepted or n=0

## S3: Update checkLanguages to use allCompatibleGrammars

**Code:** `tests/FLPQ.Languages.Tests/CrossParserEquivalenceTests.fs`
**Tests:** Self-contained
**Docs:** None

**Spec:**
- Replace `lang.Grammars |> List.filter TestHelpers.isCykValiantCompatible` with `LanguageRegistry.allCompatibleGrammars` in the `checkLanguages` helper
- Since `allCompatibleGrammars` already aggregates compatible grammars from all languages, the `langs` parameter becomes less necessary but keep for grouping by language-specific string generators
