# Detailed Plan: Task 27 — Improve graphs and trees visualization tests

## Goal

Enhance `TestUtils.checkDotCompiles` to parse `dot -Tplain` output and return structured data (node count, edge count, etc.). Add assertions in visualization tests that verify expected graph structure.

## -Tplain Format

The plain format from graphviz `dot -Tplain` contains:
```
graph scale_x scale_y
node name x y width height label style shape color fillcolor
edge tail head n x1 y1 ... xn yn [label x y] style color
stop
```

## Changes

### 1. Enhance TestUtils

Add a function that:
- Runs `dot -Tplain` and captures stdout
- Parses the output to count nodes, edges, and extract labels
- Returns a record type with parsed data

### 2. Add assertions in existing tests

Each visualization test should assert:
- Correct number of nodes
- Correct number of edges
- (Optionally) correct labels for key nodes

### Files

| File | Action |
|------|--------|
| `tests/FLPQ.Languages.Tests/TestUtils.fs` | Add `checkDotCompilesWithInfo` returning parsed data |
| `tests/FLPQ.Languages.Tests/AutomatonVisualizationTests.fs` | Add node/edge count assertions |
| `tests/FLPQ.Languages.Tests/DerivationTreeVisualizationTests.fs` | Add node/edge count assertions |
