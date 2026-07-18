# Detailed Plan: Task 187 — Improve GLL Visualization

## Task Description

Improve GLL visualization by adding step collection, step rendering, and improving summary content.

---

## Analysis

### Current State
- `GLL.buildPathIndex` returns only the final `PathIndex`, no intermediate snapshots
- `GllRunner` writes static outputs (grammar, input, RSM DOT, path index TeX, SPPF DOT) with no step directories
- No GSS DOT renderer exists
- SummaryTeX for GLL includes input, RSM/SPPF PDFs, and path index — but NOT original grammar or extended RSM
- RSM visualization uses local state numbering per block, not global

### Reuse Checklist
1. **LLParsingStep pattern** (LLParser.fs:17-21) — step type with data snapshots, reused for GLL
2. **VisualizationStep** (VisualizationTypes.fs) — rendered output type; will need GLL-specific variant
3. **writeStepsVisualization** (Helpers.fs:33-38) — step directory writer; will need GLL-specific variant
4. **PathIndexTeX.toTeX** (PathIndexTeX.fs) — existing path index TeX renderer, reuse for per-step snapshots
5. **RsmDot.toDot** (RsmDot.fs) — existing RSM DOT renderer, extend for global state numbering
6. **Graph visualization** (GraphAnalysis.Graph) — GSS is a Graph, can render via parametrized DOT

### New Types Required
- `GLLParsingStep<'t,'nt>` — step data: queue snapshot, new GSS vertices/edges, input position
- `GssDot` module — GSS to DOT with highlighting
- Descriptor TeX renderer — comma-separated descriptors in TeX format

---

## Subtasks

### S1: Define GLLParsingStep type and add step collection to buildPathIndex

**Code:**
- `src/FLPQ.Languages/GllTypes.fs` — add `GLLParsingStep<'t,'nt>` type
- `src/FLPQ.Languages/Gll.fs` — add `buildPathIndexWithSteps` function that collects steps

**Tests:** None (step collection is infrastructure, tested via visualization in later subtasks)
**Docs:** None

**Spec:**
- Add `GLLParsingStep<'t,'nt>` record type to GllTypes.fs:
  ```
  type GLLParsingStep<'t,'nt> =
      { Queue: Descriptor list
        NewGssVertices: Set<int>
        NewGssEdges: Set<int * int>
        InputPosition: int }
  ```
  - `Queue` — snapshot of remaining descriptors in the queue
  - `NewGssVertices` — GSS vertices that became active since last step (vertices with outgoing edges or storedPops)
  - `NewGssEdges` — GSS edges added since last step
  - `InputPosition` — current input position being processed (descriptor's Vertex field)
- Add `buildPathIndexWithSteps` function to Gll module:
  - Same signature as `buildPathIndex` but returns `PathIndex<'t,'nt> * GLLParsingStep<'t,'nt> list`
  - Collect initial state step before main loop (queue has one descriptor, no GSS activity yet)
  - Collect step at the end of each main loop iteration (after single descriptor fully processed)
  - Track newly added GSS vertices/edges by comparing against previous snapshot
  - Share algorithm logic with `buildPathIndex` — refactor common body into private helper

### S2: Create GssDot module for GSS DOT rendering

**Code:** New file `src/FLPQ.Printers/GssDot.fs`

**Tests:** Golden test in `tests/FLPQ.Printers.Tests/GssDotTests.fs` — render a known GSS, compare with reference
**Docs:** None

**Spec:**
- Module `GssDot` with function:
  ```
  toDot : (int -> string)          // vertex label printer (state, vertex) -> string
         -> (int * int -> string)  // edge label printer (sourceIdx, targetIdx) -> string
         -> Set<int>               // highlighted vertices
         -> Set<int * int>         // highlighted edges
         -> GSS
         -> string
  ```
- Render GSS as DOT digraph with `rankdir=LR`
- Each vertex: node ID is linear index, label from vertex label printer
- Each edge: from source to target, label from edge label printer
- Highlighted vertices: `fillcolor=yellow!30, style=filled`
- Highlighted edges: `color=red, penwidth=2.0`
- Reuse `DerivationTreeDot.escapeLabel` for label escaping
- Only render vertices that have been activated (have outgoing edges or are targets of edges)

### S3: Create descriptor queue TeX rendering and GllStepVisualizer

**Code:** New file `src/FLPQ.Printers/GllStepVisualizer.fs`

**Tests:** Unit test for descriptor TeX rendering
**Docs:** None

**Spec:**
- Module `GllStepVisualizer` with functions:
  - `descriptorToTeX : Descriptor -> string` — render single descriptor as TeX:
    `R_{state,vertex}^{gssIdx}` (with range info if non-empty)
  - `queueToTeX : Descriptor list -> string` — comma-separated descriptors in TeX
  - `renderStep : Symbol<'t,'nt> -> string -> GLLParsingStep<'t,'nt> -> PathIndex<'t,'nt> -> int -> GllVisualizationStep` — render a single step
    - Returns `GllVisualizationStep = { Queue: string; GssDot: string; PathIndex: string; Input: string }`
    - Queue: TeX from queueToTeX
    - GssDot: DOT from GssDot.toDot with highlighted new vertices/edges
    - PathIndex: TeX from PathIndexTeX.toTeX
    - Input: TeX from TeXRenderer.inputRow with position = step.InputPosition
  - `renderSteps : Symbol<'t,'nt> -> string -> GLLParsingStep<'t,'nt> list -> PathIndex<'t,'nt> -> Terminal<'t> list -> int -> GllVisualizationStep list` — render all steps

### S4: Update GllRunner to use step-aware parsing and write step visualizations

**Code:**
- `src/FLPQ.Cli/GllRunner.fs` — use `buildPathIndexWithSteps`, write step files
- `src/FLPQ.Cli/Helpers.fs` — add `writeGllStepsVisualization` function

**Tests:** None (runner changes tested via CLI manually)
**Docs:** None

**Spec:**
- Add `writeGllStepsVisualization` to Helpers:
  ```
  writeGllStepsVisualization : string -> GllVisualizationStep list -> unit
  ```
  - For each step, create `step_N/` subdirectory
  - Write `queue.tex`, `gss.dot`, `path_index.tex`, `input.tex` per step
- Update `runGll`:
  - Call `GLL.buildPathIndexWithSteps` instead of `buildPathIndex`
  - Render steps using `GllStepVisualizer.renderSteps`
  - Write step visualizations using `writeGllStepsVisualization`
  - Keep existing static outputs (grammar, input, RSM, path index, SPPF)

### S5: Add extended RSM DOT visualization with global state numbering

**Code:** `src/FLPQ.Printers/RsmDot.fs` — add `extendedRsmToDot` function

**Tests:** Golden test for extended RSM DOT output
**Docs:** None

**Spec:**
- Add `extendedRsmToDot` function to RsmDot module:
  ```
  extendedRsmToDot : 't -> string -> 'nt -> string -> ExtendedRSM<'t,'nt> -> string
  ```
- Render the extended RSM as a single DOT digraph (no subgraph clusters)
- Use global state numbering: each node ID is the global state index
- Node label: `N_s{globalIdx}` where N is block nonterminal, globalIdx is global state
- Start states: `fillcolor=green!30`
- Final states: `peripheries=2`
- Fresh start block (S') nodes: `fontcolor=blue`
- Transitions: edges with labels from transition matrix
- Epsilon transitions: `style=dotted`

### S6: Update SummaryTeX for GLL steps and summary improvements

**Code:** `src/FLPQ.Printers/SummaryTeX.fs` — update GLL header and step sections

**Tests:** Golden test for GLL summary TeX output
**Docs:** None

**Spec:**
- Update `headerSection` for `SummaryKind.GLL`:
  - Add original grammar section (already handled by existing `grammar_original.tex` logic)
  - Add extended RSM section if `ext_rsm.dot` PDF is available
- Add GLL-specific step section function `gllStepSection`:
  ```
  gllStepSection : string -> int -> string list
  ```
  - Include queue TeX, GSS PDF, path index TeX (resized), input TeX
- Update `buildContent` to use `gllStepSection` for GLL steps instead of `stackStepSection`

### S7: Run tests, build, and verify

**Code:** Fix any compilation or test issues
**Tests:** Run full test suite
**Docs:** None

**Spec:**
- Build solution: `dotnet build FLPQ.slnx`
- Run tests: `dotnet test FLPQ.slnx`
- Verify GLL runner produces step directories with expected files
- All tests pass (0 failures, 0 skipped)
