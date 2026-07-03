# Task 93: CYK and Valiant tables unification

## Plan

### 93.1: Create `ParsingTable<'nt>` type alias
- New file `src/FLPQ.Languages/ParsingTable.fs`
- `type ParsingTable<'nt when 'nt: comparison> = Matrix<Set<Nonterminal<'nt>>>`
- Add to fsproj before `Cyk.fs`

### 93.3: Refactor CYK
- Remove `CykCell` type alias
- Change `CykTraceStep` to `CykTraceStep<'nt when 'nt: comparison>` with `table: ParsingTable<'nt>`
- Change `findProducingRules` → `findTerminalRules` accepting `Terminal<'t>`
- Change `findBinaryProductions` to accept `Nonterminal<'nt>` directly
- `cykTable`/`tableTrace`: work with `Terminal<'t> list` and `Matrix<Set<Nonterminal<'nt>>>`
- `isAccepted` works with `ParsingTable<'nt>`
- Remove `Symbol` wrapping (`N nt`) from cell construction

### 93.3: Update Valiant
- Use `ParsingTable<'nt>` in `ValiantTraceStep`, `ModifiedValiantTraceStep`, return types

### 93.3: Update visualization
- `CykTeX.cellToTeX` renders `Set<Nonterminal<'nt>>` via `ParsingTableTeX.setToTeX`
- `CykTeX.tableToTeX`/`tableToTeXStyled` accept `ParsingTable<'nt>`

### 93.4: Tokens are terminals
- CYK: remove `terminals → Symbol` conversion, pass `Terminal<'t> list` directly
- Valiant: already uses raw `'t[]` internally
