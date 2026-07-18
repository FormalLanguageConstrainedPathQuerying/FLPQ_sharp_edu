# Coding Conventions

**Tags:** guide, coding-conventions, style, naming, genericity, immutability, documentation
**Kind:** guide

> **Abstract:** Defines the project's coding conventions: PascalCase for types/modules/record fields, camelCase for functions/values, immutability-first, types+modules at same level, maximal genericity over hardcoded types, non-empty collections by type (NonEmptyList/NonEmptySet), XML documentation comments for public APIs. Every implementation must be directly traceable to a specific algorithm or example in the book.

## Contents

- [Why conventions matter](#why-conventions-matter)
- [What our conventions are](#what-our-conventions-are)

## See Also

- [Design Guides](design-guides.md) | [Quality Standards](quality-standards.md)
- [Documentation Conventions](documentation-conventions.md)

## Why conventions matter

This project is supplementary material for a book on formal language constrained path querying. Every implementation must be directly traceable to a specific algorithm or example in the book. Consistent, predictable code style ensures that:

- A reader can match a code listing in the book to a source file and back without friction
- Algorithms are written as clear reference implementations, not optimized production code
- Generic code can be reused across chapters (alphabet types change, algorithm structure stays the same)

## What our conventions are

### Casing

- **PascalCase** for types, modules, record fields, union case fields
- **camelCase** for functions and values

This follows standard F# conventions. The PascalCase/camelCase split is chosen over alternatives (e.g., snake_case, all-PascalCase) because it matches both the .NET ecosystem and the mathematical notation in the book: types correspond to named sets (`Graph`, `Grammar`, `Automaton`), while functions correspond to operations (`intersect`, `accept`, `parse`).

### Immutability-first

Prefer `let` bindings over mutable state. Use structs with explicit field names, not tuples.

**Why**: Functional style with immutable data closely mirrors mathematical definitions. A type with named fields maps to a mathematical tuple with labeled components. Mutability is used only where an algorithm explicitly requires in-place state (e.g., mutable tree construction in LL parsing).

### Types + modules at the same level

```fsharp
type Automata = ...
module Automata =
    let intersect = ...
    let accept = ...
```

Types may be classes when necessary (e.g., `MutableTree` for in-place construction).

**Why**: This pattern separates the *shape* of data (type) from *operations on it* (module), mirroring algebraic structures in mathematics. It also avoids the deep nesting and `this` indirection of OOP-style class methods, making algorithms easier to read alongside the book.

### Documentation

XML documentation comments (`///`) for public APIs. Comments in English.

**Why**: XML doc comments surface in IDE tooltips and produce compiler warnings for undocumented public members. English is the language of the book and the F# ecosystem.

### Units of measure

Use units of measure where applicable.

**Why**: When an algorithm involves physical or logical dimensions (e.g., matrix row/column counts vs. element indices), units of measure make the distinction compile-time checkable, preventing index swaps and off-by-one errors.

### Maximal genericity

When writing a function `f : 'a -> 'b`, ask: can `'a` or `'b` be more general? If a function only needs `map` on `'a`, it should accept any functor, not just `list` or `Matrix`.

**Why**: Over-constrained type signatures limit reuse. A function that works on `list` but only uses `map` should also work on `array`, `seq`, or custom types — generic constraints document the minimal requirements and maximize applicability.

### Genericity over hardcoded types

All code must be as generic as possible:

| Domain | Generic over | Must NOT be |
|--------|-------------|-------------|
| Parsing algorithms (CYK, Valiant, LL, LR) | Terminal `'t`, Nonterminal `'nt` | `string`-based `Symbol`/`Terminal`/`Nonterminal` |
| Matrix operations (`mxm`, `kron`, `transpose`) | Element type `'a`, `'b`, `'c` | `bool` or `int` |
| Graphs and automata | Vertex and edge label types | Hardcoded vertex/label types |
| Visualization and printing | Symbol-printer function `'a -> string` | `sprintf "%A"` or type-specific formatting |

**Why**: The book covers multiple alphabets: Boolean matrices (Chapter 3), parse tables (Chapter 7), regex-labeled graphs (Chapter 11). Hardcoding to one alphabet requires rewriting algorithms for each chapter. Genericity makes one implementation serve all.

### Non-empty collections by type, not by runtime check

Use `NonEmptyList<'t>` and `NonEmptySet<'t>` from FSharpPlus for any collection that semantically must not be empty. Never use empty lists/sets with runtime checks when the type can enforce the invariant.

**Why**: A grammar must have at least one rule. An RSM must have at least one block. A non-empty invariant enforced by the type system eliminates a class of bugs and makes preconditions explicit in function signatures.

### Unit test instantiation

Unit tests may instantiate generic types at `string` for readability, but the implementation must never depend on it.

**Why**: Tests must be readable without decoding abstract type variables. But `string` is a concrete type — if the implementation requires `string` (e.g., calls `.Length` on a terminal), the genericity is broken.

## See Also

- [Design Guides](design-guides.md)
- [Quality Standards](quality-standards.md)
- [Documentation Conventions](documentation-conventions.md)
