# Task 255: Fix productions numbering for CYK and Valiant

## Scope

`SppfParsingEntry<'nt>` stores a `ProdIdx` field that is interpreted in two
incompatible ways:

- **Rendering** (`GrammarTeX.grammarToTeXWithNumbers`) orders the grammar's rules
  start-nonterminal-first and numbers them **1-based** (`1) S -> ...`, `2) ...`).
- **Table cells / SPPF** (`Cyk`, `Valiant`, `BasicSppf`) store the **0-based**
  index into the grammar's rule list in its original (unordered) order.

For grammar `S -> a | S S | S S S`, CNF is `S -> a; S -> S S; S -> N1; N1 -> S S`.
The last production renders as number `4`, but the table cell shows `(N1, 0, 0)`
(production number `0`). The same mismatch appears in the SPPF.

Task 255 makes the production number a single canonical value: a **1-based**
number in the **start-nonterminal-first** ordering used by the CNF renderer, and
introduces a number→production map created once and reused everywhere.

## Reuse Analysis

- **Existing `GrammarTeX.grammarToTeXWithNumbers` ordering logic** (start rules
  first, then rest) — extracted into a shared `Grammar.numberedRules` function so
  renderer and algorithms share one source of truth (Q5: extract shared helper).
- **Existing `Cyk.findTerminalRulesWithProdIdx` / `findBinaryProductionsWithProdIdx`** —
  change input from `Rule list` (0-based `List.indexed`) to `(int * Rule) list`
  (1-based numbers). Structure otherwise unchanged.
- **Existing `Valiant.terminalRulesFromGrammar` / `binaryRulesFromGrammar`** —
  same change: iterate `Grammar.numberedRules` instead of `List.indexed cnf.Rules`.
- **Existing `BasicSppf.fromParsingTable` / `validateProductionChildren`** —
  replace `cnf.Rules.[idx]` array indexing with a `Map<int, Rule>` lookup built
  once from `Grammar.productionNumberMap`.
- **Existing golden-test infrastructure** (`GoldenHelpers.verifyGolden`,
  `CREATE_GOLDEN_FILES=1`) — reused to regenerate the 4 affected reference files.
- **`ParsingTableTeX.sppfEntryCellToTeX`**, **`BasicSppfDot`**, **`BasicSppfTikz`** —
  unchanged: they only display the `ProdIdx` value, which is now 1-based.

---

### S1: Add canonical production numbering to `Grammar` module

**Code:** `src/FLPQ.Languages/Grammar.fs` — add `Grammar.numberedRules` and
          `Grammar.productionNumberMap` (reused by GrammarTeX, Cyk, Valiant, BasicSppf)
**Tests:** `tests/FLPQ.Languages.Tests/GrammarTests.fs` — new `[<Fact>]` tests for
          start-first ordering and 1-based numbering (grammar with start rules
          interleaved among other rules, and a grammar whose start appears last)
**Docs:** `docs/developer/grammar.md` — document the two new functions

**Spec:**
- Add `numberedRules (g: Grammar<'t,'nt>) : (int * Rule<'t,'nt>) list`:
  partition `g.Rules` into start rules and the rest, concatenate
  (`startRules @ otherRules`), mapi to `(i + 1, rule)` (1-based).
- Add `productionNumberMap (g: Grammar<'t,'nt>) : Map<int, Rule<'t,'nt>>` =
  `numberedRules g |> Map.ofList`.
- XML doc comments on both, describing the canonical ordering and 1-based numbering.
- Equivalence: no existing behavior changes; both functions are additive.

**Status:** [done]

---

### S2: Reuse `Grammar.numberedRules` in `GrammarTeX`

**Code:** `src/FLPQ.Printers/GrammarTeX.fs` — replace the local `orderedRules`
          computation with `Grammar.numberedRules`, use the number directly instead
          of `idx + 1`
**Tests:** none (output must be byte-identical — verified by existing
          `GrammarTeXGoldenTests` and `CykSummaryGoldenTests`)
**Docs:** `docs/developer/grammar-tex.md` — note the shared source of truth

**Spec:**
- `renderGrammar` (numbered branch) iterates `Grammar.numberedRules grammar`,
  using the `number` from each pair in place of `idx + 1`.
- Unnumbered branch keeps its own ordering (same logic; can also iterate the
  numbered list and discard the number).
- Equivalence: `grammarToTeXWithNumbers` output is byte-identical to before
  (all `GrammarTeXGoldenTests` pass unchanged).

**Status:** [done]

---

### S3: Switch CYK, Valiant, BasicSppf to 1-based canonical production numbers

**Code:**
- `src/FLPQ.Languages/Cyk.fs` — `findTerminalRulesWithProdIdx` /
  `findBinaryProductionsWithProdIdx` accept `(int * Rule<'t,'nt>) list`; call sites
  pass `Grammar.numberedRules cnf`; `ProdIdx` values become 1-based.
- `src/FLPQ.Languages/Valiant.fs` — `terminalRulesFromGrammar` /
  `binaryRulesFromGrammar` iterate `Grammar.numberedRules cnf`; `ProdIdx` values
  become 1-based.
- `src/FLPQ.Languages/BasicSppf.fs` — `fromParsingTable` builds
  `Grammar.productionNumberMap cnf` once and looks up rules by number;
  `validateProductionChildren` uses the map (and `Map.tryFind` for range checks).

**Tests:**
- `tests/FLPQ.Languages.Tests/CykTests.fs` — update the aplus test to look up the
  rule via `Grammar.productionNumberMap` instead of `cnf.Rules.[entry.ProdIdx]`.
- Regenerate golden reference files (the `ProdIdx` tuple component changes):
  `cyk_grammar1_aababb_summary.tex`, `cyk_grammar7_xplusx_summary.tex`,
  `valiant_grammar1_abab.tex`, `valiant_modified_grammar1_ab.tex`.

**Docs:**
- `docs/developer/cyk.md` — `productionIndex` is now 1-based, canonical order.
- `docs/developer/valiant.md` — same.
- `docs/developer/sppf-parsing-table.md` — update the tuple field description and
  the "0-based index" wording; add a design-decision row for the canonical
  number→production map.

**Spec:**
- `ProdIdx` stored in `SppfParsingEntry` is the 1-based production number in the
  start-nonterminal-first order (identical to `grammarToTeXWithNumbers`).
- CYK and Valiant (standard + modified) must produce identical `ProdIdx` values
  for the same rule (cross-algorithm SPPF equivalence preserved).
- `BasicSppf` reconstructs the rule via the number→production map, so
  `fromParsingTable` / `validateProductionChildren` / `extractDerivationTree`
  remain correct.
- Equivalence: acceptance results and extracted tree yields are unchanged;
  CYK ≡ Valiant ≡ modified-Valiant SPPF byte-identity still holds (property tests).
- Example (grammar `S -> a | S S | S S S`): cell for `N1 -> S S` now shows
  `(N1, 0, 4)` instead of `(N1, 0, 0)`, matching CNF rendering number `4`.

**Status:** [done]
