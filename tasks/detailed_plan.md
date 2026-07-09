# Task 150: Refactoring

## Subtasks

### S1: Remove unused FSharpPlus from FLPQ.GraphAnalysis.fsproj
- Remove `<PackageReference Include="FSharpPlus" Version="1.9.1" />` from `src/FLPQ.GraphAnalysis/FLPQ.GraphAnalysis.fsproj`
- Graph.fs and MsBfs.fs don't use FSharpPlus types
- Verify build succeeds

### S2: Extract shared `collectGraphEdges` for GLL/RNGLR
- Identical function in `Gll.fs` and `Rnglr.fs` (both in module FLPQ.Languages)
- Create a shared internal function in a common location. Since both are in the same project (`FLPQ.Languages`), place it in a suitable shared module. Check if there's a `GraphHelpers` or similar module, or put it in `GllTypes.fs` / a new module.
- Update both Gll.fs and Rnglr.fs to use the shared function

### S3: Extract shared test helpers (grammarToEbnfText, grammarToRsm)
- Duplicated between `RnglrTests.fs` and `GllTests.fs`
- Create a shared test helpers module. Check if there's a `FLPQ.TestUtilities` project or `TestHelpers.fs` in the test project.
- Both functions are private to their files — make them accessible from a shared module

### S4: Extract shared test helpers (stringToTerminals, inputToGraph, acceptance helpers, nonEpsilon)
- `stringToTerminals` (Rnglr) / `stringToChars` (Gll) — identical
- `inputToGraph` (Rnglr) / `terminalsToGraph` (Gll) — identical
- `rnglrAccepts`, `gllAccepts`, `cykAccepts` — duplicated
- `nonEpsilon` — duplicated
- Place in shared test helpers module

### S5: Parametrize filterOutgoing/filterIncoming empty-set tests in GraphTests.fs
- Lines 63-72 and 74-83 are structurally identical
- Create one generic parameterized test helper

### S6: Parametrize decompose/decomposeNonEmptySet in BooleanDecomposition.fs
- Both share the same structure: collect all elements → create bool matrix per element
- Extract shared inner implementation parameterized by cell extractor

### S7: Configure fsharplint to exclude generated files
- Add patterns to `ignoreFiles` in fsharplint.json to exclude:
  - `*AssemblyAttributes.fs`
  - `*Program.fs` (from test SDK)
  - Any other auto-generated files

## Progress
- [done] S1: Remove unused FSharpPlus
- [done] S2: Extract shared collectGraphEdges
- [done] S3: Extract shared grammarToEbnfText/grammarToRsm
- [done] S4: Extract shared test helpers
- [done] S5: Parametrize GraphTests filter tests
- [done] S6: Parametrize BooleanDecomposition
- [done] S7: Configure fsharplint ignoreFiles
