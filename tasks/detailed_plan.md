# Task 249: Fix BasicSppf.fromParsingTable Production Node Reuse - Detailed Plan

## S1: Replace getOrCreate for Production nodes with direct allocation

**Code:** `src/FLPQ.Languages/BasicSppf.fs` — modify `processCell` in `fromParsingTable`
**Tests:** None (verified by existing BasicSppf tests and tree yield tests)
**Docs:** None

**Spec:**
- In `fromParsingTable`, replace `getOrCreate(Production(...))` with direct vertex allocation
- Production nodes are context-dependent (parent cell determines children), so sharing is incorrect
- Nonterminal and Terminal nodes keep `getOrCreate` deduplication (correct for these)
