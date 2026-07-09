---
name: tests-writer
description: Use when writing tests: FsCheck property-based tests, golden/snapshot tests, test generators, and FsCheck.Xunit integration. Covers FsCheck API, Arbitrary/Gen patterns, generator registration, and golden test workflow. For FsCheck API quirks (Gen shadowing, naming, overloads), see the fsharp-coder skill.
---

# Writing Tests

## FsCheck API

### API Changes from FsCheck 2.x to 3.x

- `Arb` module is removed. Use `FsCheck.FSharp.Arb` module instead
- `Arbitrary<'T>` class has only a private default constructor. Create instances via `Arb.fromGen`
- `Arb.Default.Array2D<'a>()` exists but generates arrays of arbitrary (potentially large) size
- `Arb.registerByType` is removed. Use the attribute-based approach instead

### FsCheck.Xunit Integration

`[<Property>]` attribute for property-based tests — FsCheck generates random inputs.

`[<Properties(Arbitrary = [| typeof<MyGen> |])>]` to register custom generators for a module.

Generator class must have static methods returning `Arbitrary<'T>`:

```fsharp
type MatrixGenerators =
    static member Matrix(): Arbitrary<Matrix<int>> =
        ... |> MyArb.fromGen

[<Properties(Arbitrary = [| typeof<MatrixGenerators> |])>]
module PropertyTests = ...
```

### Test Requirements

Property-based tests:

- When a task specification states that certain constructs "can be used for property-based tests", implement `[<Property>]` tests with generated random inputs
- Do not substitute `[<Fact>]` tests enumerating hardcoded examples
- Property-based tests MUST use FsCheck `Arbitrary`/`Gen` types with `[<Property>]`. Never use `System.Random.Shared` in manual `for`-loops

Equivalence tests:

- Every new algorithm variant must include property-based equivalence tests proving identical results to at least one existing reference implementation
- Example: "standard Valiant ≡ modified Valiant", "Belyanin ≡ Arroyuelo ≡ Kronecker+MS-BFS"

Shared generators:

- FsCheck generators for shared project types (matrices, graphs, grammars, regexes) must live in a common `Generators.fs` module
- Do not duplicate random generation logic across test projects

## Golden (Snapshot) Tests in .NET xUnit

Golden tests compare generated output against committed reference files. If the output changes intentionally, update the reference files. If it changes unintentionally, the test catches the regression.

### Pattern

1. Reference files stored in a `GoldenData/` subdirectory of the test project
2. In `.fsproj`, golden files are included as `<Content>` with `CopyToOutputDirectory="PreserveNewest"`
3. At runtime, tests access golden files relative to the current working directory (the output directory)
4. **Auto-generation on first run**: if the golden file does not exist, the test writes the generated content and fails with instructions to copy it to the source tree

```fsharp
let private goldenDataDir = Path.Combine(Directory.GetCurrentDirectory(), "GoldenData")

let private verifyGolden (goldenFileName: string) (actualContent: string) =
    let goldenPath = Path.Combine(goldenDataDir, goldenFileName)
    if File.Exists goldenPath then
        let expected = File.ReadAllText goldenPath
        Assert.Equal(expected, actualContent)
    else
        Directory.CreateDirectory goldenDataDir |> ignore
        File.WriteAllText(goldenPath, actualContent)
        Assert.True(false, $"Golden file '{goldenFileName}' was created. Copy it to the source GoldenData/ and re-run.")
```

### .fsproj Content Entry (Glob)

```xml
<Content Include="GoldenData\*.tex">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

The glob automatically picks up all golden files without listing each one individually.

### Workflow

1. Run tests. Golden files are created in `bin/.../GoldenData/`
2. Copy them to `tests/<Project>/GoldenData/` in the source tree
3. Commit the golden files
4. Subsequent test runs compare against the committed files (copied to output by msbuild)
