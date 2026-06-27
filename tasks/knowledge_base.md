# Knowledge Base

Accumulated knowledge about libraries, frameworks, and tooling discovered during implementation. Prevents re-discovery of API quirks and workarounds.

## FsCheck 3.x (3.3.3)

### API Changes from FsCheck 2.x
- `Arb` module is removed. Use `FsCheck.FSharp.Arb` module instead.
- `Arbitrary<'T>` class has only a private default constructor. Create instances via `Arb.fromGen`.
- `Arb.Default.Array2D<'a>()` exists but generates arrays of arbitrary (potentially large) size.

### `Gen<'T>` Type vs `Gen` Module Shadowing

**Problem**: `open FsCheck` brings the generic type `Gen<'T>` into scope. When also opening `FsCheck.FSharp`, the `Gen` module is shadowed by the type, making module functions inaccessible.

```
Gen.Choose(1, 5)  // ERROR: 'Gen' refers to the type, not the module
```

**Solution**: Create a module alias with a different name:

```fsharp
module MyGen = FsCheck.FSharp.Gen
module MyArb = FsCheck.FSharp.Arb

MyGen.choose(1, 5)   // Works
```

### F# Compile-Time vs Runtime Names

Functions in `FsCheck.FSharp.Gen` have **lowercase** F# source names but **PascalCase** CLR names:

| F# Source Name | CLR Name | Usage |
|----------------|----------|-------|
| `choose` | `Choose` | `MyGen.choose(1, 5)` |
| `bind` | `Bind` | `MyGen.bind` |
| `map` | `Map` | `MyGen.map` |
| `listOfLength` | `ListOf` | `MyGen.listOfLength` |
| `fromGen` | `From` | `MyArb.fromGen` |

This is the standard F# automatic PascalCasing for CLR visibility. Always use lowercase at the F# call site.

### `Gen.array2DOf` Overload Ambiguity

**Problem**: `Gen.array2DOf` has two overloads:
1. `array2DOf(elementGen: Gen<'T>)` — generates array of random dimensions
2. `array2DOf(rows: int, cols: int, elementGen: Gen<'T>)` — generates array of fixed dimensions

F# cannot disambiguate these overloads at compile time. Both `MyGen.array2DOf(3, 3, elemGen)` (tupled) and `MyGen.array2DOf 3 3 elemGen` (curried) fail.

**Workaround**: Use `Gen.listOfLength` to generate a flat list, then reshape manually:

```fsharp
MyGen.choose(-100, 100)
|> MyGen.listOfLength (rows * cols)
|> MyGen.map (fun values ->
    Array2D.init rows cols (fun i j -> values.[i * cols + j]))
```

### FsCheck.Xunit Integration

- `[<Property>]` attribute for property-based tests — FsCheck generates random inputs
- `[<Properties(Arbitrary = [| typeof<MyGen> |])>]` to register custom generators for a module
- Generator class must have static methods returning `Arbitrary<'T>`:

## F# Closure Capture Patterns

### Mutable Set in Closure for Fresh Name Generation

When generating unique names, a closure that captures a mutable `used` set of already-taken names ensures each generated name is unique:

```fsharp
let mutable used = existing
fun () ->
    let rec loop n =
        let candidate = Nonterminal($"N_CNF_{n}")
        if Set.contains candidate used then loop (n+1)
        else
            used <- Set.add candidate used
            candidate
    loop 1
```

**Contrast with buggy version**: Capturing only an immutable `existing` set and using a mutable counter leads to collisions when `fresh()` is called multiple times, because the counter alone doesn't guarantee uniqueness against previously-generated names (e.g., counter starts at 1, generates N_CNF_1 which skips existing but doesn't track it; next call with counter=2 may generate N_CNF_2 which was also generated in the first call if N_CNF_1 was in existing and N_CNF_2 wasn't).


```fsharp
type MatrixGenerators =
    static member Matrix(): Arbitrary<Matrix<int>> =
        ... |> MyArb.fromGen

[<Properties(Arbitrary = [| typeof<MatrixGenerators> |])>]
module PropertyTests = ...
```

### `Arb.registerByType` — Removed in 3.x

Use the attribute-based approach (`[<Properties(Arbitrary = ...)>]`) instead of the old `Arb.registerByType`.

## .NET 10.0

### `.slnx` Format
`dotnet new sln -n <name>` creates `<name>.slnx` by default in .NET 10.0, not the legacy `.sln` format. All `dotnet sln` commands work transparently with `.slnx` files.

## fantomas

### Installation
Install as a local tool (committed to repo) rather than globally:

```sh
dotnet new tool-manifest
dotnet tool install fantomas
```

This creates `dotnet-tools.json` (commit this file). Other developers run `dotnet tool restore`.

### Usage
```sh
dotnet fantomas .          # Format all F# files
dotnet fantomas . --check  # Check formatting without modifying
```

### CI
In CI, run `dotnet tool restore` before `dotnet fantomas . --check`.

## xUnit + F# Project

### Test Project Template
```sh
dotnet new xunit -lang F# -n <Name> -o <Path>
```

Default packages included: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`.

### Adding FsCheck
```sh
dotnet add package FsCheck.Xunit
```

This brings in `FsCheck` transitively.
