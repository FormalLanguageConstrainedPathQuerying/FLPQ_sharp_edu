# Detailed Plan: Task 54 — EBNF Grammar Reading and RSM Construction

## Overview
Implement EBNF grammar parsing (`.ebnf` files) and RSM construction using Brzozowski derivatives. The book defines EBNF as grammars where right-hand sides are regular expressions over `Σ ∪ N`.

## Design Decisions

### 1. EBNF Parser
- Use FParsec for parsing
- Two-stage: parse into regex AST, group rules by nonterminal
- Nonterminals: PascalCase (uppercase first), terminals: camelCase
- Operators: `|`, `*`, `+`, `?`, `(...)`, `eps`
- Multiple rules with same LHS → join with `|`

### 2. Regular Expression AST
- Epsilon, Empty, Terminal, Nonterminal, Sequence, Alternative, Star
- Plus = Sequence(x, Star x), Optional = Alternative(x, Epsilon)

### 3. Brzozowski Derivatives
- `derive(regexp, symbol)` — symbolic derivative
- `nullable(regexp)` — does regexp match empty string?
- `getSymbols(regexp)` — all symbols in the expression
- Build DFA: closure under derivatives, nullable states are final

### 4. RSM Construction Pipeline
1. Parse EBNF → regex AST per nonterminal
2. For each nonterminal, build DFA via Brzozowski derivatives
3. Relabel nonterminal edges → block start states
4. Assemble RSM

## Files to Create/Modify
1. `src/FLPQ.Languages/EbnfParser.fs` — EBNF parser + regex AST + derivatives
2. `src/FLPQ.Languages/FLPQ.Languages.fsproj` — add compile and FParsec ref
3. `tests/FLPQ.Languages.Tests/EbnfParserTests.fs` — tests
4. `docs/ebnf-parser.md` — documentation
