# FLPQ.RPQ

**Tags:** rpq, regular, automaton, graph, kronecker-product, bfs, boolean-decomposition, matrix-multiplication
**Kind:** hub
**Source:** `src/FLPQ.RPQ/`
**Depends on:** FLPQ.LinearAlgebra, FLPQ.GraphAnalysis, FLPQ.Languages, FSharpPlus
**Used by:** FLPQ.Cli
**Book reference:** Chapters 3, 11, 12

> **Abstract:** Regular Path Querying library implementing three RPQ algorithms (Belyanin's BFS-based LARPQ, Arroyuelo's matrix-based regex evaluation, Kronecker-based automaton intersection) and graph file reading. All three algorithms accept a DFA query and an NFA labeled graph, returning a boolean reachability matrix. Property-based equivalence tests verify identical results across algorithms.

## Contents

- [Project](#project)
- [Modules](#modules)
- [Role](#role)
- [Book References](#book-references)
- [See Also](#see-also)

## Project

- **Type**: F# class library (`net10.0`)
- **Path**: `src/FLPQ.RPQ/`
- **Dependencies**: `FLPQ.LinearAlgebra`, `FLPQ.GraphAnalysis`, `FLPQ.Languages`, FSharpPlus

## Modules

| Module | Source | Documentation |
|--------|--------|---------------|
| `GraphReader` | `GraphReader.fs` | [Graph Reader module design and logic](graph-reader.md) |
| `BelyaninRPQ` | `BelyaninRPQ.fs` | [Belyanin RPQ module design and logic](belyanin-rpq.md) |
| `ArroyueloRPQ` | `ArroyueloRPQ.fs` | [Arroyuelo RPQ module design and logic](arroyuelo-rpq.md) |
| `KroneckerRPQ` | `KroneckerRPQ.fs` | [Kronecker RPQ module design and logic](kronecker-rpq.md) |

## Role

Implements Regular Path Querying — finding vertices reachable from source vertices along paths whose labels form a word in a given regular language:
- **Belyanin's LARPQ** — BFS-based single-source RPQ: propagation through simultaneous automaton + graph transition
- **Arroyuelo's RPQ** — matrix-based regex evaluation: translates regular expression to Boolean matrix expression, evaluates post-order
- **Kronecker-based RPQ** — Kronecker product of automaton and graph adjacency matrices with MS-BFS filtering
- **GraphReader** — reads labeled graphs from text files, returns graph as NFA

All three algorithms accept a DFA (query) and an NFA (labeled graph), returning a boolean reachability matrix. Property-based tests verify they produce identical results.

## Book References

- Chapter 3: MS-BFS (used by Kronecker-based RPQ)
- Chapter 11: Belyanin's algorithm, Arroyuelo's algorithm
- Chapter 12: Tensor product approach (Kronecker-based RPQ)

## See Also

- [Belyanin RPQ module](belyanin-rpq.md) — BFS-based single-source RPQ
- [Arroyuelo RPQ module](arroyuelo-rpq.md) — matrix-based regex evaluation
- [Kronecker RPQ module](kronecker-rpq.md) — Kronecker product with MS-BFS
- [Graph Reader module](graph-reader.md) — labeled graph I/O
- [FLPQ.GraphAnalysis](FLPQ.GraphAnalysis.md) — graph and MS-BFS infrastructure
