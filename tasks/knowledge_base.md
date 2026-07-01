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

### Property-Based Tests from Task Spec

When a task specification states that certain constructs "can be used for property-based tests", the intended implementation is FsCheck `[<Property>]` tests with generated random inputs. Do NOT replace them with `[<Fact>]` tests iterating over hardcoded strings or values. The generators must cover the input space relevant to the property (bounded randomization is acceptable when exhaustive generation is impractical, e.g., bounding string length or matrix dimensions).

## nicematrix v6+ (2024+) `\Block` Syntax

**Problem**: nicematrix versions 6.x and later (distributed with TeX Live 2024+) use a different `\Block` syntax than older versions. The old positional syntax `\Block[draw=red]{r1-c1-r2-c2}{}` was removed.

**New syntax** (v6+): `\Block[draw=red]{rows-cols}{content}`
- `rows`: number of rows the block spans
- `cols`: number of columns the block spans
- `content`: the content displayed in the block (overrides the top-left cell content)

**Placement**: `\Block` must be placed at the top-left cell of the block within the matrix, not before the matrix. In `toTeXStyled`, this means merging the `\Block` command into the cell content string at the block's starting position.

**Resolved in**: Matrix.fs `toTeXStyled` function (task 52), which generates `\Block[draw=color]{rowCount-colCount}{styledCellContent}` at the appropriate cell.

## F# Record Type Ambiguity in Module with Shadowed Name

**Problem**: When a module defines its own record type (e.g., `type Submatrix = { A: int; B: int; Size: int }` in Valiant module), constructing a record from a different namespace (e.g., `Matrix.SubmatrixBlock`) using qualified field syntax (`{ Matrix.SubmatrixBlock.field = val }`) may fail because F# resolves the record type based on the first field name and finds an ambiguous match.

**Solution**: Use a separate `let` binding with an explicit type annotation:
```fsharp
let block: Matrix.SubmatrixBlock =
    { startRow = r
      startCol = c
      ... }
```

## FSharpPlus 1.9.1 `NonEmptySet` API

### `NonEmptySet.intersect` — Not Available

`NonEmptySet.intersect` does not exist in FSharpPlus 1.9.1. To intersect two `NonEmptySet<'a>` values, convert to plain `Set<'a>` via `NonEmptySet.toSet`, use `Set.intersect`, check for emptiness, and convert back via `NonEmptySet.ofSet` (which returns `NonEmptySet<'a>` directly, not `Option`).

```fsharp
let common = Set.intersect (NonEmptySet.toSet nesA) (NonEmptySet.toSet nesB)
if Set.isEmpty common then None
else Some(NonEmptySet.ofSet common)
```

### `NonEmptySet.ofSet` Returns `NonEmptySet<'a>`, Not `Option`

Despite what some FSharpPlus documentation suggests, in v1.9.1 `NonEmptySet.ofSet : Set<'a> -> NonEmptySet<'a>` returns the value directly (throws on empty). Always check for emptiness before calling.

### Available Members

| Function | Signature | Notes |
|----------|-----------|-------|
| `NonEmptySet.singleton` | `'a -> NonEmptySet<'a>` | Create singleton. Used in `Nfa.buildMatrix`. |
| `NonEmptySet.add` | `'a -> NonEmptySet<'a> -> NonEmptySet<'a>` | Add element. |
| `NonEmptySet.contains` | `'a -> NonEmptySet<'a> -> bool` | Membership test. |
| `NonEmptySet.toSet` | `NonEmptySet<'a> -> Set<'a>` | Convert to plain Set. |
| `NonEmptySet.toSeq` | `NonEmptySet<'a> -> seq<'a>` | Enumerate elements. |
| `NonEmptySet.ofSet` | `Set<'a> -> NonEmptySet<'a>` | Convert from Set. Throws if empty. |

## Fish Speculative Import Hunting

When searching for usages of a removed field or renamed function, prefer `grep` with narrow patterns over glob-based file reading. Example: to find all direct field accesses of `epsTransitions`, use `grep "\.epsTransitions"` — this catches property accesses while excluding `Set.empty` passed as a parameter.

