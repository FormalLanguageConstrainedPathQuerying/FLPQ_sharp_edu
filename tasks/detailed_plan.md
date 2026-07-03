# Task 92: Common table TeX rendering for CYK and Valiant

## Analysis

CYK and Valiant both render tables containing sets of symbols:
- CYK cells: `Option<HashSet<Symbol<'t,'nt>>>` — `None` = empty, `Some set` = `\{...\}`
- Valiant cells: `Set<Nonterminal<'nt>>` — empty = `\cdot`, non-empty = `\{...\}`

The `\cdot` for empty / `\{...\}` for non-empty pattern is duplicated.

## Plan

1. Create `ParsingTableTeX` module in FLPQ.Printers with:
   - `setToTeX: ('a -> string) -> 'a seq -> string` — renders collection as `\{...\}` or `\cdot`
   - `optionSetToTeX: ('a -> string) -> ('a seq) option -> string` — renders optional collection

2. Update `CykTeX.fs` to use `ParsingTableTeX.optionSetToTeX`

3. Update `ValiantTeX.fs` to use `ParsingTableTeX.setToTeX`

4. Update fsproj, format, build, test
