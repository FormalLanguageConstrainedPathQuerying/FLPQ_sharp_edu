# Detailed Plan: Task 99 — Improve Generation for LR Family

## Changes

### 1. `src/FLPQ.Cli/AlgorithmTypes.fs`
- Replace `LR` with `LR0 | SLR1 | CLR1` in the `Algorithm` DU
- Add `displayName` helper: maps each case to its precise display name (e.g., `LR0 -> "LR(0)"`)
- Update usage message to list all LR variants

### 2. `src/FLPQ.Cli/Program.fs`
- Replace `| AlgorithmTypes.LR -> LRRunner.runLR grammar input output`
  with three cases dispatching to `LRRunner.runLR` with the algorithm variant

### 3. `src/FLPQ.Cli/LRRunner.fs`
- Add `AlgorithmTypes.Algorithm` parameter to `runLR`
- Dispatch table builder and automaton builder based on variant:
  - LR0: `buildLR0Table` + `buildLR0` automaton
  - SLR1: `buildSLR1Table` + `buildLR0` automaton
  - CLR1: `buildCLR1Table` + `buildLR1` automaton

### 4. `src/FLPQ.Cli/Summary.fs`
- Update `algorithmKind`: all three LR variants map to `StackPerStep`
- Replace `algo.ToString()` with `AlgorithmTypes.displayName algo` for display
- `algorithmLower` still works via `.ToString().ToLower()` — produces "lr0", "slr1", "clr1"
- Update LR automaton check: match on all three LR variants

### 5. `src/FLPQ.Printers/SummaryTeX.fs`
- Update `buildContent` to accept display name (already receives string)
- Update match in `algoKind` to detect LR variants via prefix or explicit check
- The "LR Automaton" header and "LR Parsing Table" header are shared across LR variants — adjust headers as needed

### 6. `data/`
- Add `example_lr_grammar.bnf`: arithmetic expression grammar (grammar2 from task 11)
- Add `example_lr_input.txt`: `x + x * x`

## Verification
- `dotnet build FLPQ.slnx -c Debug`
- `dotnet test`
- `dotnet fantomas . --check`
