# Task 263: Fix LL summary TeX compilation (raw `$` EOI terminal in math mode)

## Task description (verbatim)

When I compile summary tex file for LL (generated with `dotnet src/FLPQ.Cli/bin/Release/net10.0/FLPQ.Cli.dll -s -a LL -o viz_output/LL/ -i data/example_input_an_bn.txt -g data/example_grammar.bnf`) I get number of errors. Fix it. Check that other algorithms visualization not broken. Add tests to prevent such problem in the future.

## Context and analysis

### Root cause (verified empirically)

`TeXRenderer.inputRow` (`src/FLPQ.Printers/TeXRenderer.fs:13`) renders the input token
list as a one-row `pNiceMatrix` — a **math-mode** environment. LL and LR runners append
the end-of-input marker to the tokens before parsing:

- `LLRunner.fs:13` — `tokensWithEoi = tokens @ [ Grammar.eoiTerminal ]`
- `LRRunner.fs:19` — same

`Grammar.eoiTerminal = Terminal "$"` (`Grammar.fs:67`). The terminal printer used by the
step visualizers is the identity function (`SymbolTeX.toLaTeX string string`), so the EOI
token is emitted as a **raw `$`** inside math mode. In LaTeX a raw `$` in math mode is a
math-shift character and is invalid — lualatex reports `! Missing $ inserted.` at every
input row.

Reproduction results (grammar `S -> a S b S | eps`, input `a b a b a b`):

| Algorithm | Merged summary compile | Errors |
|-----------|------------------------|--------|
| LL        | FAIL                   | 16 × `Missing $ inserted` (one per step input row) |
| LR0 / SLR1 / CLR1 | FAIL              | 10 × same error each |
| CYK, Valiant, ValiantModified, RNGLR | PASS (0 errors) | — |
| GLL       | FAIL (unrelated, pre-existing) | `TeX capacity exceeded [number of strings=476553]` — 31 repeated 50×50 path-index matrices exhaust TeX's string pool; also >10 min with `\maxstrings=1000000`. **Out of scope** — separate pre-existing performance issue, reported to user |

### Why existing tests missed it

- ``LL step input TeX compiles with lualatex`` and ``LR step input TeX compiles with
  lualatex`` (`tests/FLPQ.Printers.Tests/TexCompilationTests.fs:51,65`) parse **without**
  the EOI terminal — unlike the real runners. The rendered input row never contains `$`.
- `CliSummaryTests.fs` runs the full pipeline for LL and all LR variants but only checks
  that the merged TeX file exists — it never compiles it.

### Why other rendering paths are safe (checked)

- LL/LR/RNGLR table renderers hardcode the EOI column header as `\$`
  (`LLTableTeX.fs:69`, `LRTableTeX.fs:104`, `RnglrTableTeX.fs:66,170`).
- TikZ edge labels are escaped for text mode via `AutomatonTikz.escapeLatex`.
- LR(1) item lookaheads render EOI as `\varepsilon` (no raw `$`).
- CYK/Valiant/RNGLR input rows contain no EOI token.

`inputRow` is the only unescaped math-mode path for terminals.

### Fix design

Escape TeX special characters at the math-mode boundary: add `escapeMath` to
`TeXRenderer` and apply it to each cell in `inputRow`. Math-mode-safe forms (verified
with lualatex): `\` → `\textbackslash `, `&` → `\&`, `%` → `\%`, `$` → `\$`,
`#` → `\#`, `_` → `\_`, `{` → `\{`, `}` → `\}`, `^` → `\text{\^{}}`. `~` is valid as-is
in math mode.

Not reused: `AutomatonTikz.escapeLatex` is a **text-mode** escaper (`\^`, `\~{}` forms
are invalid in math mode) and lives in the automaton-rendering module; generalizing it
would touch 8+ call sites for no benefit. The codebase pattern is per-boundary escapers
(`AutomatonTikz.escapeLatex` for text, `DerivationTreeDot.escapeLabel` for DOT), so a
math-mode escaper in `TeXRenderer` follows the same pattern.

Fixing `inputRow` fixes LL and all three LR variants at once and protects CYK/Valiant/
RNGLR input rows if an input ever contains a TeX-special terminal.

## Subtasks

### S1: Escape TeX special characters in `inputRow` (math mode) — [done, bfa639d]

**Code:** `src/FLPQ.Printers/TeXRenderer.fs` — add public `escapeMath : string -> string`
(math-mode escaper, spec below); apply it to each cell content in `inputRow` after the
terminal printer is applied. No other source changes.
**Tests:** New `[<Fact>]` in `tests/FLPQ.Printers.Tests/TexCompilationTests.fs`:
`inputRow` with an EOI token (`Grammar.eoiTerminal`) compiles with lualatex (this is the
direct regression test for the bug); plus a `[<Fact>]` asserting `escapeMath` maps each
special character to its escaped form and leaves plain identifiers untouched.
**Docs:** Update `docs/developer/visualization-types.md` — `TeXRenderer` section: add
`escapeMath` entry and note that `inputRow` escapes TeX special characters (math mode).

**Spec:**
- `escapeMath` replaces, in this order: `\` → `\textbackslash `, `&` → `\&`, `%` → `\%`,
  `$` → `\$`, `#` → `\#`, `_` → `\_`, `{` → `\{`, `}` → `\}`, `^` → `\text{\^{}}`.
  `~` and all other characters pass through unchanged.
- In `inputRow`, each cell = `escapeMath (symbolPrinter token)`; the current-position
  cell is then wrapped in `\underbar{...}` as before. Empty-token case (`\varepsilon`)
  unchanged.
- All existing golden/compilation tests must pass without regeneration (escaping is a
  no-op for the plain `a`/`b` terminals used by all current fixtures).

### S2: Include EOI terminal in LL/LR step-input compilation tests — [done, 7167231]

**Code:** none.
**Tests:** `tests/FLPQ.Printers.Tests/TexCompilationTests.fs` — update
``LL step input TeX compiles with lualatex`` and ``LR step input TeX compiles with
lualatex`` to append `Grammar.eoiTerminal` to the token list before `parseWithSteps`,
matching `LLRunner.fs:13` / `LRRunner.fs:19`. This closes the gap that let the bug
through (tests parsed EOI-free inputs while runners always append EOI).
**Docs:** none.

**Spec:**
- LL test: `let tokens = Tokenizer.tokenizeTerminals "a b" @ [ Grammar.eoiTerminal ]`.
- LR test: same for its `"a a"` input.
- Both tests must fail on the pre-fix code (verified by running against the unfixed
  `inputRow`) and pass after S1.

### S3: Add merged-summary compilation tests for LL and LR — [done, 3fb17c0]

**Code:** none.
**Tests:** `tests/FLPQ.Cli.Tests/CliSummaryTests.fs` — add two `[<Fact>]` tests that run
the full CLI pipeline (existing `runWithSummary` helper, which already uses the user's
exact scenario: Dyck1 grammar + input `a a b a b b`) and then compile the produced
merged TeX with `ExternalTools.compileTexFile`:
- ``LL summary merged TeX compiles with lualatex``
- ``SLR(1) summary merged TeX compiles with lualatex``

**Docs:** none.

**Spec:**
- Reuse `runWithSummary algo false` (TikZ mode — the default and the mode the user hit).
- Locate `results/<algo>/<algo>_merged.tex` via the existing `assertMergedTexExists`
  path logic, then `Assert.True(ExternalTools.compileTexFile texPath (dir of texPath))`.
- Clean up the temp output directory in a `finally` block.
- Both tests must fail on pre-fix code and pass after S1.

## Verification (after all subtasks)

1. Re-run the user's exact command; compile `viz_output/LL/results/ll/ll_merged.tex` — 0 errors.
2. Generate + compile summaries for LR0, SLR1, CLR1 — 0 errors each.
3. Generate + compile CYK, Valiant, ValiantModified, RNGLR — still 0 errors (no regression).
4. Full test suite via hard gate — `STATUS: PASS`.
