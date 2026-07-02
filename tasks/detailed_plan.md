# Detailed Plan: Task 86 — Fix run_viz.py pdflatex error handling

## Goal

Treat non-zero pdflatex return codes as hard errors (not warnings). The script must exit with code 1 when TeX compilation fails.

## Changes to `run_viz.py`

### 1. `run_command` (line 69)
- Change "WARNING" to "ERROR" in the print message

### 2. `run_algorithm` (line 87) 
- Check return value of `run_command`, if False, print error and exit with code 1

### 3. `compile_tex_to_pdf` callers in `process_algorithm` (lines 224-225)
- Check return value of `compile_tex_to_pdf`, if False, print error and exit with code 1

### 4. `main` function
- After algorithms loop, if `merged_pdf` doesn't exist for any algorithm, print error and exit with code 1

## Verification

- Run `run_viz.py --example`, verify it completes successfully with pdflatex
- (Harder to test failure case without breaking TeX, but the logic change is straightforward)
