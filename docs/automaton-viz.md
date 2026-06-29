# Automaton Visualizer

## Overview

Visualizes NFA and DFA as Graphviz DOT graphs.

## Types

Uses `NFA<'t,'s>` and `DFA<'t,'s>` from `Automaton.fs`. No new types.

## Functions

- `nfaToDot: (int -> 's -> string) -> NFA<'t,'s> -> string` — renders NFA to DOT with state labels, green start states, double-circle final states, dotted epsilon transitions
- `dfaToDot: (int -> 's -> string) -> DFA<'t,'s> -> string` — renders DFA to DOT

## Design decisions

- State visualizer callback allows parameterized label generation
- Epsilon transitions rendered as dotted edges with epsilon label
