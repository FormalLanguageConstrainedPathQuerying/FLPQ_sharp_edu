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
    | Epsilon
```
Sum type representing a terminal, a nonterminal, or epsilon (the empty string ε). The explicit `Epsilon` case allows epsilon to be represented as a symbol itself, following classical language theory conventions. This eliminates the need for ad-hoc empty-list or empty-string representations in first/follow computations and lookahead handling.

### `Rule<'t, 'nt>`
```fsharp
type Rule<'t, 'nt> = { lhs: Nonterminal<'nt>; rhs: Symbol<'t, 'nt> list }
```
A production rule with a left-hand side nonterminal and a right-hand side sequence of symbols. An epsilon-production is represented as `rhs = [Epsilon]` (a single-element list containing the Epsilon symbol).

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
- The right-hand side `eps` denotes an epsilon production, represented as `[Epsilon]`
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

### `toCnf`
```fsharp
val toCnf: g:Grammar<string, string> -> Grammar<string, string>
```
Transforms a context-free grammar into Chomsky Normal Form (CNF). In CNF all production rules have one of three forms:
- `A -> BC` (two nonterminals)
- `A -> a` (single terminal)
- `S -> ε` (start symbol only, only if the language contains ε)

**Transformation steps**:

1. **Epsilon elimination**: Computes nullable nonterminals (transitive closure), generates all combinations of RHS with nullable symbols optionally removed, adds a fresh start symbol if the language contains ε.

2. **Unit production elimination**: Computes unit pairs (transitive closure of A → B relations), adds new rules bypassing unit chains, removes all single-nonterminal RHS rules.

3. **Terminal replacement**: For each terminal appearing in a RHS with length > 1, creates a fresh nonterminal `T_a → a` and replaces the terminal occurrence with the fresh nonterminal.

4. **Binarization**: For each rule with |RHS| > 2, introduces fresh nonterminals to chain binary productions. `A → X₁ X₂ … Xₙ` becomes `A → X₁ N₁, N₁ → X₂ N₂, …, N_{n-2} → X_{n-1} Xₙ`.

**Postcondition**: All rules in the result satisfy CNF constraints.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Generic `Terminal<'t>`, `Nonterminal<'nt>` | Enables use with non-string identifiers (e.g., structured token types) |
| `Symbol` uses case labels `T` and `N` | Avoids naming conflicts with the `Terminal` and `Nonterminal` types |
| Tokens classified by PascalCase/camelCase convention | Simple, unambiguous parsing without explicit type annotations in the grammar file |
| `parseGrammar` returns `Grammar<string, string>` | Most common use case; generic types available for programmatic construction |
| `start` inferred from first rule | Standard BNF convention; no explicit start symbol marker needed |
| Fresh nonterminal names prefixed `N_CNF_` | Prevents name collisions with user-defined nonterminals |
| Fixed-point computation for nullable/unit pairs | Standard Hopcroft-Ullman approach; guaranteed termination on finite grammars |
| New start symbol always introduced in CNF | Simplifies epsilon handling; ensures start doesn't appear on any RHS |

## Relationship to the Book

Context-free grammar representation is fundamental to the Chomsky Normal Form transformation (Task 5) and the CYK parsing algorithm (Task 6).
