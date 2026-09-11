# Task 269: Fix duplicated SPPF section in GLL/RNGLR summary and broken math rendering in TikZ SPPF/RSM figures

## Task description (verbatim)

269. Fix duplicated SPPF section in GLL/RNGLR summary and broken math rendering in TikZ SPPF/RSM figures. The GLL and RNGLR merged summaries contain SPPF twice — once in the header (DOT-compiled PDF via `rsmSppfPdfs`) and once at the end (`sppfSection`, TikZ in tikz mode). The trailing TikZ version renders node labels as literal TeX source (`S^\varepsilon @1`, `[s0,v0]\to[s4,v6]`) because `SppfTikz.toTikz` passes whole labels through `AutomatonTikz.escapeLatex`, which escapes the math commands.

     1. Remove the SPPF entry from the GLL/RNGLR summary header: drop `("SPPF", "dot_pdfs/sppf.pdf")` from the pdfs lists in `Summary.fs`. The trailing `sppfSection` becomes the single SPPF location (TikZ in tikz mode, DOT PDF fallback otherwise), consistent with CYK/Valiant summaries.
     2. Fix `SppfTikz.toTikz`: escape only terminal/nonterminal names via `AutomatonTikz.escapeLatex`; structural TeX stays intact and math commands are placed in math mode — epsilon node label `$S^{\varepsilon}$ @1`, range/intermediate labels `[s0,v0]$ \to $[s4,v6]`.
     3. Fix the same bug class in RSM/automaton TikZ: `RsmTikz` epsilon edge label becomes `$\varepsilon$` (not routed through escapeLatex; names still escaped); `AutomatonTikz.epsEdges` `\varepsilon` becomes `$\varepsilon$`.
     4. Tests: regression assertions that SPPF TikZ output contains `$\varepsilon$`/`$\to$` and no `\textbackslash`; RSM TikZ epsilon edge label is `$\varepsilon$`; existing TeX compilation tests (SPPF tikz, RSM tikz, GLL/RNGLR merged summaries) keep passing; regenerate GLL + RNGLR summaries and verify exactly one SPPF section.

## Context and analysis

### Root causes (verified in code and generated output)

1. **Duplication.** `Summary.fs:109-128` puts `("SPPF", "dot_pdfs/sppf.pdf")` into
   `rsmSppfPdfs` for GLL and RNGLR; `SummaryTeX.headerSection` renders it as a
   `\subsection*{SPPF}` + `\includegraphics` (DOT-compiled PDF). Independently,
   `SummaryTeX.buildContent` appends `sppfSection vizDir useTikz` at the end of
   the document (TikZ in tikz mode, DOT PDF fallback). The header entry dates
   from task 152 (DOT era); the trailing section from tasks 238/243 (TikZ era).
   In tikz mode both render (two different renderings of the same forest); in
   DOT mode the same PDF is included twice. CYK/Valiant have no header SPPF —
   only the trailing section — so removing the GLL/RNGLR header entry makes all
   algorithms consistent: exactly one SPPF section, at the end.

2. **Broken math in TikZ SPPF.** `SppfTikz.toTikz` (SppfTikz.fs:42-52) builds
   labels containing math commands (`S^\varepsilon @1`, `[s0,v0]\to[s4,v6]`) and
   then passes the *whole* label through `AutomatonTikz.escapeLatex`, which
   rewrites `\` → `\textbackslash` and `^` → `\^`. Verified in
   `viz_output/check_rnglr/sppf.tikz.tex`: labels render as literal text
   `S^\varepsilon @1` / `[s0,v0]\to[s4,v6]`. The DOT renderer (SppfDot.fs) uses
   Unicode `ε`/`→` directly, which is why the header version "looks good".

3. **Same bug class in RSM/automaton TikZ.** `RsmTikz.fs:107,116` routes the
   `\varepsilon` edge label through `escapeLatex` (literal text).
   `AutomatonTikz.fs:95` emits raw `\varepsilon` in a text-mode edge quote —
   an undefined control sequence if ever compiled. `LRAutomatonTikz` is not
   affected (its content is wrapped in `$\begin{aligned}$`, line 68).

### Reuse decisions (reusing skill checklist)

- **S1**: no new code; `sppfSection` (SummaryTeX.fs:380) is the existing single
  source of truth for SPPF placement — it already handles TikZ/DOT fallback and
  is shared with CYK/Valiant. Only the two header entries are removed.
- **S2**: reuse `AutomatonTikz.escapeLatex` for name escaping only; label
  construction stays local to `SppfTikz`. No new helpers.
- **S3**: local one-line-class fixes in `RsmTikz`/`AutomatonTikz`; no new
  helpers.
- **S4**: extend existing tests (`TexCompilationTests.fs`, runner tests) rather
  than adding new files; reuse `ExternalTools.compileTexStringWithTemplate` and
  the registry grammars already used by those tests.

### Affected docs (documentation-conventions mapping)

- `docs/developer/summary-tex.md` — SPPF is a single trailing section for all
  algorithms; header no longer carries SPPF for GLL/RNGLR.
- `docs/developer/automaton-viz.md` — epsilon edge label is `$\varepsilon$`
  (math mode), not raw `\varepsilon`.
- `docs/developer/rsm-viz.md` — note the Tikz epsilon edge style (dotted,
  `$\varepsilon$`).

## Subtasks

### S1: Remove duplicate SPPF from GLL/RNGLR summary header [done — a795e96]

**Code:** `src/FLPQ.Cli/Summary.fs` — drop the
`if File.Exists(Path.Combine(vizDir, "sppf.dot")) then ("SPPF", "dot_pdfs/sppf.pdf")`
entries from the GLL (lines 113-114) and RNGLR (lines 123-124) pdfs lists.
Reuse: trailing `SummaryTeX.sppfSection` becomes the single SPPF location.

**Tests:** `tests/FLPQ.Cli.Tests/CliSummaryTests.fs` — add a test that the GLL
and RNGLR merged TeX contain exactly one `\subsection*{SPPF` occurrence (the
trailing "SPPF (Shared Packed Parse Forest)" section) and no
`\includegraphics` of `dot_pdfs/sppf.pdf` in the header area. Reuse the existing
`mergedTexPath` helper and runner invocation pattern.

**Docs:** `docs/developer/summary-tex.md` — Overview: SPPF appears exactly once,
as a trailing section for all algorithms (TikZ in tikz mode, DOT PDF fallback);
header no longer includes SPPF for GLL/RNGLR. Add a Design Decisions row.

**Spec:**

- After the change, `rsmSppfPdfs` for GLL carries only the Extended RSM entry
  (DOT mode) and for RNGLR only the RSM entry; in tikz mode both lists may be
  empty (the TikZ head sections come from `extRsmTikzSection`).
- `sppfSection` output is unchanged: title "SPPF (Shared Packed Parse Forest)",
  TikZ via `wrapTikzCenter` when `sppf.tikz.tex` exists and `useTikz`, else
  `includePdf "dot_pdfs/sppf.pdf"` when `sppf.dot` exists, else omitted.
- Merged summaries for GLL/RNGLR must still compile with lualatex (existing
  tests cover this).

### S2: Fix math rendering in SppfTikz labels [done — b6ce4d4]

**Code:** `src/FLPQ.Printers/SppfTikz.fs` — escape only the terminal/nonterminal
names (`AutomatonTikz.escapeLatex (terminalPrinter t)` /
`escapeLatex (nonterminalPrinter nt)`); build labels with math commands intact:

- terminal/nonterminal: `sprintf "%s [%d,%d]" escapedName l r`
- epsilon: `sprintf "$%s^{\\varepsilon}$ @%d" escapedName p`
- range: `sprintf "[s%d,v%d]$\\to$[s%d,v%d]" fs fp ts tp`
- intermediate: `sprintf "I(%d,%d) @[s%d,v%d]$\\to$[s%d,v%d]" s p fs fp ts tp`

**Tests:** `tests/FLPQ.Printers.Tests/TexCompilationTests.fs` — extend the
existing `SPPF tikz compiles with lualatex` test (line 810) with assertions:
output contains `$\varepsilon$` and `$\to$`, and does NOT contain
`\textbackslash`. The grammar/input used (ANBN classic, `a a b b`) produces
epsilon and range nodes.

**Docs:** none required beyond S1's summary-tex.md update (no dedicated
SppfTikz doc page exists; label format is covered by the compilation test).

**Spec:**

- Node labels must render as math: epsilon node shows S^ε with a proper
  superscript, range/intermediate nodes show an arrow between bracketed
  coordinates.
- Names from printers are still escaped (a terminal named `a_b` must not break
  the picture).
- The picture must compile under `data/tex_tikz_template.tex` (lualatex).

### S3: Fix epsilon labels in RsmTikz and AutomatonTikz [done — 8837732]

**Code:**

- `src/FLPQ.Printers/RsmTikz.fs` — for `AutomatonLabel.AEpsilon` emit the edge
  label `$\varepsilon$` without routing it through `escapeLatex`; terminal and
  `call Nt` labels keep escaping the name part only.
- `src/FLPQ.Printers/AutomatonTikz.fs` — `epsEdges`: `"\\varepsilon"` →
  `"$\\varepsilon$"`.

**Tests:** `tests/FLPQ.Printers.Tests/TexCompilationTests.fs` — add a test that
renders an RSM/extended-RSM TikZ figure containing an epsilon transition and
asserts the edge label is `$\varepsilon$` (not `\textbackslash varepsilon`) and
compiles with lualatex. Reuse the registry grammar with an epsilon production
(e.g. ANBN classic has `S -> eps`) and `RsmTikz.extendedRsmToTikz`.

**Docs:** `docs/developer/automaton-viz.md` — Visual Style: epsilon transitions
are `dotted` edges with `$\varepsilon$` label (math mode).
`docs/developer/rsm-viz.md` — Tikz section: note epsilon transitions render as
dotted edges with `$\varepsilon$` label.

**Spec:**

- Epsilon edge labels must be valid math-mode TeX in both modules.
- Non-epsilon labels (terminals, `call Nt`) keep the existing escaping behavior.
- Existing compilation tests (RSM tikz highlighted, GLL/RNGLR merged summaries)
  keep passing.

### S4: End-to-end verification and regression assertions [done — see results]

**Verification results (grammar `S -> a S b | eps`, input `a a b b`):**

- GLL + RNGLR merged TeX (tikz mode): 0 header `\subsection*{SPPF}` occurrences,
  exactly 1 trailing "SPPF (Shared Packed Parse Forest)" section each.
- SPPF TikZ labels in the merged documents: `$S^{\varepsilon}$ @2` and
  `[s0,v0]$\to$[s4,v6]` style; zero `\textbackslash` occurrences.
- Both merged documents compile with lualatex (exit 0).
- GLL DOT mode (`--use-dot`): 1 trailing SPPF section including
  `dot_pdfs/sppf.pdf` exactly once.
- Full suites: FLPQ.Cli.Tests 153/153, FLPQ.Printers.Tests 176/176, 0 skipped.

**Code:** none (verification only).

**Tests:**

- Run the full TeX compilation test suite (SPPF tikz, RSM tikz, GLL/RNGLR
  merged summaries in both DOT and TikZ modes) plus `CliSummaryTests`.
- Regenerate real summaries via the CLI for a GLL and an RNGLR example
  (accepted input with epsilon + range nodes), verify: exactly one SPPF
  subsection per merged TeX, SPPF TikZ labels contain `$\varepsilon$`/`$\to$`
  and no `\textbackslash`, and the merged documents compile with lualatex.

**Docs:** none.

**Spec:**

- Zero test failures; no skipped tests introduced.
- Both regenerated merged TeX files compile (lualatex) and contain a single
  SPPF section at the end of the document.
