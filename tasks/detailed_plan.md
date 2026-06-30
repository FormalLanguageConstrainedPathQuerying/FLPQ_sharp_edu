# Detailed Plan: Task 64 — Refactoring (RPQ project separation, MS-BFS to GraphAnalysis, NFA interface unification, Matrix.fold)

## Overview

Refactor the project structure to separate RPQ algorithms and MS-BFS into their own projects,
unify RPQ algorithm interfaces to accept NFA as graph representation, and add `fold` to `Matrix`.

## Sub-tasks

### 1. Add `fold` to `Matrix` module

- Add `Matrix.fold : ('acc -> 'a -> 'acc) -> 'acc -> Matrix<'a> -> 'acc` to `Matrix.fs`
- Implementation: iterate over all cells left-to-right, top-to-bottom, applying the folder
- Replace the private `anyTrue` in `MsBfs.fs` with `Matrix.fold (fun acc x -> acc || x) false m`

### 2. Create `FLPQ.GraphAnalysis` project

- Create `src/FLPQ.GraphAnalysis/` directory
- Create `src/FLPQ.GraphAnalysis/FLPQ.GraphAnalysis.fsproj`:
  - TargetFramework: `net10.0`
  - `GenerateDocumentationFile: true`
  - Compile: `MsBfs.fs`
  - ProjectReference: `FLPQ.LinearAlgebra`
  - PackageReference: `FSharpPlus` (may be needed for NonEmptySet if used internally — actually no, MsBfs doesn't use FSharpPlus, but keep for consistency)
- Move `MsBfs.fs` from `src/FLPQ.LinearAlgebra/` to `src/FLPQ.GraphAnalysis/`
  - Change namespace from `FLPQ.LinearAlgebra` to `FLPQ.GraphAnalysis`
  - Add `open FLPQ.LinearAlgebra`
  - Replace private `anyTrue` with `Matrix.fold (fun acc x -> acc || x) false m`
- Remove `MsBfs.fs` from `src/FLPQ.LinearAlgebra/FLPQ.LinearAlgebra.fsproj`

### 3. Create `FLPQ.GraphAnalysis.Tests` project

- Create `tests/FLPQ.GraphAnalysis.Tests/` directory
- Create `tests/FLPQ.GraphAnalysis.Tests/FLPQ.GraphAnalysis.Tests.fsproj`:
  - TargetFramework: `net10.0`, `IsPackable: false`
  - Compile: `MsBfsTests.fs`
  - PackageReferences: same as other test projects (coverlet, FsCheck.Xunit, FSharpPlus, xunit, etc.)
  - ProjectReferences: `FLPQ.GraphAnalysis`, `FLPQ.LinearAlgebra`
- Move `MsBfsTests.fs` from `tests/FLPQ.LinearAlgebra.Tests/` to `tests/FLPQ.GraphAnalysis.Tests/`
  - Update `open FLPQ.GraphAnalysis` (instead of only `open FLPQ.LinearAlgebra`)
- Remove `MsBfsTests.fs` from `tests/FLPQ.LinearAlgebra.Tests/FLPQ.LinearAlgebra.Tests.fsproj`

### 4. Create `FLPQ.RPQ` project

- Create `src/FLPQ.RPQ/` directory
- Create `src/FLPQ.RPQ/FLPQ.RPQ.fsproj`:
  - TargetFramework: `net10.0`, `GenerateDocumentationFile: true`
  - Compile order: `GraphReader.fs` → `BelyaninRPQ.fs` → `ArroyueloRPQ.fs` → `KroneckerRPQ.fs`
  - ProjectReferences: `FLPQ.LinearAlgebra`, `FLPQ.GraphAnalysis`, `FLPQ.Languages`
  - PackageReference: `FSharpPlus`
- Move files from `src/FLPQ.Languages/` to `src/FLPQ.RPQ/`:
  - `GraphReader.fs` → namespace `FLPQ.RPQ`
  - `BelyaninRPQ.fs` → namespace `FLPQ.RPQ`
  - `ArroyueloRPQ.fs` → namespace `FLPQ.RPQ`
  - `KroneckerRPQ.fs` → namespace `FLPQ.RPQ`
- Remove these files from `src/FLPQ.Languages/FLPQ.Languages.fsproj`

### 5. Unify RPQ algorithms interface (accept graph as NFA)

#### NFA-to-perLabel helper

Add a helper function (private in each module or shared) to convert `NFA<'t, int>` to `Map<'t, Matrix<bool>>`.

```fsharp
let private nfaToPerLabelMatrices (nfa: NFA<'t, int>) : Map<'t, Matrix<bool>> =
    let vCount = Nfa.stateCount nfa
    let labels = Nfa.alphabet nfa
    labels |> Set.toList |> List.map (fun label ->
        let m = Matrix.init vCount vCount false
        for i in 0..vCount-1 do
            for j in 0..vCount-1 do
                match nfa.transitions.data.[i, j] with
                | Some nes when NonEmptySet.contains label nes -> m.data.[i, j] <- true
                | _ -> ()
        (label, m)
    ) |> Map.ofList
```

#### BelyaninRPQ.evaluate

New signature: `DFA<'t, int> -> NFA<'t, int> -> Matrix<bool>`

- Extract source vertices from `nfa.startStates`
- Convert NFA to per-label matrices
- For each source vertex, run the single-source algorithm
- Stack results into |sources| × |V| matrix
- Return `Matrix<bool>` instead of `bool[]`

#### ArroyueloRPQ.evaluate (rename from evaluateWithSources)

New signature: `NFA<'t, int> -> Regexp<'t, 'nt> -> Matrix<bool>`

- Extract source vertices from `nfa.startStates`
- Convert NFA to per-label matrices
- Derive `vCount` from `Nfa.stateCount nfa`
- Compute full |V|×|V| matrix via `evaluate` (keep internal name)
- Extract rows for source vertices
- Return |sources| × |V| matrix

#### KroneckerRPQ.evaluate

New signature: `DFA<'t, int> -> NFA<'t, int> -> Matrix<bool>`

- Extract source vertices from `nfa.startStates`
- Convert NFA to per-label matrices
- Derive `vCount` from `Nfa.stateCount nfa`
- Run the Kronecker algorithm as before
- Return |sources| × |V| matrix

#### GraphReader.parseGraph

Change return type from `LabeledGraph<string>` to `NFA<string, int>`.

- Keep the internal parsing logic (parse edges, determine max vertex, etc.)
- Build NFA: all vertices are states, edges become transitions, start vertices become start states, all vertices are final states (for completeness)
- Remove `LabeledGraph` type (no longer needed externally, or keep internal)

Actually, remove `LabeledGraph` type entirely since it's no longer used. Return `NFA<string, int>` directly.

### 6. Create `FLPQ.RPQ.Tests` project

- Create `tests/FLPQ.RPQ.Tests/` directory
- Create `tests/FLPQ.RPQ.Tests/FLPQ.RPQ.Tests.fsproj`:
  - TargetFramework: `net10.0`, `IsPackable: false`
  - Compile: `RPQTests.fs`
  - PackageReferences: same as other test projects
  - ProjectReferences: `FLPQ.RPQ`, `FLPQ.Languages`, `FLPQ.LinearAlgebra`, `FLPQ.GraphAnalysis`
- Move `RPQTests.fs` from `tests/FLPQ.Languages.Tests/` to `tests/FLPQ.RPQ.Tests/`
  - Update `open` statements: add `open FLPQ.RPQ`, `open FLPQ.GraphAnalysis`
  - Update tests to use NFA-based interfaces
  - Update GraphReader tests to check NFA properties instead of LabeledGraph properties
  - Update `smallGraph` helper to return NFA or adapt to NFA
- Remove `RPQTests.fs` from `tests/FLPQ.Languages.Tests/FLPQ.Languages.Tests.fsproj`

### 7. Update solution file

- Add new projects to `FLPQ.slnx`:
  ```xml
  <Folder Name="/src/">
    ...
    <Project Path="src/FLPQ.GraphAnalysis/FLPQ.GraphAnalysis.fsproj" />
    <Project Path="src/FLPQ.RPQ/FLPQ.RPQ.fsproj" />
  </Folder>
  <Folder Name="/tests/">
    ...
    <Project Path="tests/FLPQ.GraphAnalysis.Tests/FLPQ.GraphAnalysis.Tests.fsproj" />
    <Project Path="tests/FLPQ.RPQ.Tests/FLPQ.RPQ.Tests.fsproj" />
  </Folder>
  ```

### 8. Update documentation

- `docs/architecture.md`: Add FLPQ.GraphAnalysis and FLPQ.RPQ projects, update file listings
- `docs/main.md`: Add links to new modules if needed, update existing links
- Individual module docs: update namespace in docs (msbfs.md, belyanin-rpq.md, arroyuelo-rpq.md, kronecker-rpq.md, graph-reader.md)
- `docs/matrix.md`: Document the new `fold` function

### 9. Verify

- `dotnet fantomas .` — format
- `dotnet build -c Release` — compile
- `dotnet test` — all tests pass
- `dotnet test --filter "Category=TexCompilation"` — TeX tests pass (if applicable)

## Files to CREATE

| File | Description |
|------|-------------|
| `src/FLPQ.GraphAnalysis/FLPQ.GraphAnalysis.fsproj` | New project for MS-BFS |
| `src/FLPQ.GraphAnalysis/MsBfs.fs` | Moved from FLPQ.LinearAlgebra |
| `src/FLPQ.RPQ/FLPQ.RPQ.fsproj` | New project for RPQ algorithms |
| `src/FLPQ.RPQ/GraphReader.fs` | Moved from FLPQ.Languages |
| `src/FLPQ.RPQ/BelyaninRPQ.fs` | Moved from FLPQ.Languages |
| `src/FLPQ.RPQ/ArroyueloRPQ.fs` | Moved from FLPQ.Languages |
| `src/FLPQ.RPQ/KroneckerRPQ.fs` | Moved from FLPQ.Languages |
| `tests/FLPQ.GraphAnalysis.Tests/FLPQ.GraphAnalysis.Tests.fsproj` | New test project |
| `tests/FLPQ.GraphAnalysis.Tests/MsBfsTests.fs` | Moved from FLPQ.LinearAlgebra.Tests |
| `tests/FLPQ.RPQ.Tests/FLPQ.RPQ.Tests.fsproj` | New test project |
| `tests/FLPQ.RPQ.Tests/RPQTests.fs` | Moved from FLPQ.Languages.Tests |

## Files to MODIFY

| File | Change |
|------|--------|
| `src/FLPQ.LinearAlgebra/Matrix.fs` | Add `fold` function |
| `src/FLPQ.LinearAlgebra/FLPQ.LinearAlgebra.fsproj` | Remove MsBfs.fs |
| `src/FLPQ.Languages/FLPQ.Languages.fsproj` | Remove GraphReader.fs, BelyaninRPQ.fs, ArroyueloRPQ.fs, KroneckerRPQ.fs |
| `tests/FLPQ.LinearAlgebra.Tests/FLPQ.LinearAlgebra.Tests.fsproj` | Remove MsBfsTests.fs |
| `tests/FLPQ.Languages.Tests/FLPQ.Languages.Tests.fsproj` | Remove RPQTests.fs |
| `FLPQ.slnx` | Add new projects |
| `docs/architecture.md` | Update structure |
| `docs/main.md` | Update links |
| `docs/matrix.md` | Document `fold` |
| `docs/msbfs.md` | Update namespace |
| `docs/graph-reader.md` | Update namespace, return type |
| `docs/belyanin-rpq.md` | Update namespace, interface |
| `docs/arroyuelo-rpq.md` | Update namespace, interface |
| `docs/kronecker-rpq.md` | Update namespace, interface |

## Files to DELETE

| File | Reason |
|------|--------|
| `src/FLPQ.LinearAlgebra/MsBfs.fs` | Moved to FLPQ.GraphAnalysis |
| `src/FLPQ.Languages/GraphReader.fs` | Moved to FLPQ.RPQ |
| `src/FLPQ.Languages/BelyaninRPQ.fs` | Moved to FLPQ.RPQ |
| `src/FLPQ.Languages/ArroyueloRPQ.fs` | Moved to FLPQ.RPQ |
| `src/FLPQ.Languages/KroneckerRPQ.fs` | Moved to FLPQ.RPQ |
| `tests/FLPQ.LinearAlgebra.Tests/MsBfsTests.fs` | Moved to FLPQ.GraphAnalysis.Tests |
| `tests/FLPQ.Languages.Tests/RPQTests.fs` | Moved to FLPQ.RPQ.Tests |

## Dependency Graph (Updated)

```
FLPQ.LinearAlgebra
    ↑               ↑
    │               │
    │         FLPQ.GraphAnalysis
    │               ↑
FLPQ.Languages     │
    ↑               │
    └───→ FLPQ.RPQ ─┘
             ↑
      FLPQ.RPQ.Tests
             ↑
      FLPQ.Cli (unchanged, no RPQ usage)
```
