# Detailed Plan: Task 89 — Fix input rendering for LL and LR

## Goal

Render Terminal content without F# type wrappers in LL and LR step visualization. Use a proper symbol print function instead of `string`.

## Changes

### 1. `src/FLPQ.Cli/Program.fs`
- Line 160: Change `LLStepVisualizer.renderSteps string steps` to `LLStepVisualizer.renderSteps symbolPrinter steps`
- Line 183: Change `LRStepVisualizer.renderSteps string steps` to `LRStepVisualizer.renderSteps symbolPrinter steps`

### 2. `tests/FLPQ.Printers.Tests/LLVisualizerTests.fs`
- Add a `symbolPrinter` helper that unwraps Terminal/Nonterminal/Epsilon
- Replace `string` with `symbolPrinter` in all calls to `LLStepVisualizer.renderSteps`

### 3. `tests/FLPQ.Printers.Tests/LRVisualizerTests.fs`
- Add a `symbolPrinter` helper that unwraps Terminal/Nonterminal/Epsilon
- Replace `string` with `symbolPrinter` in all calls to `LRStepVisualizer.renderSteps`

## Verification
- All tests pass
- Check formatting
