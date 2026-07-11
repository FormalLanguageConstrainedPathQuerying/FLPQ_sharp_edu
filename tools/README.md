# Tools

Auxiliary tools for project work and code quality control.

## Conventions

1. All tools run **without timeout** — let each command take as long as it needs.
2. All output is written to `tmp/<tool-name>.txt`. **Nothing** is written to console.
3. Output files are overwritten on each run (no append).
4. Each output file follows a structured format:
   - **Header**: short summary of findings (PASS/BLOCKED, key counts)
   - **Separator**: `--- DETAILED LOG ---`
   - **Body**: detailed output of each executed command
5. Workflow: run tool → analyze output file → fix all problems → re-run tool.

## Tools

| Script | Purpose |
|--------|---------|
| `detect_changes.py` | Detect projects with modified `.fs` files relative to `dev` |
| `quality_check.py` | Inter-subtask check: format + build |
| `hard_gate.py` | Full gate: format + build + tests with coverage + lint on changed projects |

## Usage

```bash
python3 tools/detect_changes.py
python3 tools/quality_check.py
python3 tools/hard_gate.py
```

After each run, read and analyze the corresponding `tmp/<script>.txt` file:

```bash
# Read the summary only (lines before DETAILED LOG)
sed -n '1,/DETAILED LOG/p' tmp/detect-changes.txt

# Read full output
cat tmp/detect-changes.txt
```
