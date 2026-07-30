# Detailed Plan: Task 217 — RNGLR Descriptor Refactoring (S1-S8)

### S1: Add GssIdx to RnglrDescriptor + redesign RnglrGSS type

**Code:** `src/FLPQ.Languages/RnglrTypes.fs`
**Tests:** Build check
**Docs:** None (docs in S8)

**Spec:**
- RnglrDescriptor: add `GssIdx: int` field
- RnglrGSS type: replace GssGraph with VertexLookup, VertexInfo, Edges (Dictionary-based)
- Replace StoredStates array with Dictionary<int, Set<...>>
- RnglrGSS module: remove linearIndex, init; add create, getOrCreateVertex, getVertexInfo
- Adapt addEdge, outgoingEdges, getStoredStates, setStoredStates to new internals

### S2: Add collectActiveGssForDict to GraphHelpers

**Code:** `src/FLPQ.Languages/GllTypes.fs`
**Tests:** Build check
**Docs:** None

**Spec:**
- New function: collectActiveGssForDict with same return type (Set<int> * Set<int*int>)
- Iterates Dictionary keys for vertices, nested keys for edges

### S3: Adapt Rnglr.fs algorithm to new GSS + descriptor

**Code:** `src/FLPQ.Languages/Rnglr.fs`
**Tests:** `RnglrTests.fs` — all pass
**Docs:** None

**Spec:**
- RnglrGSS.init → RnglrGSS.create()
- Remove linearIdx local function
- Descriptor creation: use getOrCreateVertex, store GssIdx
- Graph.getVertex → getVertexInfo
- GraphHelpers.collectActiveGss → collectActiveGssForDict
- processedGotos: array→Dictionary
- productBfs/findPredecessors/processReduction: vertex access adaptations

### S4: Adapt step collection (edge symbols + actions) to new GSS

**Code:** `src/FLPQ.Languages/Rnglr.fs`
**Tests:** All algorithm tests pass
**Docs:** None

**Spec:**
- collectEdgeSymbols: use getVertexInfo instead of Graph.getVertex (outgoingEdges signature unchanged)
- Action tracking (stepShiftTerminals etc.) — unchanged, still works with LR state/input vertex

### S5: Update RnglrStepVisualizer for 3-field descriptor + new vertex numbering

**Code:** `src/FLPQ.Printers/RnglrStepVisualizer.fs`
**Tests:** Visualization tests
**Docs:** None

**Spec:**
- rnglrDescriptorToTeX: 3 fields (lrState, vertex, gssIdx)
- descriptorsTableToTeX: header 3 columns
- newDescriptorsToTeX: picks up 3-field format from rnglrDescriptorToTeX
- renderStep: currentGssIdx from step.CurrentDescriptor.Value.GssIdx directly
- GSS DOT vertex label: need vertex info lookup; pass via step data or separate parameter

### S6: Pass vertex info + update runner

**Code:** `src/FLPQ.Printers/RnglrStepVisualizer.fs`, `src/FLPQ.Cli/RnglrRunner.fs`
**Tests:** Runner tests
**Docs:** None

**Spec:**
- Carry vertexInfo in RnglrParsingStep or pass to visualizer
- GSS DOT vertex labels show sequential IDs with (lrState, v) lookup

### S7: Golden data regeneration

**Code:** `tests/FLPQ.Printers.Tests/GoldenHelpers.fs`, `tests/FLPQ.Printers.Tests/GoldenData/`
**Tests:** All passing
**Docs:** None

**Spec:**
- Regenerate all 6 RNGLR golden files
- Update regex patterns for new vertex numbering

### S8: Update developer docs

**Code:** `docs/developer/rnglr.md`
**Tests:** None
**Docs:** Updated

**Spec:**
- Update RnglrDescriptor (3 fields), RnglrGSS (lazy creation)
- Update GSS module table, design decisions
