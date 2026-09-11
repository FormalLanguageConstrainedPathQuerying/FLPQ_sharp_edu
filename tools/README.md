# Tools

Auxiliary tools for project work and code quality control.

## Conventions

1. All tools run **without timeout** — let each command take as long as it needs.
2. All output is written to `tmp/<tool-name>.txt`. **Nothing** is written to console.
3. Output files are overwritten on each run (no append).
4. Each output file follows a structured format:
   - **Header**: short summary of findings (`PASS` / `BLOCKED` / `IN_PROGRESS`)
   - **Separator**: `--- DETAILED LOG ---`
   - **Body**: detailed output of each executed command
5. Workflow: run tool → analyze output file → fix all problems → re-run tool.
6. **Incremental flushing** (`hard_gate.py` only): the output file is refreshed after each step. A running gate shows `STATUS: IN_PROGRESS` — read the file at any time to check intermediate progress. The final `STATUS: PASS` or `STATUS: BLOCKED` is only written when the process exits. The exit code is the authoritative signal; never treat `IN_PROGRESS` as a result.

For detailed per-tool documentation (steps, thresholds, output format examples), see `docs/developer/guides/tools.md`.

## Setup

The markdown toolchain requires a one-time install per environment:

```bash
pip install mdformat mdformat-frontmatter
pip install -e tools/mdformat_tasklog
```

- `mdformat` — Markdown formatter; all `.md` files in the repo must be mdformat-clean.
- `mdformat-frontmatter` — keeps YAML frontmatter intact (SKILL.md files).
- `tools/mdformat_tasklog` — local plugin: ordered lists keep consecutive numbering without zero-padding (explicit list numbers such as task entries survive formatting), and GFM tables are parsed and rendered correctly (mdformat's default preset has no table support and would destroy them).

## Tools

| Script | Purpose |
| --- | --- |
| `detect_changes.py` | Detect projects with modified `.fs` files relative to `dev` |
| `quality_check.py` | Inter-subtask check: markdown format + format + build |
| `hard_gate.py` | Full gate: markdown format + format + build + tests per project with coverage + lint on changed projects. Each step shows `<N>/<M>` progress counter. Tests run per-project with coverage merged at the end. |
| `split_tasks.py` | Task log formatter/splitter: repairs and mdformat-normalizes `tasks/tasks.md` and `tasks/tasks<N>.md`, verifies structure preservation, and archives the oldest 100 entries to the next `tasks/tasks<N>.md` when the active log exceeds 100 entries. Run after every task-log change. |

## Usage

```bash
python3 tools/detect_changes.py
python3 tools/quality_check.py
python3 tools/hard_gate.py
python3 tools/split_tasks.py
```

After each run, read and analyze the corresponding `tmp/<script>.txt` file:

```bash
# Read the summary only (lines before DETAILED LOG)
sed -n '1,/DETAILED LOG/p' tmp/detect-changes.txt

# Read full output
cat tmp/detect-changes.txt
```
