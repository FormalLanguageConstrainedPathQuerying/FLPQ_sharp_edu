# Detailed Plan: Task 195 — Add PathIndex-to-SPPF node count invariants

## Task Description

Add invariants to GLL and RNGLR tests (`accepts` function) comparing node counts between PathIndex and SPPF. If SPPF construction adds nodes not represented in PathIndex, fix the SPPF construction code.

## Analysis

### Current State
- `accepts` function in `TestHelpers.fs` runs PathIndex invariants (`assertPathIndexInvariant`) and SPPF invariants (`assertSppfInvariant`) but has no cross-structure node count checks
- No counting/aggregation functions exist in `PathIndex.fs` or `Sppf.fs` — only per-node validation predicates
- Range nodes are implicit in the PathIndex (each cell corresponds to a range) so they are excluded from SPPF counting

### Reuse Checklist
1. **Existing invariant structure** (`TestHelpers.fs:10-76`) — `assertPathIndexInvariant` and `assertSppfInvariant` pattern; new checks follow the same `Result<unit, string list>` pattern
2. **`PathIndex.get`** — existing accessor to iterate all matrix cells, reused for counting
3. **`Graph.vertexCount`, `Graph.getVertex`** — existing SPPF graph accessors, reused for counting
4. **`Result<unit, string list>` pattern** — all existing validators return this type; new invariant follows same convention

---

## Subtasks

### S1: Add counting functions to PathIndex and SPPF modules

**Code:**
- `src/FLPQ.Languages/PathIndex.fs` — add `countPNonterminals`, `countPTerminals`, `countPEpsilons`, `countPIntermediates`
- `src/FLPQ.Languages/Sppf.fs` — add `countNonterminals`, `countTerminals`, `countEpsilons`, `countIntermediates`

**Tests:** None (private helper functions, tested indirectly via invariant enforcement)
**Docs:** None

**Spec:**
- PathIndex counting functions iterate all K×K cells, sum counts per entry type:
  - `countPNonterminals (pi: PathIndex<'t,'nt>) : int` — count all `PNonterminal` entries across all cells
  - `countPTerminals (pi: PathIndex<'t,'nt>) : int` — count all `PTerminal` entries across all cells
  - `countPEpsilons (pi: PathIndex<'t,'nt>) : int` — count all `PEpsilonNonterminal` entries across all cells
  - `countPIntermediates (pi: PathIndex<'t,'nt>) : int` — count all `PIntermediate` entries across all cells
- SPPF counting functions iterate all vertices in the SPPF graph, count per node type:
  - `countNonterminals (sppf: SPPF<'t,'nt>) : int` — count all `SppfNonterminal` vertices
  - `countTerminals (sppf: SPPF<'t,'nt>) : int` — count all `SppfTerminal` vertices
  - `countEpsilons (sppf: SPPF<'t,'nt>) : int` — count all `SppfEpsilon` vertices
  - `countIntermediates (sppf: SPPF<'t,'nt>) : int` — count all `SppfIntermediate` vertices
  - Note: `SppfRange` nodes are NOT counted (they correspond to PathIndex cells, not cell content)

### S2: Add invariant validation function

**Code:** `src/FLPQ.Languages/PathIndex.fs` — add `checkSppfCoverageInvariant`

**Tests:** None (called from `accepts`, failure is visible as test failure)
**Docs:** None

**Spec:**
- `checkSppfCoverageInvariant (pi: PathIndex<'t,'nt>) (sppf: SPPF<'t,'nt>) : Result<unit, string list>`
- Compare counts:
  1. `countPNonterminals pi >= countNonterminals sppf` (no SPPF nonterminal without PathIndex entry)
  2. `countPTerminals pi >= countTerminals sppf` (no SPPF terminal without PathIndex entry)
  3. `countPEpsilons pi >= countEpsilons sppf` (no SPPF epsilon without PathIndex entry)
  4. `countPIntermediates pi >= countIntermediates sppf` (no SPPF intermediate without PathIndex entry)
- Return `Error` with list of violated conditions if any check fails
- Range nodes excluded: they are cells in PathIndex, not cell content

### S3: Integrate invariant into `accepts` function

**Code:** `tests/FLPQ.TestUtilities/TestHelpers.fs` — add `assertSppfCoverageInvariant` and call it in `accepts`

**Tests:** None (integration — existing tests verify behavior)
**Docs:** None

**Spec:**
- Add `assertSppfCoverageInvariant (pi: PathIndex<string,string>) (sppf: SPPF<string,string>) : unit` to TestHelpers.fs
  - Calls `PathIndex.checkSppfCoverageInvariant`, fails with descriptive message if violation found
- Insert call in `accepts` after `assertSppfInvariant sppf` line (line 215), before tree extraction
  - `assertSppfCoverageInvariant pathIndex sppf`

### S4: Run all tests, fix violations if any

**Code:** Fix any code in Sppf.fs or algorithm modules (Gll.fs, Rnglr.fs) if the invariant reveals violations per task clause 5

**Tests:** Run `dotnet test FLPQ.slnx` — all must pass
**Docs:** None

**Spec:**
- Run `dotnet test FLPQ.slnx`
- If any test fails due to the new invariant:
  - Identify which node type is over-counted in SPPF
  - Per task clause 5: "If SPPF construction adds some nodes not represented in PathIndex — remove respective code"
  - Fix Sppf.buildSppfFromIndex to not create SPPF nodes that lack corresponding PathIndex entries
- Iterate until 0 failures, 0 skipped
