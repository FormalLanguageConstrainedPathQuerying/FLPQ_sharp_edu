# Graph Reader Module Design

## Overview

The `GraphReader` module in `FLPQ.RPQ` provides graph file parsing. The graph is returned as an NFA where states are vertices, transitions are labeled edges, and start states correspond to source vertices.

## Function Signatures

### `parseGraph: string -> NFA<string, int>`
Parse a graph from text. Input format:
- Optional first line: space-separated start vertex indices (0-based). If absent, all vertices are start vertices.
- Following lines: `fromVertex label toVertex` triples.

Returns an NFA where:
- States are integers 0..vertexCount-1
- Transitions encode labeled edges
- Start states are the specified (or all) source vertices
- No final states — RPQ algorithms determine reachability via the query automaton's final states

### `parseGraphFile: string -> NFA<string, int>`
Parse a graph from a file. Convenience wrapper around `parseGraph`.

## Design Decisions

- Returns `NFA<string, int>` to unify the interface across all RPQ algorithms (task 64).
- Defaults to all vertices as start states when no explicit sources are given.
- Computes vertex count from the maximum vertex index appearing in edges.
- Generic over label type — currently only string labels are supported.
- Start vertices encode graph sources as NFA start states.

## Relationship to the Book

- Chapter 11: RPQ algorithms that operate on labeled graphs.
- The NFA representation unifies the graph and query representations, enabling a consistent interface across Belyanin, Arroyuelo, and Kronecker algorithms.
