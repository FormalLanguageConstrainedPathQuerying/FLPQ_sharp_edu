# RSM Visualization (Dot and Tikz)

**Tags:** visualization, rsm, dot, tikz, graphviz, gll, rnglr
**Kind:** visualization
**Module:** RsmDot, RsmTikz
**Source:** `src/FLPQ.Printers/RsmDot.fs`, `src/FLPQ.Printers/RsmTikz.fs`
**Depends on:** RSM (FLPQ.Languages), AutomatonTikz, DerivationTreeDot (label escaping)
**Used by:** GllRunner, RnglrRunner, GllStepVisualizer

> **Abstract:** Renders Recursive State Machines (RSM) and extended RSMs (augmented with the fresh start block S′). `RsmDot` emits Graphviz DOT; `RsmTikz` emits a TikZ `tikzpicture` in which each block is one connected component stacked top-to-bottom (S′ on top) while the layout inside every block grows left-to-right. Book reference: sec:CFPQ_GLL, sec:CFPQ_RNGLR.

## Contents

- [Overview](#overview)
- [Supported Formats](#supported-formats)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

An RSM is stored as a single flat automaton: one global state index space across all blocks, a `StateInfo` array mapping each state to its block, and a `BlockStart` map from nonterminal to the block's start state. Blocks are the graph's connected components — no inter-block edges exist in the transition matrix. A `call N` transition stays inside the caller's block (its target is the continuation state); the jump to N's start state is implicit and resolved by algorithms via `BlockStart[N]`.

The extended RSM appends a fresh start block S′ with two states: start → final, labeled `call <originalStart>`. Its states occupy the last global indices.

## Supported Formats

### DOT

- `toDot` — one `subgraph cluster_N` per block (dashed frame, nonterminal label; the start block's label gets a `(start)` suffix). Local state numbering inside each cluster.
- `extendedRsmToDot` — a single flat digraph with global state numbering (`s0`..`sN`). Start states are green-filled, final states double-bordered, S′ block nodes blue-fonted; an optional highlighted state is filled lightblue (same color as the current GSS node).

### Tikz

A single `tikzpicture` with one flat `\graph`:

```
\graph [layered layout, nodes={draw, circle}, grow'=right, level sep=2cm, sibling sep=1.5cm,
        components go down left aligned, component sep=1.5cm] { ... };
```

- **Inside each block** — the layered layout grows left-to-right (`grow'=right`), exactly as for a standalone DFA drawing.
- **Between blocks** — pgf-gd connected-component packing (`components go down left aligned`, PGF manual §28.7) stacks the components top-to-bottom with left edges aligned and `component sep=1.5cm` vertical padding.
- **Block order** — pgf-gd orders components by first specified node, so node declaration order controls stacking: the S′ block is declared first (on top), remaining blocks follow in global-state appearance order; within each block the start state is declared first.
- **Labels** — state content is `Nt_globalIdx` (S′ states keep the prime form `S'\_k`). Every block's start state additionally carries a plain nonterminal label to its left (`label=left:Nt`); block start states also get `label=above:Start` and `fill=green!30`, final states `double, double distance=1.5pt, fill=red!30`, a highlighted state `fill=lightblue!20`.
- **Edge labels** — terminals as-is (escaped), nonterminal calls as `call Nt`, epsilon transitions as dotted edges with a `$\varepsilon$` label (math mode).

## Function Signatures

- `RsmDot.toDot: (terminalPrinter: 't -> string) -> (nonterminalPrinter: 'nt -> string) -> RSM<'t, 'nt> -> string`
- `RsmDot.extendedRsmToDot: (terminalPrinter: 't -> string) -> (nonterminalPrinter: 'nt -> string) -> ExtendedRSM<'t, 'nt> -> highlightedState: int option -> string`
- `RsmTikz.extendedRsmToTikz: (terminalPrinter: 't -> string) -> (nonterminalPrinter: 'nt -> string) -> ExtendedRSM<'t, 'nt> -> highlightedState: int option -> string`

Printers convert symbol values to display strings; the highlighted-state argument marks the current RSM state in per-step GLL visualizations.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Single flat `\graph` with component packing instead of per-block subgraphs | Blocks are exactly the connected components, so pgf-gd packs them natively; intra-block layout stays the plain layered algorithm with no manual coordinates or height estimation |
| `components go down left aligned` + declaration order for stacking | PGF §28.7: component order defaults to *by first specified node*; declaring S′ first puts it on top, and appearance order keeps the rest deterministic |
| Plain nonterminal label left of each block's start state (no frame) | The user-confirmed delimiting style; the start state always lands in layer 0 of its component (verified for cyclic blocks such as `S -> (a S b)*`), so the label sits cleanly at the block's left edge |
| `component sep=1.5cm` | Matches `sibling sep=1.5cm` for visual consistency; the pgf-gd default (1.5em) is tighter than the intra-block spacing |
| lightblue highlight fill | Shared with the current GSS node color so step figures read consistently in the summary legend |

## See Also

- [RSM data type](rsm.md) — flat RSM storage, `extendWithStart`, block reconstruction
- [GLL parsing](gll.md) — consumes the extended RSM; per-step figures highlight the current state
- [RNGLR parsing](rnglr.md) — canonical level-based parser over the same RSM
- [Automaton visualization](automaton-viz.md) — `AutomatonTikz.escapeLatex` and the shared layered graph options
