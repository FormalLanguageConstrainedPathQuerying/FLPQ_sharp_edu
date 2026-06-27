# Grammar Module

## Module Purpose

Provides generic types for representing context-free grammars and a parser for `.bnf` grammar files.

## Type Definitions

### `Terminal<'t>`
```fsharp
type Terminal<'t> = Terminal of 't
```
A terminal symbol wrapping a user-defined type `'t`. In the context of `.bnf` files, `'t` is typically `string`.

### `Nonterminal<'nt>`
```fsharp
type Nonterminal<'nt> = Nonterminal of 'nt
```
A nonterminal symbol wrapping a user-defined type `'nt`.

### `Symbol<'t, 'nt>`
```fsharp
type Symbol<'t, 'nt> =
    | T of Terminal<'t>
    | N of Nonterminal<'nt>
```
Sum type representing either a terminal or a nonterminal symbol. Case labels `T` and `N` avoid naming conflicts with the wrapper types.

### `Rule<'t, 'nt>`
```fsharp
type Rule<'t, 'nt> = { lhs: Nonterminal<'nt>; rhs: Symbol<'t, 'nt> list }
```
A production rule with a left-hand side nonterminal and a right-hand side sequence of symbols. An empty `rhs` list represents an epsilon-production.

### `Grammar<'t, 'nt>`
```fsharp
type Grammar<'t, 'nt> = { rules: Rule<'t, 'nt> list; start: Nonterminal<'nt> }
```
A context-free grammar consisting of a list of production rules and a designated start nonterminal.

## Function Signatures

### `parseGrammar`
```fsharp
val parseGrammar: text:string -> Grammar<string, string>
```
Parses a context-free grammar from BNF text.

**Format rules**:
- One rule per line
- Empty lines are ignored
- Each line has the form `<nonterm> -> <symbols>` or `<nonterm> -> eps`
- The left-hand side must be a single PascalCase identifier (starts with uppercase)
- The right-hand side `eps` denotes the empty production
- Otherwise, symbols on the right-hand side are separated by spaces
- Each symbol is classified by naming convention: PascalCase → `Nonterminal`, camelCase → `Terminal`

**Preconditions**:
- Input must contain at least one rule (throws `ArgumentException` otherwise)

**Postcondition**:
- `start` is the left-hand side of the first rule

### `parseGrammarFromFile`
```fsharp
val parseGrammarFromFile: path:string -> Grammar<string, string>
```
Reads a `.bnf` file and delegates to `parseGrammar`.

## BNF File Format

A `.bnf` file contains one production rule per line:
```
A -> B c
B -> d
B -> eps
```
- First rule's left-hand side is the start nonterminal
- Empty lines are allowed and ignored
- `eps` is the reserved keyword for epsilon

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Generic `Terminal<'t>`, `Nonterminal<'nt>` | Enables use with non-string identifiers (e.g., structured token types) |
| `Symbol` uses case labels `T` and `N` | Avoids naming conflicts with the `Terminal` and `Nonterminal` types |
| Tokens classified by PascalCase/camelCase convention | Simple, unambiguous parsing without explicit type annotations in the grammar file |
| `parseGrammar` returns `Grammar<string, string>` | Most common use case; generic types available for programmatic construction |
| `start` inferred from first rule | Standard BNF convention; no explicit start symbol marker needed |

## Relationship to the Book

Context-free grammar representation is fundamental to the Chomsky Normal Form transformation (Task 5) and the CYK parsing algorithm (Task 6).
