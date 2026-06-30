# Graph Reader Module Design

## Overview

The `GraphReader` module in `FLPQ.Languages` provides graph file parsing into per-label boolean adjacency matrices. Used as input for RPQ algorithms.

## Type Definitions

### `LabeledGraph<'t>`
```
{ vertexCount: int
  labels: Set<'t>
  adjacency: Map<'t, Matrix<bool>>
  startVertices: int[] }
```

## Function Signatures

### `parseGraph: string -> LabeledGraph<string>`
Parse a graph from text. Input format:
- Optional first line: space-separated start vertex indices (0-based). If absent, all vertices are start vertices.
- Following lines: `fromVertex label toVertex` triples.

Returns a `LabeledGraph` with per-label boolean adjacency matrices and start vertices.

### `parseGraphFile: string -> LabeledGraph<string>`
Parse a graph from a file. Convenience wrapper around `parseGraph`.

## Design Decisions

- Uses `Map<'t, Matrix<bool>>` for per-label adjacency storage.
- Defaults to all vertices as start vertices when no explicit sources are given.
- Computes vertex count from the maximum vertex index appearing in edges.
- Generic over label type — currently only string labels are supported.

## Relationship to the Book

- Chapter 11: RPQ algorithms that operate on labeled graphs.
- Per-label adjacency matrices correspond to the G^a matrices used in Belyanin and other RPQ algorithms.
