# Detailed Plan: Task 114 — Shared Generators.fs Module

## Goal
Create a shared `Generators.fs` module in a new `FLPQ.TestUtilities` test project. Consolidate all FsCheck `Arbitrary`/`Gen` types into this module. Eliminate `MyGen`/`MyArb` duplication across 10 files.

## Steps

### 1. Create `tests/FLPQ.TestUtilities/` project
- Create project directory: `tests/FLPQ.TestUtilities/`
- Create `FLPQ.TestUtilities.fsproj`: references `FsCheck.Xunit` (for Gen/Arb types), and source projects needed by generator types (FLPQ.Languages, FLPQ.LinearAlgebra, FLPQ.GraphAnalysis, FLPQ.RPQ)
- Create `Generators.fs` with consolidated generators and `module MyGen = FsCheck.FSharp.Gen` / `module MyArb = FsCheck.FSharp.Arb`

### 2. Consolidate generators into `Generators.fs`
From each source file, move generator classes into `Generators.fs`:

| Source File | Generator Class | Type Generated |
|---|---|---|
| `tests/FLPQ.LinearAlgebra.Tests/MatrixTests.fs` | `MatrixGenerators` | `Matrix<int>`, `Matrix<int> * Matrix<int>` |
| `tests/FLPQ.LinearAlgebra.Tests/LinearAlgebraTests.fs` | `LinearAlgebraGenerators` | `Matrix<int>` (square), `Matrix<int> * Matrix<int>` (compatible) |
| `tests/FLPQ.LinearAlgebra.Tests/BooleanDecompositionTests.fs` | `SetMatrixGenerators` | `Matrix<Set<int>>` |
| `tests/FLPQ.GraphAnalysis.Tests/RandomGraphGenerators.fs` | `RandomGraphGenerators` | `Matrix<bool> * int[]` |
| `tests/FLPQ.RPQ.Tests/RPQTests.fs` | `RPQGenerators` | `RPQTestData` |
| `tests/FLPQ.Languages.Tests/TestGrammars.fs` | `AbStringGenerators`, `AStringGenerators`, `ExprStringGenerators` | `string` |
| `tests/FLPQ.Languages.Tests/AutomatonTests.fs` | `IntersectionGenerators` | `NFA<string,int>`, `Terminal<string> list` |

The `RPQTestData` record must also move to `Generators.fs` (it's defined in RPQTests.fs alongside its generator).

### 3. Add to solution
```sh
dotnet sln FLPQ.slnx add tests/FLPQ.TestUtilities/FLPQ.TestUtilities.fsproj
```

### 4. Update all test projects' .fsproj files
Add `<ProjectReference>` to `FLPQ.TestUtilities.fsproj` in each of the 6 test projects:
- `FLPQ.Languages.Tests`
- `FLPQ.LinearAlgebra.Tests`
- `FLPQ.GraphAnalysis.Tests`
- `FLPQ.Printers.Tests`
- `FLPQ.RPQ.Tests`

### 5. Update 10 source files that define `MyGen`/`MyArb`
For each file:
- Replace `module MyGen = FsCheck.FSharp.Gen` / `module MyArb = FsCheck.FSharp.Arb` with `open FLPQ.TestUtilities.Generators` (or just remove if the file moved its generators out)
- Remove the generator class definitions (moved to Generators.fs)
- Update `[<Properties(Arbitrary = [| typeof<MatrixGenerators> |])>]` references to use fully qualified path: `typeof<Generators.MatrixGenerators>` or add an `open` for the module
- For `RPQTestData` record: import from Generators.fs

### 6. Handle RPQTests.fs special case
`RPQTests.fs` defines `RPQTestData` record AND `RPQGenerators` class. Both must move to Generators.fs.
The `RPQTests.fs` must import `RPQTestData` and reference `Generators.RPQGenerators`.

### 7. Handle TestGrammars.fs special cases
`TestGrammars.fs` defines grammar values (grammar1, grammar2, etc.) AND generator classes. Only the generator classes move. The grammar values stay.

### 8. Handle RandomGraphGenerators.fs
This file ONLY contains the generator class. The file becomes empty after moving. Keep the file as a thin wrapper or remove it entirely (prefer removing and using Generators.fs directly).

### 9. Remove dead imports
- `EbnfParserTests.fs`: remove `module MyGen`/`module MyArb` (lines 11-12) — they are unused
- `RsmToGrammarTests.fs`: remove `module MyGen`/`module MyArb` (lines 11-12) — they are unused

### 10. Verify
- `dotnet build FLPQ.slnx -c Debug`
- `dotnet test`
- `dotnet fantomas .`

## Files to create
1. `tests/FLPQ.TestUtilities/FLPQ.TestUtilities.fsproj`
2. `tests/FLPQ.TestUtilities/Generators.fs`

## Files to modify
1. `FLPQ.slnx` — add new project
2. `tests/FLPQ.Languages.Tests/FLPQ.Languages.Tests.fsproj` — add ProjectReference
3. `tests/FLPQ.LinearAlgebra.Tests/FLPQ.LinearAlgebra.Tests.fsproj` — add ProjectReference
4. `tests/FLPQ.GraphAnalysis.Tests/FLPQ.GraphAnalysis.Tests.fsproj` — add ProjectReference
5. `tests/FLPQ.Printers.Tests/FLPQ.Printers.Tests.fsproj` — add ProjectReference
6. `tests/FLPQ.RPQ.Tests/FLPQ.RPQ.Tests.fsproj` — add ProjectReference
7. `tests/FLPQ.LinearAlgebra.Tests/MatrixTests.fs` — remove MyGen/MyArb/MatrixGenerators, import from Generators
8. `tests/FLPQ.LinearAlgebra.Tests/LinearAlgebraTests.fs` — remove MyGen/MyArb/LinearAlgebraGenerators, import from Generators
9. `tests/FLPQ.LinearAlgebra.Tests/BooleanDecompositionTests.fs` — remove MyGen/MyArb/SetMatrixGenerators, import from Generators
10. `tests/FLPQ.GraphAnalysis.Tests/RandomGraphGenerators.fs` — remove entire file (or make empty module redirecting to Generators)
11. `tests/FLPQ.GraphAnalysis.Tests/MsBfsTests.fs` — remove MyGen/MyArb, update reference to Generators.RandomGraphGenerators
12. `tests/FLPQ.RPQ.Tests/RPQTests.fs` — remove RPQTestData, RPQGenerators, MyGen/MyArb; import from Generators
13. `tests/FLPQ.Languages.Tests/TestGrammars.fs` — remove MyGen/MyArb, AbStringGenerators, AStringGenerators, ExprStringGenerators; import from Generators
14. `tests/FLPQ.Languages.Tests/AutomatonTests.fs` — remove MyGenAuto/MyArbAuto, IntersectionGenerators; import from Generators
15. `tests/FLPQ.Languages.Tests/EbnfParserTests.fs` — remove unused MyGen/MyArb
16. `tests/FLPQ.Languages.Tests/RsmToGrammarTests.fs` — remove unused MyGen/MyArb

## Design Decisions

### Generator module naming
All generator classes move into `Generators.fs` module. Each class keeps its original name (e.g., `MatrixGenerators`, `RPQGenerators`). The `MyGen`/`MyArb` aliases are defined once in `Generators.fs`.

### References for Generators.fs
Generators that produce types from `FLPQ.LinearAlgebra`, `FLPQ.Languages`, `FLPQ.RPQ`, `FLPQ.GraphAnalysis` need project references to those projects. All can be added.

### RandomGraphGenerators.fs fate
This file becomes empty after consolidation. Keep the file as `module RandomGraphGenerators` that just re-exports from `FLPQ.TestUtilities.Generators` to avoid breaking references. Actually, just delete it since we'll update all references.

### RPQTestData record
Must move to Generators.fs since it's closely tied to RPQGenerators. RPQTests.fs imports it.
