# Detailed Plan: Task 196 — Improve GLL visualization

## Task Description

1. In descriptors table highlight full row instead of individual cells.
2. Fix GSS vertex labels: use nonterminal names from RsmStateInfo instead of raw state index numbers.
3. Show GSS node numbers explicitly in vertex labels (e.g., `v5: (<S'_start>,0)`).
4. Add per-step RSM with highlighted current state, same color as current GSS node.

---

### S1: Highlight full row instead of individual cells in descriptors table

**Code:** `src/FLPQ.Printers/GllStepVisualizer.fs` — `descriptorsTableToTeX` function;
         `data/tex_color_template.tex` — add `[table]` option to `\usepackage{xcolor}`
**Tests:** `tests/FLPQ.Printers.Tests/TexCompilationTests.fs` — update assertion
**Docs:** `docs/developer/guides/documentation-conventions.md` — check if any relevant docs

**Spec:**
- In `GllStepVisualizer.descriptorsTableToTeX` (line 65-74), replace per-cell `\colorbox{yellow!20}` wrapping with `\rowcolor{yellow!20}` command before the row's `\\`.
- The `xcolor` package with `table` option provides `\rowcolor`. Summary template already has `\usepackage[table]{xcolor}`.
- For `tex_color_template.tex`: change `\usepackage{xcolor}` to `\usepackage[table]{xcolor}` so `\rowcolor` works in compilation tests.
- Update the `renderRow` function: when `isCurrent`, emit `\rowcolor{yellow!20}` before the row content, followed by `q & i & g & mr \\`. When not current, just `q & i & g & mr \\`.
- The `\rowcolor` command colors from its position to the end of the current row, so it must appear at the start of the row (before any `&`).
- Update test assertion: `\colorbox{yellow!20}` → `\rowcolor{yellow!20}`.

---

### S2: Fix GSS vertex labels to show nonterminal names and GSS node numbers

**Code:** `src/FLPQ.Printers/GllStepVisualizer.fs` — `renderStep` and `renderSteps` functions
**Tests:** `tests/FLPQ.Printers.Tests/TexCompilationTests.fs` — add GSS DOT compilation test
**Docs:** `docs/developer/guides/documentation-conventions.md` — check if any relevant docs

**Spec:**
- Add `stateLabelPrinter: int -> string` parameter to `renderStep` and `renderSteps`.
- In `renderStep`, modify the vertex label printer (lines 132-135): change from `sprintf "(%d,%d)" state vertex` to `sprintf "v%d: (%s,%d)" idx (stateLabelPrinter state) vertex`.
- The `stateLabelPrinter` function maps global RSM state index → nonterminal name. For the initial state of the fresh start block S', the label should look like `<S'_start>`. For other states: `<nonterminalName>`.
- For edge labels (lines 136-141): use `stateLabelPrinter` there too for the state parts.
- In `GllRunner.runGll`: pre-compute the state label mapping from `flatExt.StateInfo`:
  ```
  let stateLabel state =
      let info = flatExt.StateInfo.[state]
      let (Nonterminal ntName) = info.BlockNonterminal
      sprintf "<%s>" ntName
  ```
  Pass this as the `stateLabelPrinter` parameter.
- Add a new compiled test: generate GSS DOT for a simple grammar and verify that vertex labels contain `v<number>: (<...>,<number>)` pattern and compile with Graphviz.

---

### S3: Add per-step RSM with highlighted current state

**Code:** `src/FLPQ.Printers/RsmDot.fs` — add `extendedRsmToDotWithHighlight` function;
         `src/FLPQ.Printers/GllStepVisualizer.fs` — add `RsmDot` field to `GllVisualizationStep`, update `renderStep`;
         `src/FLPQ.Cli/GllRunner.fs` — pass extended RSM to `renderSteps`;
         `src/FLPQ.Cli/Helpers.fs` — write `rsm_step.dot` per step;
         `src/FLPQ.Printers/SummaryTeX.fs` — include per-step RSM PDF in `gllStepSection`
**Tests:** `tests/FLPQ.Printers.Tests/RsmDotTests.fs` — add compilation test for highlighted RSM DOT;
         `tests/FLPQ.Printers.Tests/TexCompilationTests.fs` — update `GLL merged summary TeX compiles` to include rsm_step.dot files
**Docs:** `docs/developer/guides/documentation-conventions.md` — check if any relevant docs

**Spec:**
- Add new function `extendedRsmToDotWithHighlight` in `RsmDot.fs`: same signature as `extendedRsmToDot` plus `highlightedState: int option`. When `highlightedState = Some s`, render state `s` with `fillcolor=lightblue, style=filled` (same color as current GSS node).
- Add `RsmDot: string` field to `GllVisualizationStep` type.
- In `GllStepVisualizer.renderStep`: generate the per-step RSM DOT:
  ```
  let rsmDot =
      let currentState = step.CurrentDescriptor |> Option.map (fun d -> d.RsmState)
      RsmDot.extendedRsmToDotWithHighlight terminalPrinter nonterminalPrinter currentState ersm
  ```
- `renderStep` needs access to the extended RSM for state rendering. Pass either the `ExtendedRSM` or the `RsmStateInfo[]` array. For cleanliness, pass the `ExtendedRSM` since `extendedRsmToDotWithHighlight` needs it.
- Update `GllRunner.runGll` to pass the extended RSM to `renderSteps`.
- Update `Helpers.writeGllStepsVisualization` to write `rsm_step.dot` in each step directory.
- Update `SummaryTeX.gllStepSection` to include the per-step RSM PDF:
  ```
  let rsmPdfLine = [ includePdf (sprintf "dot_pdfs/%s_rsm.pdf" (Path.GetFileName stepDir)); "" ]
  ```
- Add `rsmPdfLine` to the step section output in `gllStepSection`.
- In the `GLL merged summary TeX compiles` test: add `rsm_step.dot` file writes and stub PDF copies for per-step RSM.
- Add a Graphviz compilation test for `extendedRsmToDotWithHighlight` in `RsmDotTests.fs`: build an extended RSM from `S -> a | eps`, render with a highlighted state, verify Graphviz compiles.
