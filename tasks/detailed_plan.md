# Task 91: Refactoring — Symbol Printing, Rhs, inputRow, grammarToTeX

## 91.1: Create single symbol-to-TeX printing function

Create new module `SymbolTeX` in `FLPQ.Printers` with a public function:
- `toLaTeX: Symbol<'t, 'nt> -> string`
  - Implements: `T(Terminal t) -> string t`, `N nt -> string nt`, `Epsilon -> @"\varepsilon"`
  - This is the single source of truth for rendering any symbol to TeX

Files to update:
- **NEW**: `src/FLPQ.Printers/SymbolTeX.fs` — add before `MatrixTeX.fs` in fsproj
- `src/FLPQ.Printers/GrammarTeX.fs` — remove private `symToTeX`, use `SymbolTeX.toLaTeX`
- `src/FLPQ.Printers/CykTeX.fs` — remove private `shortSymbolPrinter`, use `SymbolTeX.toLaTeX`
- `src/FLPQ.Cli/Program.fs` — remove private `symbolPrinter` and inline lambdas, use `SymbolTeX.toLaTeX`
- `tests/FLPQ.Printers.Tests/TexCompilationTests.fs` — remove `symbolToStr`, use `SymbolTeX.toLaTeX`
- `tests/FLPQ.Printers.Tests/LLVisualizerTests.fs` — remove `symbolPrinter`, use `SymbolTeX.toLaTeX`
- `tests/FLPQ.Printers.Tests/LRVisualizerTests.fs` — remove `symbolPrinter`, use `SymbolTeX.toLaTeX`

## 91.2: Remove `| [] -> @"\varepsilon"` pattern in grammarToTeX

Since `Rhs` already has `EpsilonRhs` case and `Rhs.toSymbols` returns `[]` for epsilon:
- Change `grammarToTeX` to match on `rule.rhs` directly:
  - `EpsilonRhs` → `@"\varepsilon"`
  - `Symbols nel` → map with `SymbolTeX.toLaTeX` and join

## 91.3: inputRow accepts list of terminals

- Change `StepInput<'t, 'nt>` → `StepInput<'t>` with `tokens: Terminal<'t> list`
- Update `LLParsingStep<'t, 'nt>` and `LRParsingStep<'t, 'nt>` to use `StepInput<'t>`
- Change `inputRow` signature: `(Terminal<'t> -> string) -> Terminal<'t> list -> int -> string`
- Simplify `inputRow` internals (no need for Symbol match since tokens are always terminals)
- Update `LLStepVisualizer.renderStep` and `LRStepVisualizer.renderStep`:
  - Derive terminal printer from symbol visualizer for inputRow
- Update `LLParser.parseWithSteps`: store terminals directly (no conversion to Symbol)
- Update `LRParser.parseWithSteps`: store terminals directly (no conversion to Symbol)
- Update `Program.fs` callers of `inputRow` (CYK, Valiant)

## 91.4: Add production numbers option to grammarToTeX

- Add `?showNumbers: bool` parameter (default false)
- When true, prepend `[0]`, `[1]`, etc. before each rule

## 91.5: Print start nonterminal productions first

- Sort rules: start nonterminal rules first, rest in original order
