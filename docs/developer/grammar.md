# Grammar Module

**Tags:** algorithm, grammar, parsing, cfg, Chomsky Normal Form, fixed-point, epsilon-elimination
**Kind:** algorithm
**Module:** Grammar
**Source:** `src/FLPQ.Languages/Grammar.fs`
**Depends on:** _(none)_
**Used by:** Cyk, Valiant, LLParser, LRParser, FirstFollow, Tokenizer
**Book reference:** Chapters 5, 7

> **Abstract:** Provides generic types for representing context-free grammars (`Terminal<'t>`, `Nonterminal<'nt>`, `Symbol<'t,'nt>`, `Rule<'t,'nt>`, `Grammar<'t,'nt>`) and a parser for `.bnf` grammar files. Implements Chomsky Normal Form (CNF) transformation: epsilon elimination, unit production elimination, terminal replacement, and binarization. Also provides `ExtendedGrammar` wrapper for grammar augmentation.

## Contents

- [Algorithm: CNF Transformation](#algorithm-cnf-transformation)
- [Type Definitions](#type-definitions)
- [Function Signatures](#function-signatures)
- [BNF File Format](#bnf-file-format)
- [ExtendedGrammar](#extendedgrammar)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Algorithm: CNF Transformation

### `toCnf`
```fsharp
val toCnf: g:Grammar<string, string> -> Grammar<string, string>
```

Transforms a context-free grammar into Chomsky Normal Form. In CNF all production rules have one of three forms:
- `A → BC` (two nonterminals)
- `A → a` (single terminal)
- `S → ε` (start symbol only, only if the language contains ε)

**Transformation steps:**

1. **Epsilon elimination**: Computes nullable nonterminals (transitive closure), generates all combinations of RHS with nullable symbols optionally removed, adds a fresh start symbol if the language contains ε.

2. **Unit production elimination**: Computes unit pairs (transitive closure of A → B relations), adds new rules bypassing unit chains, removes all single-nonterminal RHS rules.

3. **Terminal replacement**: For each terminal appearing in a RHS with length > 1, creates a fresh nonterminal `T_a → a` and replaces the terminal occurrence with the fresh nonterminal.

4. **Binarization**: For each rule with |RHS| > 2, introduces fresh nonterminals to chain binary productions. `A → X₁ X₂ … Xₙ` becomes `A → X₁ N₁, N₁ → X₂ N₂, …, N_{n-2} → X_{n-1} Xₙ`.

**Postcondition**: All rules in the result satisfy CNF constraints.

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
Sum type representing a terminal, a nonterminal, or epsilon (the empty string ε). The explicit `Epsilon` case allows epsilon to be represented as a symbol itself, eliminating the need for ad-hoc empty-list or empty-string representations.

### `Rule<'t, 'nt>`
```fsharp
type Rule<'t, 'nt> = { lhs: Nonterminal<'nt>; rhs: Symbol<'t, 'nt> list }
```
A production rule with a left-hand side nonterminal and a right-hand side sequence of symbols. An epsilon-production is represented as `rhs = [Epsilon]`.

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

**Format rules:**
- One rule per line, empty lines ignored
- Each line has the form `<nonterm> -> <symbols>` or `<nonterm> -> eps`
- Left-hand side: single PascalCase identifier
- `eps` denotes an epsilon production, represented as `[Epsilon]`
- Symbols classified by naming: PascalCase → `Nonterminal`, camelCase → `Terminal`

### `parseGrammarFromFile`
```fsharp
val parseGrammarFromFile: path:string -> Grammar<string, string>
```
Reads a `.bnf` file and delegates to `parseGrammar`.

### `toCnf`
```fsharp
val toCnf: g:Grammar<string, string> -> Grammar<string, string>
```
Transform a CFG into Chomsky Normal Form (described above).

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

## ExtendedGrammar

### `ExtendedGrammar<'t, 'nt>`
```fsharp
type ExtendedGrammar<'t, 'nt> =
    { originalGrammar: Grammar<'t, 'nt>
      freshStart: Nonterminal<'nt>
      extended: Grammar<'t, 'nt> }
```
An augmented grammar with fresh start `S'`. The extended grammar has `S' -> S` as the first rule. The type wraps both the original and augmented grammars, providing uniform access to the original start.

### Module helpers

| Function | Signature | Description |
|----------|-----------|-------------|
| `create` | `Nonterminal<'nt> -> Grammar<'t,'nt> -> ExtendedGrammar<'t,'nt>` | Creates an extended grammar |
| `originalGrammar` | `ExtendedGrammar<'t,'nt> -> Grammar<'t,'nt>` | Returns the original grammar |
| `freshStart` | `ExtendedGrammar<'t,'nt> -> Nonterminal<'nt>` | Returns the fresh start nonterminal |
| `extGrammar` | `ExtendedGrammar<'t,'nt> -> Grammar<'t,'nt>` | Returns the augmented grammar |
| `originalStart` | `ExtendedGrammar<'t,'nt> -> Nonterminal<'nt>` | Returns the original start nonterminal |

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Generic `Terminal<'t>`, `Nonterminal<'nt>` | Enables use with non-string identifiers (e.g., structured token types) |
| `Symbol` uses case labels `T` and `N` | Avoids naming conflicts with the `Terminal` and `Nonterminal` types |
| Tokens classified by PascalCase/camelCase convention | Simple, unambiguous parsing without explicit type annotations in the grammar file |
| `start` inferred from first rule | Standard BNF convention; no explicit start symbol marker needed |
| Fresh nonterminal names prefixed `N_CNF_` | Prevents name collisions with user-defined nonterminals |
| Fixed-point computation for nullable/unit pairs | Standard Hopcroft-Ullman approach; guaranteed termination on finite grammars |
| New start symbol always introduced in CNF | Simplifies epsilon handling; ensures start doesn't appear on any RHS |
| `ExtendedGrammar` as wrapper type | Preserves original-extended relationship; eliminates need for parallel variables in runners |

## Book Reference

Context-free grammar representation is fundamental to the CNF transformation and CYK/Valiant/LL/LR parsing algorithms. The EOI (end-of-input) symbol `$` is explicitly added to token streams in all algorithm runners, making the end-of-input condition visible in visualization steps.

## See Also

- [CYK algorithm](cyk.md) — uses CNF-transformed grammars
- [Valiant algorithm](valiant.md) — uses CNF-transformed grammars
- [LL parser module](ll-parser.md) — uses Grammar types
- [LR parser module](lr-parser.md) — uses ExtendedGrammar
- [Tokenizer module](tokenizer.md) — string-to-symbol tokenization
