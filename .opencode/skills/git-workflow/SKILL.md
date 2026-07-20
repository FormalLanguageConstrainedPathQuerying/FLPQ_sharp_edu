---
name: git-workflow
description: Use when doing git operations: branching, committing, merging, rebasing. Covers branch naming convention, commit message format, merge strategy, and pre-commit/pre-merge checks for this project.
---

# Git Workflow

## Branching

- Stable development branch: `dev`
- Protected main branch: `main`
- Feature branches: `feature/XXX-short-description` where `XXX` is the task ID from `tasks/tasks.md`
- One task per branch — never combine multiple task IDs in a single branch

## Commits

### Message format

Conventional Commits with **exactly one** subtask identifier:

```
feat(XXX-SN): description
fix(XXX-SN): description
docs(XXX-SN): description
```

- `XXX` — task ID from `tasks.md`
- `SN` — a **single** atomic subtask identifier from `tasks/detailed_plan.md` (e.g., `S1`, `S4`). Ranges (`S1-S6`), lists (`S1,S3`), or commas are forbidden
- One commit per completed atomic subtask — never combine subtasks in one commit

**Pre-commit validation**: before running `git commit`, verify the message contains exactly one `SN` by checking the prepared message. If the message mentions multiple subtask identifiers, STOP — split the changes into individual commits.

### Pre-commit checklist

1. Run commit gate (format + build). See `quality-gates` skill.
2. **Verify `tasks.md` is not staged:**
   ```bash
   git diff --cached --name-only | grep -q tasks.md
   ```
   If it matches, unstage it: `git reset HEAD tasks.md`
   `tasks.md` must never be committed from a feature branch.

### Commit scope

- Each commit is a self-contained, compilable, testable increment
- Commit messages must be detailed enough to understand why changes were required

## Merging to dev

### Pre-merge checks

The hard gate MUST pass before merging. Run asynchronously with periodic polling:

```bash
python3 tools/hard_gate.py > tmp/hard-gate-stderr.txt 2>&1 &
```

See the `quality-gates` skill for the full async polling procedure. Poll `grep "STATUS:" tmp/hard-gate.txt` every 5 minutes. Only non-zero exit code — for ANY reason — is a blocker. Do not merge. Only merge when exit code 0.

### Merge strategy

- Use **Squash and Rebase** from feature branch to `dev`
- History of `dev` must be linear
- Merge commit message on `dev` must include the full detailed body from the feature branch commit(s) — a bare subject line is insufficient

## Rules

- No emergency fixes
- All work is local, no `push`-es
- If a test cannot pass, use `[<Fact(Skip="explanation")>]` with a clear reason — never an empty body `()`, tautological assertions, or commented-out asserts
- Never run `git checkout tasks/tasks.md` or `git restore tasks/tasks.md` — the working-tree version is authoritative and may contain uncommitted user-authored content
