# Visualization Types

## Overview

Shared types for LL and LR parser step visualization.

## Types

- `VisualizationStep` (struct): contains pre-rendered `tree: string` (DOT), `stack: string` (TeX), `input: string` (TeX) for a single parser step

## Modules

- `LLVisualizer.visualizeSteps` — wraps `LLParser.parseWithSteps`, returns `VisualizationStep list`
- `LRVisualizer.visualizeSteps` — wraps `LRParser.parseWithSteps`, returns `VisualizationStep list`

## Design decisions

- Struct type for stack allocation efficiency
- Pre-rendered strings avoid coupling visualization consumers to rendering libraries
