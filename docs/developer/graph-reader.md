# Graph Reader Module

**Tags:** utility, graph, file-io, parsing, text
**Kind:** utility
**Module:** GraphReader
**Source:** `src/FLPQ.RPQ/GraphReader.fs`
**Depends on:** Automaton, Graph
**Used by:** BelyaninRPQ, ArroyueloRPQ, KroneckerRPQ, FLPQ.Cli

> **Abstract:** Provides graph file parsing for RPQ algorithms. Reads labeled graphs from text files and returns them as NFAs where states are vertices, transitions are labeled edges, and start states correspond to source vertices. Supports optional explicit start vertex specification. Used by all three RPQ algorithms for loading input graphs.

## Contents

- [Purpose](#purpose)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Purpose

Graph files are the input for Regular Path Querying algorithms. The module parses a simple text format (optional start vertices line, followed by `fromVertex label toVertex` triples) and returns a unified representation (`NFA<string, int>`) that all RPQ algorithms consume.

## Function Signatures

### `parseGraph: string -> NFA<string, int>`
Parse a graph from text. Input format:
- Optional first line: space-separated start vertex indices (0-based). If absent, all vertices are start vertices.
- Following lines: `fromVertex label toVertex` triples.

### `parseGraphFile: string -> NFA<string, int>`
Parse a graph from a file. Convenience wrapper around `parseGraph`.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Returns `NFA<string, int>` | Unifies the interface across all RPQ algorithms |
| Defaults to all vertices as start states | When no explicit sources are given |
| Vertex count from maximum index | Computes from the maximum vertex index appearing in edges |
| Generic over label type | Currently only string labels are supported |
| Start vertices as NFA start states | Encodes graph sources; RPQ algorithms iterate over them |

## See Also

- [Belyanin RPQ](belyanin-rpq.md) — uses parsed graphs as input
- [Arroyuelo RPQ](arroyuelo-rpq.md) — uses parsed graphs as input
- [Kronecker RPQ](kronecker-rpq.md) — uses parsed graphs as input
- [Automaton module](automaton.md) — NFA type
