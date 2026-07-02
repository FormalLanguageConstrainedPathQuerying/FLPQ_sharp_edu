# Global Plan: Tasks 84--87

## Task Summary

| ID | Description | Type | Status |
|----|-------------|------|--------|
| 84 | Fix grammar rendering in TeX. Arrow is broken. | Bug fix | Pending |
| 85 | Add generation of input string for CYK and Valiant. | Feature | Pending |
| 86 | In run_viz.py: pdflatex non-zero exit is an error, not a warning. | Bug fix | Pending |
| 87 | Add generation for LL and LR: grammar, table, automata for LR, parsing steps. | Feature | Pending |

## Dependencies

```
Task 84 ── independent (one-line fix in GrammarTeX.fs)
Task 85 ── independent in CLI changes, touches run_viz.py
Task 86 ── independent (run_viz.py only)
Task 87 ── depends on Task 84 (uses fixed grammar TeX), touches same files as 85+86
```

Tasks 84, 85, and 86 are independent of each other. Task 87 depends on Task 84 (grammar TeX fix) and overlaps in files with Tasks 85 and 86.

## Potential Conflicts

| Task | Files Modified | Conflicts With |
|------|---------------|----------------|
| 84 | `src/FLPQ.Printers/GrammarTeX.fs` | None (unique file) |
| 85 | `src/FLPQ.Cli/Program.fs`, `run_viz.py` | 87 (same files) |
| 86 | `run_viz.py` | 85, 87 (same file) |
| 87 | `src/FLPQ.Cli/Program.fs`, `run_viz.py` | 85, 86 (same files) |

## Execution Order

1. **Task 84** — Fix grammar rendering arrow:
   - Change `sprintf "%s &\rightarrow %s \\\\"` to `sprintf @"%s &\rightarrow %s \\"` in `GrammarTeX.fs:32`
   - `\rightarrow` is broken because `\r` is interpreted as carriage return in non-verbatim string
   - Use verbatim string `@""` to preserve literal `\rightarrow`

2. **Task 85** — Add input string generation for CYK and Valiant:
   - In `runCyk` (`Program.fs:54`): add `writeOutputFile (Path.Combine(outputDir, "input.tex")) (TeXRenderer.inputRow string inputTokens 0)` — renders full input as one-row matrix without position marker (position 0 = first cell underlined? No... position 0 means index 0 is underlined. Wait, `inputRow` underlines the cell at `position`. For a static display of the full input, position should be -1 or something else. Actually, looking at the code, `inputRow` takes a position and underlines that cell. For a "show the input string" without underlining, we could pass -1 or set position outside range. Let me re-read... Actually, let's look at the inputRow code: if position = 0 and tokens are `[a, a, b, a, b, b]`, the first `a` would be underlined. We want NO cell underlined. We could pass -1 or just use a different approach. Actually, the simplest is to render the input tokens as a one-row `pNiceMatrix` without underlining. We can just call it with a sentinel position. But looking at the code, `inputRow` always underlines position. So I need a variant that doesn't underline anything. Let me add a separate function or use position outside range.)
   - Actually, let me think about this more carefully: for CYK and Valiant, we want to show the full input string. The `inputRow` function underlines the current position. For the static input display, we want NO underline. Options:
     a. Add a new function that renders tokens without position marker
     b. Pass a position outside range and handle gracefully
   - Looking at inputRow code: `if i = position then @"\underbar{" + s + "}" else s`. If position = -1, no cell matches. So just pass -1.
   - In `runValiant` (`Program.fs:80`): same addition
   - In `run_viz.py` `process_algorithm`: after grammar sections, add input string section for CYK/Valiant (read `input.tex` if it exists from viz root)

3. **Task 86** — Fix run_viz.py pdflatex error handling:
   - In `run_command` (`run_viz.py:69`): change print from "WARNING" to "ERROR"
   - In `run_algorithm` (`run_viz.py:87`): check return value of `run_command` and exit with error code 1 if CLI fails
   - In `compile_tex_to_pdf` call and its callers: if pdflatex fails (returns False), exit with error code 1
   - In `main` after the algorithm loop: if `merged_pdf` doesn't exist, exit with error code 1 (not just print warning)

4. **Task 87** — Add LL and LR generation: grammar, table, automata for LR, parsing steps:
   - **LL changes** in `Program.fs` (`runLL`):
     - Write `grammar_original.tex` (like CYK/Valiant do)
     - Write `ll_table.tex` using `LLTableTeX.tableToTeX`
   - **LR changes** in `Program.fs` (`runLR`):
     - Write `grammar_original.tex` (like CYK/Valiant do)
     - Write `lr_table.tex` using `LRTableTeX.tableToTeX`
     - Build LR automaton (LR(0) for LR(0) mode or based on what table was built). Actually, `runLR` currently builds SLR(1). We should also build and write the LR automaton. For SLR(1), the automaton is LR(0). Let's write it as `lr_automaton.dot` using `AutomatonDot.dfaToDot`.
   - In `run_viz.py`:
     - Include `ll_table.tex` and `lr_table.tex` in the `process_algorithm` function for LL/LR respectively
     - For LR: include `lr_automaton.dot` (compile to PDF, include in TeX)
     - Include `grammar_original.tex` for LL/LR (already generic code handles this)
     - Add LL and LR to `--example` mode: run on `S -> a S b S | eps` with input `aababb`
     - For the example: also add `--algorithms` to include LL and LR by default, or extend `--example` to run all four

## Shared Infrastructure

- All tasks use existing infrastructure (GrammarTeX, LLTableTeX, LRTableTeX, AutomatonDot, TeXRenderer)
- No new shared infrastructure needed

## Architecture Alignment

- **Task 84**: Minimal fix — GrammarTeX.fs line 32, change string to verbatim
- **Task 85**: Add input.tex to CYK/Valiant CLI output, read it in run_viz.py. Follow existing pattern for LL/LR input.
- **Task 86**: run_viz.py quality improvement — treat pdflatex failures as hard errors
- **Task 87**: Extend CLI and run_viz.py to generate full output for LL/LR. Follow existing CYK/Valiant patterns. Grammar TeX at root, table TeX at root, steps in subdirectories.
