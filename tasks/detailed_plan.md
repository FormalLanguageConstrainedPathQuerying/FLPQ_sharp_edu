# Detailed Plan — Task 279: Improve RNGLR summary

## Task description (verbatim)

279. Improve RNGLR summary. First, remove dot visualization of RSM from tikz-based summary. Check that dot-base summary contains dot-visualized extended RSM, beceuse currently I see not-extended DOT rsm in tikz-based summary, while included tikz visualization is correct: it is for extended RSM. Second: add LR automata visualization at the start of summary. Use ajustbox for LR automata (look at SPPF for example). Order: extended RSM, LR automata, LR table.

## Current state (verified by exploration)

- `RnglrRunner.fs` writes `rsm_blocks.dot` (the **original**, not extended RSM) unconditionally
  (both modes), and `ext_rsm.tikz.tex` only in TikZ mode. It writes no LR-automaton figure at all.
- `Summary.fs` RNGLR branch: `RsmPdfs = [("RSM", "dot_pdfs/rsm_blocks.pdf")]` when
  `rsm_blocks.dot` exists; no LR automaton. So the TikZ-mode summary shows the non-extended DOT
  RSM next to the correct extended TikZ RSM (the user's complaint), and the DOT-mode summary shows
  the non-extended RSM.
- `SummaryTeX.headerSection` RNGLR branch order: Color Legend → RNGLR Parsing Table →
  `extRsmTikzSection` (TikZ mode only) → `rsmFigureLines` (DOT PDFs, both modes) → Path Index.
- GLL is already correct and stays untouched: in TikZ mode it writes no `ext_rsm.dot`, so its
  `rsmPdfs` is empty there; in DOT mode it uses `ext_rsm.pdf`.
- The RNGLR LR automaton is `lrTable.Automaton : DFA<Symbol<'t,'nt>, Set<RnglrItem<'nt>>>`;
  no renderer for `RnglrItem` exists anywhere (grep-verified).
- SPPF adjustbox pattern to mirror: `SummaryTeX.sppfSection` wraps TikZ with
  `wrapTikzAdjustbox true` (width + height limit) and falls back to `includePdf` in DOT mode.

## Reuse analysis (reusing skill checklist)

Reused, not recreated:

- `AutomatonDot.dfaToDot` — generic over state content `'s`; used directly for the DOT-mode
  LR automaton (same as `LRRunner`).
- `AutomatonTikz.dfaToTikz` — layered-layout TikZ DFA renderer; the new RNGLR wrapper delegates
  to it (same pattern as `LRAutomatonTikz.lr0AutomatonToTikz`).
- `LRAutomatonTikz.stateContentToTikzAs` — public math-mode aligned-content wrapper.
- `RsmDot.extendedRsmToDot` / `RsmTikz.extendedRsmToTikz` — extended-RSM figures (GLL precedent).
- `SymbolTeX.toLaTeX` — edge-label printer for `Symbol<'t,'nt>`.
- `SummaryTeX.wrapTikzAdjustbox` / `includePdf` / `maybe` — section wrappers.
- `Summary.fs` LR-branch file-existence pattern (`lr_automaton.tikz.tex` inline vs
  `lr_automaton.dot` → PDF) — mirrored for RNGLR.

Created new (nothing existing covers it):

- `RnglrAutomatonTikz` module — the only new code: `RnglrItem` rendering + a typed wrapper over
  `AutomatonTikz.dfaToTikz`.

## Design decisions

- **Item notation**: one `RnglrItem` renders as `<nonterminal> : <rsmState>` (e.g. `S' : 1`).
  A state's content is a `State N` header line plus one line per item, mirroring
  `LRAutomatonTikz.renderLR0StateContent`.
- **DOT-mode automaton labels** carry the same item lines as TikZ (multi-line dot label
  `State N\n<item>\n...`) — strictly more informative than LR's bare `State N`, same data.
- **RNGLR header order**: Color Legend → Extended RSM → LR Automaton → RNGLR Parsing Table →
  Path Index (the user's "extended RSM, LR automata, LR table" order; the legend stays first).
- **Extended RSM head is mode-aware for RNGLR**: TikZ mode → `ext_rsm.tikz.tex` wrapped in
  `wrapTikzAdjustbox false` (existing); DOT mode → dot-compiled `ext_rsm.pdf`. The non-extended
  `rsm_blocks.dot` is no longer written by the RNGLR runner at all.
- **LR automaton wrap**: TikZ mode → `wrapTikzAdjustbox true` (SPPF-style, width + height limit);
  DOT mode → `includePdf "dot_pdfs/lr_automaton.pdf"`.
- **Docs placement**: `RnglrAutomatonTikz` is documented inside the existing grouped
  `docs/developer/automaton-viz.md` (precedent: `LRAutomatonTikz` lives there, not in its own
  file), plus hub/architecture entries.

---

### S1: RnglrAutomatonTikz module [done — dfce604]

**Code:** New file `src/FLPQ.Printers/RnglrAutomatonTikz.fs` (add to `FLPQ.Printers.fsproj`
Compile list after `LRAutomatonTikz.fs`):

- `renderRnglrItem (nonterminalPrinter: 'nt -> string) (item: RnglrItem<'nt>) : string` —
  `<nonterminal> : <rsmState>`; unwraps the `Nonterminal` newtype.
- `renderRnglrStateContent (nonterminalPrinter: 'nt -> string) (stateIdx: int) (items: Set<RnglrItem<'nt>>) : string` — `\text{State N}\\` header + one `<item> \\` line per
  item in `Set.toSeq` order, trailing newline trimmed (mirrors `renderLR0StateContent`).
- `rnglrAutomatonToTikz (labelPrinter: Symbol<'t,'nt> -> string) (nonterminalPrinter: 'nt -> string) (dfa: DFA<Symbol<'t,'nt>, Set<RnglrItem<'nt>>>) : string` —
  delegates to `AutomatonTikz.dfaToTikz` with `shape = "rectangle"` and state visualizer
  `LRAutomatonTikz.stateContentToTikzAs (renderRnglrStateContent ...)`.

**Tests:** New `tests/FLPQ.Printers.Tests/RnglrAutomatonTikzTests.fs` (build the real automaton
via `ExtendedRSM.create` + `RnglrLR.buildLR0Table` on a registry grammar, e.g. ANBN
"S -> a S b | eps"):

- `[<Fact>]` `renderRnglrItem` renders `<nt> : <state>` exactly.
- `[<Fact>]` `renderRnglrStateContent` — header line + one line per item, each ending `\\`.
- `[<Fact>]` `rnglrAutomatonToTikz` — contains `\begin{tikzpicture}`, `rectangle` shape, start
  state styling (`fill=green!30`), final-state `double`, and terminal/nonterminal edge labels.
- `[<Fact>] [<Trait("Category", "TeX")>]` rendered automaton compiles with lualatex via
  `ExternalTools.compileTexStringWithTemplate` + `tex_tikz_template.tex` (pattern from
  `AutomatonVisualizationTests`).

**Docs:** Extend `docs/developer/automaton-viz.md` (module list, new "RnglrAutomatonTikz Module"
section with signatures + item notation, See Also); update `docs/developer/FLPQ.Printers.md`
module table row; add cross-reference in `docs/developer/rnglr.md` See Also.

**Spec:**

- Module doc comment: RNGLR LR automaton = DFA over RSM symbols whose states hold sets of
  `RnglrItem`; book reference sec:CFPQ_RNGLR.
- All public functions carry XML doc comments (FS3569 is WarningsAsErrors in FLPQ.Printers).
- No new dependencies; file opens only what it uses.

### S2: RnglrRunner output files [done]

**Code:** `src/FLPQ.Cli/RnglrRunner.fs`:

- Delete the unconditional `rsm_blocks.dot` write (line 50).
- `useDot` branch: write `ext_rsm.dot` via `RsmDot.extendedRsmToDot string string extRsm None`;
  write `lr_automaton.dot` via `AutomatonDot.dfaToDot (SymbolTeX.toLaTeX string string) (fun idx items -> "State N" + item lines joined by "\n") lrTable.Automaton` using
  `RnglrAutomatonTikz.renderRnglrItem` for the item lines.
- TikZ branch: keep `ext_rsm.tikz.tex`; add `lr_automaton.tikz.tex` via
  `RnglrAutomatonTikz.rnglrAutomatonToTikz (SymbolTeX.toLaTeX string string) string lrTable.Automaton`.

**Tests:** `tests/FLPQ.Cli.Tests/RnglrRunnerTests.fs`:

- Replace `runRnglr produces rsm_blocks.dot` with `runRnglr dot mode produces ext_rsm.dot`.
- New `[<Fact>]` `runRnglr dot mode produces lr_automaton.dot` (exists, non-empty).
- New `[<Fact>]` `runRnglr tikz mode produces lr_automaton.tikz.tex` (exists, non-empty).
- New `[<Fact>]` `runRnglr no longer produces rsm_blocks.dot` (regression: file absent in both
  modes — one fact covering the dot-mode helper is enough; assert on the dot run).

**Docs:** `docs/user/cli.md` — RNGLR row of the root-level artifacts table: replace
`rsm_blocks.dot` with `ext_rsm.tikz.tex` (default Tikz mode) or `ext_rsm.dot` (`--use-dot`), and
add `lr_automaton.tikz.tex` (default) or `lr_automaton.dot` (`--use-dot`).

**Spec:**

- File names match the GLL/LR conventions exactly (`ext_rsm.*`, `lr_automaton.*`) so
  `Summary.compileDotArtifacts` picks them up without changes.
- DOT state label: first line `State <idx>`, then one `<nt> : <rsmState>` line per item in
  `Set.toSeq` order, joined with `\n`.

### S3: Summary.fs RNGLR visuals [done]

**Code:** `src/FLPQ.Cli/Summary.fs` — RNGLR branch of the `visuals` match:

- `RsmPdfs = [("Extended RSM", "dot_pdfs/ext_rsm.pdf")]` when `ext_rsm.dot` exists (was
  `rsm_blocks.dot` / "RSM").
- LR automaton detection mirroring the LR branch: if `lr_automaton.tikz.tex` exists →
  `LrAutomatonTikz = Some <content>`, `LrAutomatonPdf = None`; else
  `LrAutomatonPdf = Some "dot_pdfs/lr_automaton.pdf"` when `lr_automaton.dot` exists.

**Tests:** `tests/FLPQ.Cli.Tests/SummaryTests.fs`:

- `[<Fact>]` `buildSummary for RNGLR includes the extended RSM PDF when ext_rsm.dot exists`
  (DOT mode; merged TeX contains "Extended RSM").
- The LR-automaton detection facts (DOT-mode PDF, TikZ-mode embed) were moved to S4: they
  assert on rendered header output, which only exists after the S4 `SummaryTeX` change.

**Docs:** none new — behavior is documented with the header layout in S4.

**Spec:**

- The `HeaderVisuals` record shape is unchanged; only the RNGLR branch fills it differently.
- Absent files degrade gracefully (no section), same as the LR branch.

### S4: SummaryTeX RNGLR header layout [done]

**Code:** `src/FLPQ.Printers/SummaryTeX.fs` — RNGLR branch of `headerSection`:

- New local `extRsmLines`: TikZ mode → existing shared `extRsmTikzSection`; DOT mode →
  `rsmFigureLines` (the `rsmPdfs` includePdf list). This removes the DOT RSM from the TikZ-mode
  RNGLR summary.
- New local `lrAutomatonLines`: TikZ mode → `[ section "LR Automaton"; wrapTikzAdjustbox true tikz; "" ]`
  when `lrAutomatonTikz` is Some (SPPF-style width+height adjustbox); DOT mode →
  `[ section "LR Automaton"; includePdf rel; "" ]` when `lrAutomatonPdf` is Some.
- Branch body becomes: `colorLegend @ extRsmLines @ lrAutomatonLines @ tableLines @ pathIndexLines`.
- GLL branch and all other kinds are untouched.

**Tests:**

- `tests/FLPQ.Printers.Tests/SummaryTexSectionTests.fs`:
  - `[<Fact>]` RNGLR header in TikZ mode orders Extended RSM → LR Automaton → RNGLR Parsing
    Table (write `ext_rsm.tikz.tex` + `rnglr_table.tex`, pass `lrAutomatonTikz = Some`; assert
    `IndexOf` ordering and the `max totalheight=\textheight` adjustbox on the automaton).
  - `[<Fact>]` RNGLR header in TikZ mode omits DOT RSM figures even when `rsmPdfs` is non-empty
    (no `\includegraphics`).
  - `[<Fact>]` RNGLR header in DOT mode includes the extended RSM PDF and the LR automaton PDF
    (both includegraphics present, no adjustbox).
- `tests/FLPQ.Printers.Tests/TexCompilationTests.fs` — update the two RNGLR end-to-end tests:
  - DOT-mode test: `rsmPdfs = [("Extended RSM", "dot_pdfs/ext_rsm.pdf")]` (stub copy renamed),
    pass `Some "dot_pdfs/lr_automaton.pdf"` + stub copy so the new section compiles.
  - TikZ-mode test: `rsmPdfs = []`; write `lr_automaton.tikz.tex` rendered by the new
    `RnglrAutomatonTikz.rnglrAutomatonToTikz` and pass it as `lrAutomatonTikz`; update the stale
    comment ("the RSM figure stays a header PDF entry"); assert the automaton adjustbox is
    present. Both must still compile with lualatex.
- `tests/FLPQ.Cli.Tests/CliSummaryTests.fs`:
  - `RNGLR summary wraps each step GSS figure in adjustbox`: the width-only needle count stays
    `1 + sections.Length` — the LR automaton head uses the max-totalheight variant
    (`wrapTikzAdjustbox true`, per the code spec above), which the width-only needle
    (`\begin{adjustbox}{max width=\textwidth}` with closing brace) does not match; update the
    comment to say so.
  - `assertSppfUsesMaxTotalHeightAdjustbox` → renamed `assertMaxTotalHeightAdjustboxCount`,
    parameterized by expected occurrence count — CYK/Valiant/GLL keep 1, RNGLR becomes 2
    (SPPF + LR automaton); fix the stale "only figure" comment.
  - New `[<Fact>]` `RNGLR summary orders extended RSM, LR automaton, and parsing table`
    (IndexOf ordering in the merged TeX).
  - New `[<Fact>]` `RNGLR summary tikz mode contains no DOT RSM figure` (no
    `dot_pdfs/rsm_blocks.pdf`, no `dot_pdfs/ext_rsm.pdf`).
  - New `[<Fact>]` `RNGLR summary dot mode includes extended RSM and LR automaton PDFs` —
    add a `useDot`-capable EBNF runner helper (existing `runWithSummaryEBNF` delegates with
    `useDot = false`; new wrapper passes the flag).
  - Moved from S3 (`tests/FLPQ.Cli.Tests/SummaryTests.fs`):
    `[<Fact>]` `buildSummary for RNGLR includes the LR automaton PDF when lr_automaton.dot exists`
    (DOT mode; merged TeX contains "LR Automaton" and `lr_automaton.pdf`) and
    `[<Fact>]` `buildSummary for RNGLR in tikz mode embeds the LR automaton TikZ` (write a
    marker `lr_automaton.tikz.tex`; merged TeX contains the marker and an adjustbox, no
    `lr_automaton.pdf` includegraphics).

**Docs:** `docs/developer/summary-tex.md` — Overview bullet 27 (RNGLR DOT-mode head is now
`ext_rsm.pdf`; TikZ mode never includes a DOT RSM) + new Overview bullet for the RNGLR LR
automaton section; Design Decisions rows (mode-aware extended-RSM head for RNGLR; SPPF-style
adjustbox for the RNGLR LR automaton). `docs/developer/FLPQ.Cli.md` — update the abstract's
summary-head sentence to cover the RNGLR LR automaton and the DOT-mode ext RSM.

**Spec:**

- Section titles exactly: "Extended RSM", "LR Automaton", "RNGLR Parsing Table" (existing).
- Missing files degrade gracefully: each of the three sections is independently optional.
- No signature changes to `headerSection`/`buildContent` — the `lrAutomatonPdf` /
  `lrAutomatonTikz` / `rsmPdfs` parameters already exist.
