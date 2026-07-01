# Global Plan: Tasks 77--80

## Task Summary

| ID | Description | Type | Status |
|----|-------------|------|--------|
| 77 | LR: visualize all stack frames, including state frames | Feature | Pending |
| 78 | Refactor visualization pattern: parseWithTrace returns trace, then visualize trace | Refactoring | Pending |
| 79 | Implement graph hierarchy (Graph, Automaton wraps Graph) | Feature | Pending |
| 80 | Filtering graph edges via diagonal matrix multiplication | Feature | Pending |

## Dependencies

```
Task 78 (refactor viz pattern) ── depends on nothing, affects LL/LR visualizers
Task 77 (LR state frames) ── depends on Task 78 (needs trace data, not visualizeSteps)
Task 79 (graph hierarchy) ── independent, affects Automaton.fs
Task 80 (diagonal filtering) ── depends on Task 79 (needs Graph type from 79), uses LinearAlgebra
```

Tasks 78 and 79 are fully independent. Task 77 depends on 78 because 77 needs raw trace data (LRParsingStep with state frames) rather than pre-rendered VisualizationStep strings. Task 80 depends on 79 since it operates on the new Graph type.

## Potential Conflicts

| Task | Files Modified | Conflicts With |
|------|---------------|----------------|
| 77 | `DerivationTreeDot.fs`, `LRStepVisualizer.fs`, `LRVisualizerTests.fs` | Task 78 (same visualizer files) |
| 78 | `VisualizationTypes.fs`, `LLStepVisualizer.fs`, `LRStepVisualizer.fs`, `LLVisualizerTests.fs`, `LRVisualizerTests.fs`, `Program.fs`, docs | Task 77 (same visualizer files) |
| 79 | `Automaton.fs` → renamed/shared, new `Graph.fs`, tests, docs | Task 80 (Graph type used) |
| 80 | `LinearAlgebra.fs`, new tests, docs | Task 79 (needs Graph) |

## Execution Order

1. **Task 78** — Refactor visualization pattern:
   - `parseWithSteps` already returns `(Option<DerivationTree>, LRParsingStep list)` / `LLParsingStep list`
   - The steps ARE the trace data (LRParsingStep, LLParsingStep contain F# data structures)
   - Remove `LLStepVisualizer.visualizeSteps` and `LRStepVisualizer.visualizeSteps` — they currently call parser internally
   - Replace with pure rendering functions: `LLStepVisualizer.renderStep` and `LRStepVisualizer.renderStep` that take a step and return `VisualizationStep`
   - Update CLI (`Program.fs`) to call `parseWithSteps` first, then render
   - Update tests to use the new pattern
2. **Task 77** — LR visualize all stack frames including state frames:
   - Modify `DerivationTreeDot.toDotWithStack` or create a new variant that also visualizes `LRState` frames
   - `LRState(n)` frames appear as labeled nodes (e.g., "s0", "s1") in the DOT graph
   - State frame nodes are included in the `rank=same` constraint
   - Update tests: verify LRState frames appear in visualization
3. **Task 79** — Implement graph hierarchy:
   - Create `Graph.fs` in `FLPQ.Languages` with `Graph<'v,'e>` type: vertices map `Map<int, 'v>`, edges `Matrix<'e>`
   - Functions: vertex count, get vertex, add vertex, edge operations
   - Refactor `Automaton.fs`: NFA/DFA wrap `Graph<'s, Option<NonEmptySet<'t>>>`
   - NFA/DFA add start/final state info on top of Graph
   - Backward compatibility: keep existing accessor functions working
4. **Task 80** — Diagonal matrix filtering:
   - Add `Matrix.diagonal` function: create diagonal matrix from list of indices and value, rest are zero
   - Add `Graph.filterEdgesByVertices` using diagonal matrix multiplication (left-multiply for outgoing, right-multiply for incoming)
   - Tests: verify filtered graph has only edges from/to specified vertices

## Shared Infrastructure

- Task 78 and 77 share the DOT visualization of stack frames in `DerivationTreeDot.fs`
- Task 79 and 80 share the `Graph` type defined in task 79
- No other shared infrastructure needed across these 4 tasks

## Architecture Alignment

- **Separation of data collection and rendering** (Task 78): This aligns with existing pattern used by CYK/Valiant. LL and LR were outliers.
- **Graph hierarchy** (Task 79): Graph is the fundamental structure. Automaton is Graph + start/final annotation. This matches the book's definitions.
- **Matrix operations** (Task 80): All operations on graphs are expressed through existing matrix algebra. No ad-hoc loops.
