# Detailed Plan: Task 004 — BNF Grammar Reading

## Overview
Add generic types for terminals/nonterminals/symbols and a parser for `.bnf` grammar files.

## 1. Types

```fsharp
type Terminal<'t> = Terminal of 't
type Nonterminal<'nt> = Nonterminal of 'nt

type Symbol<'t, 'nt> =
    | Terminal of Terminal<'t>
    | Nonterminal of Nonterminal<'nt>

type Rule<'t, 'nt> = {
    lhs: Nonterminal<'nt>
    rhs: Symbol<'t, 'nt> list
}

type Grammar<'t, 'nt> = {
    rules: Rule<'t, 'nt> list
    start: Nonterminal<'nt>
}
```

## 2. BNF File Format

- One rule per line
- Empty lines allowed
- Format: `<nonterm> -> <symbols>` or `<nonterm> -> eps`
- Start nonterminal = left side of the first rule
- Nonterminal: PascalCase (starts with uppercase)
- Terminal: camelCase (starts with lowercase)
- `eps` = epsilon (empty right-hand side)
- Symbols separated by spaces on right-hand side

## 3. Parser

```fsharp
val parseGrammar: string -> Grammar<string, string>
val parseGrammarFromFile: string -> Grammar<string, string>
```

- `parseGrammar`: takes BNF text, returns Grammar
- `parseGrammarFromFile`: reads file, delegates to parseGrammar
- Both work with `string` identifiers (most common case)
- Token classification: `eps` → empty list; starts with uppercase → Nonterminal; otherwise → Terminal

## 4. Tests

- Parse simple grammar (3-4 rules)
- Parse grammar with eps
- Parse grammar with empty lines
- Verify start nonterminal
- Verify rule counts
- Roundtrip test: parse + check structure

## 5. Files

| File | Action |
|------|--------|
| `src/FLPQ.Core/Grammar.fs` | Create — types, parser |
| `src/FLPQ.Core/FLPQ.Core.fsproj` | Modify |
| `tests/FLPQ.Core.Tests/GrammarTests.fs` | Create |
| `tests/FLPQ.Core.Tests/FLPQ.Core.Tests.fsproj` | Modify |
| `docs/grammar.md` | Create |
| `docs/main.md`, `docs/architecture.md` | Modify |
| `tasks/tasks.md` | Mark task 4 done |
