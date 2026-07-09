---
name: fsharp-coder
description: Use when writing F# code: language-specific patterns, idioms, gotchas, and library API quirks. Covers FsCheck API quirks (Gen shadowing, naming, overloads), closure patterns, record ambiguity, NonEmptySet API, optional parameters, pattern annotations, Argu flags, and CLI entry points.
---

# F# Coding Patterns and Quirks

## FsCheck API Quirks

These are F#-language interaction issues when using the FsCheck library. For FsCheck testing methodology (Property attribute, generators, registration), see the `tests-writer` skill.

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

## Closure Capture Patterns

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

**Contrast with buggy version**: Capturing only an immutable `existing` set and using a mutable counter leads to collisions when `fresh()` is called multiple times, because the counter alone doesn't guarantee uniqueness against previously-generated names.

## Record Type Ambiguity in Module with Shadowed Name

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

`NonEmptySet.intersect` does not exist. To intersect two `NonEmptySet<'a>` values, convert to plain `Set<'a>`, use `Set.intersect`, check for emptiness, and convert back:

```fsharp
let common = Set.intersect (NonEmptySet.toSet nesA) (NonEmptySet.toSet nesB)
if Set.isEmpty common then None
else Some(NonEmptySet.ofSet common)
```

### `NonEmptySet.ofSet` Returns `NonEmptySet<'a>`, Not `Option`

In FSharpPlus 1.9.1, `NonEmptySet.ofSet : Set<'a> -> NonEmptySet<'a>` returns the value directly (throws on empty). Always check for emptiness before calling.

### Available Members

| Function | Signature | Notes |
|----------|-----------|-------|
| `NonEmptySet.singleton` | `'a -> NonEmptySet<'a>` | Create singleton |
| `NonEmptySet.add` | `'a -> NonEmptySet<'a> -> NonEmptySet<'a>` | Add element |
| `NonEmptySet.contains` | `'a -> NonEmptySet<'a> -> bool` | Membership test |
| `NonEmptySet.toSet` | `NonEmptySet<'a> -> Set<'a>` | Convert to plain Set |
| `NonEmptySet.toSeq` | `NonEmptySet<'a> -> seq<'a>` | Enumerate elements |
| `NonEmptySet.ofSet` | `Set<'a> -> NonEmptySet<'a>` | Convert from Set. Throws if empty |

## Module-Level Optional Parameters — Not Allowed (FS0718)

**Problem**: Optional parameters with `?` syntax work only on type members (methods), not on module-level `let` bindings. Error FS0718: "Optional arguments are only permitted on type members."

**Solution** — use one of:

- **Two overloaded functions** (recommended): one with the flag, one without:

  ```fsharp
  let grammarToTeX (g: Grammar<'t, 'nt>) = renderGrammar false g
  let grammarToTeXWithNumbers (g: Grammar<'t, 'nt>) = renderGrammar true g
  let private renderGrammar (showNumbers: bool) (g: Grammar<'t, 'nt>) = ...
  ```

- **Explicit `bool` parameter**: accept the flag as a required parameter
- **`Option<'T>` parameter**: accept `Option<bool>` explicitly

## Pattern Type Annotation Syntax

`let f (Terminal t: string) = t` is parsed as `let f ((Terminal t): string) = t` — the annotation applies to the whole pattern, not the inner binding. To annotate the inner binding, use explicit parentheses: `let f (Terminal (t: string)) = t`. The simpler form `let f (Terminal t) = t` lets type inference handle the inner type.

## Argu Flag (No-Payload) Union Cases in `IArgParserTemplate`

For a flag union case like `| Summary`, the `Usage` property match must use `| Summary` (not `| Summary _`). Using `Summary _` produces warning FS3548: "Pattern discard is not allowed for union case that takes no data."

## Testable CLI Entry Points

An `[<EntryPoint>]` function should delegate to a separate `runCli : string[] -> int` function. Calling `System.Environment.Exit` directly inside the entry point terminates the test runner when the CLI is invoked from tests. The wrapper pattern (`runCli` returns int, `main` delegates) lets tests call `Program.runCli args` and assert on the return code without killing the process.

## Code Search Tips

### Speculative Import Hunting

When searching for usages of a removed field or renamed function, prefer `grep` with narrow patterns over glob-based file reading. Example: to find all direct field accesses of `epsTransitions`, use `grep "\.epsTransitions"` — this catches property accesses while excluding `Set.empty` passed as a parameter.
