---
name: git-workflow
description: "Use when doing git operations: branching, committing, merging, rebasing. Covers branch naming convention, commit message format, merge strategy, and pre-commit/pre-merge checks for this project."
---

# Git Workflow

## Branching

- Stable development branch: `dev`
- Protected main branch: `main`
- Feature branches: `feature/XXX-short-description` where `XXX` is the task ID (the GitHub issue number)
- One task per branch — never combine multiple task IDs in a single branch

## Commits

### Message format

Conventional Commits with **exactly one** subtask identifier:

```
feat(XXX-SN): description
fix(XXX-SN): description
docs(XXX-SN): description
```

- `XXX` — task ID (the GitHub issue number)
- `SN` — a **single** atomic subtask identifier from `tasks/detailed_plan.md` (e.g., `S1`, `S4`). Ranges (`S1-S6`), lists (`S1,S3`), or commas are forbidden
- One commit per completed atomic subtask — never combine subtasks in one commit

**Pre-commit validation**: before running `git commit`, verify the message contains exactly one `SN` by checking the prepared message. If the message mentions multiple subtask identifiers, STOP — split the changes into individual commits.

### Pre-commit checklist

1. Run commit gate (format + build). See `quality-gates` skill.
2. **Verify no machine-local file is staged:** nothing from the gitignored `tasks/` directory may be committed (it holds local mirrors of issue comments and history archives — the durable record lives in GitHub issues):
   ```bash
   git diff --cached --name-only | grep -q '^tasks/'
   ```
   If it matches, unstage it: `git reset HEAD <file>`

### Commit scope

- Each commit is a self-contained, compilable, testable increment
- Commit messages must be detailed enough to understand why changes were required

## Merging to dev

### Pre-merge checks

**Issue check (first):** verify the task issue exists and is open:

```bash
gh issue view NNN --json state,labels -q '.state + " " + (.labels | map(.name) | join(","))'   # NNN = task ID
```

If the issue is missing or already closed, **STOP — do not merge.** A merged task without an open issue loses its tracking record.

The hard gate MUST pass before merging. **This is absolute — no exceptions, no self-assessment.**

```bash
python3 tools/hard_gate.py &
echo $! > tmp/hard-gate.pid
```

See the `quality-gates` skill for the full async polling procedure. The gate writes to `tmp/hard-gate.txt` — poll that file every 5 minutes.

- **`STATUS: PASS`** — proceed to merge.
- **`STATUS: BLOCKED`** — **STOP. Do not merge.** Do not assess whether failures are pre-existing or unrelated to your changes. Do not rationalize. The gate is the gate. Fix every failure and re-run from the start.
- **`STATUS: IN_PROGRESS`** — keep polling. Do not proceed until terminal state.

### Merge strategy

- Use **Squash and Rebase** from feature branch to `dev`
- History of `dev` must be linear
- Merge commit message on `dev` must include the full detailed body from the feature branch commit(s) — a bare subject line is insufficient
- The merge commit message must carry `Closes #<N>` (the task's issue number) as a **standalone line** — the issue closes when the commit reaches `main`. Until then the issue stays open; "integrated" means the subtask commits are on `dev` (`git log dev --format=%s | grep -cE '\(<N>-S[0-9]+\):'` > 0)

## Rules

- No emergency fixes
- All work is local, no `push`-es
- If a test cannot pass, use `[<Fact(Skip="explanation")>]` with a clear reason — never an empty body `()`, tautological assertions, or commented-out asserts
- Never commit anything from the gitignored `tasks/` directory (local mirrors of issue comments + history archives); never edit issue bodies — they are user-authored and immutable
