# Detailed Plan: Task 50 — LL(k) table TeX visualization

## Function

Add `LLParser.tableToTeX` to `LLParser.fs`:

```fsharp
val tableToTeX:
    (Symbol<'t,'nt> -> string) ->
    Grammar<'t,'nt> ->
    int ->
    Map<Nonterminal<'nt>, Set<Symbol<'t,'nt> list>> ->
    Map<Nonterminal<'nt>, Set<Symbol<'t,'nt> list>> ->
    Map<Nonterminal<'nt> * Symbol<'t,'nt> list, int> ->
    string
```

## Algorithm

1. Extract nonterminals (ordered by first occurrence in grammar)
2. Extract terminals (from all RHS, deduplicated)
3. Build column spec: `r || c | c || c | c | ... | c`
4. Build header row: N, FIRST, FOLLOW, terminals, $
5. Build data rows: for each nonterminal, fill columns

## Set rendering

- `$\{a, \varepsilon\}$` or `$\varnothing$` for empty
- Epsilon display: `$\varepsilon$`

## Rule rendering

- `$S \rightarrow a S b S$` or `$S \rightarrow \varepsilon$`
- Use `\rightarrow`, not `\to`

## Tests

1. LL(1) table for S → aSbS | eps — check TeX compiles, check structure
2. LL(1) table for grammar7 (E → E+T | T, T → T*F | F, F → (E) | x) — multiple nonterminals — check rows and entries
