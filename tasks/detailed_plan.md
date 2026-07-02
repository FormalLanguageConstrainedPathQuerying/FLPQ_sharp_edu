# Detailed Plan: Task 87 — Add generation for LL and LR

## Goal

Extend CLI and run_viz.py to generate complete output for LL and LR algorithms:
- Grammar rendering (TeX)
- LL parsing table (TeX) 
- LR parsing table (TeX)
- LR automaton (Dot)
- Parsing steps (already implemented)
- Add LL+LR to example generation

## Changes

### 1. `src/FLPQ.Cli/Program.fs`

**runLL** (currently lines 114-123):
- Write grammar_original.tex
- Write ll_table.tex using LLTableTeX.tableToTeX
- Keep existing step visualization

**runLR** (currently lines 125-138):
- Write grammar_original.tex
- Write lr_table.tex using LRTableTeX.tableToTeX
- Build and write LR automaton (LR(0) for SLR(1) context) as lr_automaton.dot
- Keep existing step visualization

### 2. `run_viz.py`

**process_algorithm**:
- For LL: include ll_table.tex (wrapped in \[...\] since it uses tabular)
- For LR: include lr_table.tex (tabular, needs tabularTemplate-like wrapping) and lr_automaton.dot (compile to PDF)

**--example mode**: 
- Add LL and LR to the example algorithms
- Use grammar `S -> a S b S | eps` with input `a a b a b b`

## Verification

- Run `python3 run_viz.py --example` and verify all 4 algorithms produce PDFs
- Run tests to ensure no regressions
