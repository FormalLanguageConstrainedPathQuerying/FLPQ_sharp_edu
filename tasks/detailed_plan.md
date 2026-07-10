# Detailed Plan: Task 157 — Prevent subtask batching

## S1: Rename "Commit Gate" → "Pre-Commit Check" in subtask-loop skill

**Code:** N/A
**Tests:** N/A
**Docs:** Edit `.opencode/skills/subtask-loop/SKILL.md`

**Spec:**
- Line 52: Rename `### 4. Commit Gate` → `### 4. Pre-Commit Check`
- Line 54: `Run commit gate (format + build)` → `Run format + build`
- Line 104 (example): `"S1: Commit gate (format + build)"` → `"S1: Pre-Commit Check (format + build)"`
- Also rename in the description line 3: `quality gates` → `checks` (to avoid overloading "gates")
- The purpose: disambiguate from the "Commit gate (per subtask)" row in the Task Verification table

## S2: Add hard gate before Step 6 (Commit) in subtask-loop skill

**Code:** N/A
**Tests:** N/A
**Docs:** Edit `.opencode/skills/subtask-loop/SKILL.md`

**Spec:**
Before the "Commit with message" line, insert:

```
**Hard gate — before committing, verify:**

- [ ] Exactly ONE subtask's worth of changes is being committed. If multiple subtasks have been completed since the last commit, STOP. Unstage all files. Commit each subtask individually, one at a time.
- [ ] The commit message uses a single subtask identifier: `feat(XXX-SN): ...`, not ranges like `S1-S6`.
```

Update "SN is the subtask identifier" → "SN is the **single** subtask identifier"

## S3: Add "Documentation-Only Subtasks" section to subtask-loop skill

**Code:** N/A
**Tests:** N/A
**Docs:** Edit `.opencode/skills/subtask-loop/SKILL.md`

**Spec:**
Insert after the "Cycle Steps" header and before "### 1. Implement" a new section:

```
## Documentation-Only Subtasks

When a subtask modifies only `.md` files (no `.fs` files), the following cycle steps are adapted:

| Step | Action |
|------|--------|
| 1. Implement | Write documentation |
| 2. Write Tests | **Skip** — no code to test |
| 3. Update Docs | The implementation IS the documentation; verify navigation links are updated |
| 4. Pre-Commit Check | **Skip** — no source files to format or build |
| 5. Code Quality Checks | **Skip** — no code to check |
| 6. Commit | **Follow exactly** — one commit per subtask, single SN identifier |
| 7. Mark Done | **Follow exactly** |

The absence of code changes **never** justifies batching multiple subtasks into a single commit.
```

## S4: Add "Multi-Subtask Discipline" section to subtask-loop skill

**Code:** N/A
**Tests:** N/A
**Docs:** Edit `.opencode/skills/subtask-loop/SKILL.md`

**Spec:**
Insert after the "Per-Subtask Execution Tracking" section:

```
## Multi-Subtask Discipline

When a task has multiple subtasks (S1, S2, S3, ...), execute them **strictly sequentially**:

1. Complete all cycle steps for S1 (Implement → Tests → Docs → Pre-Commit Check → Quality → Commit → Mark Done)
2. Only after S1 is committed, start S2
3. Never mark multiple subtasks `completed` in `todowrite` before committing each individually

A `todowrite` listing "S1: Implement [completed], S2: Implement [completed], S1: Write tests [completed]" indicates skipped commits — each subtask must be fully committed before the next begins.
```

## S5: Strengthen commit message format in git-workflow skill

**Code:** N/A
**Tests:** N/A
**Docs:** Edit `.opencode/skills/git-workflow/SKILL.md`

**Spec:**
Replace the commit message format section (lines 17-29) with:

```
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
```

## S6: Add doc-only task note to AGENTS.md

**Code:** N/A
**Tests:** N/A
**Docs:** Edit `AGENTS.md`

**Spec:**
Insert after line 43 ("Commit messages must be detailed enough...") in the Core Rules section:

```
- Documentation-only tasks (no `.fs` files changed) skip code-specific gates (tests, lint, format, build) but still follow all other workflow rules: one task per branch, one commit per subtask, code review
```

## S7: Disambiguate Task Verification table row in subtask-loop skill

**Code:** N/A
**Tests:** N/A
**Docs:** Edit `.opencode/skills/subtask-loop/SKILL.md`

**Spec:**
Change the first row of the table (line 136):

```
| Subtask completeness | (per subtask cycle) | Tests written, docs updated — verified at steps 2-3 above |
```

Was: `| Commit gate (per subtask) | subtask-loop | ...`

This removes the final instance of ambiguous "Commit gate" naming.
