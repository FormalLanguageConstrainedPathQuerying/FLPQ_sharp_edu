# Task 266: Fix task logging — restore lost entries, harden workflow

## Task description (verbatim)

Fix task logging: restore lost task entries and harden the workflow so that every task is reliably logged in `tasks/tasks.md` with no losses.
1. Restore tasks 1-100 to `tasks/tasks1.md` verbatim from `ac729de~1:tasks/tasks.md` (the version before `ac729de` removed them and replaced them with the header note). Add a one-line provenance note at the top of the file.
2. Restore entries 263 and 264 in `tasks/tasks.md` verbatim, tagged `[done]`, from the "Task description (verbatim)" sections of `b23a09a:tasks/detailed_plan.md` and `58c4f40:tasks/detailed_plan.md`. Insert between 262 and 265.
3. Harden workflow instructions so task entries cannot be lost again: (a) AGENTS.md working loop — new gate: before creating a feature branch, verify the chosen task has an entry in `tasks/tasks.md`; if not, add it verbatim and commit on `dev` (`docs(tasks): add task NNN`); step 8 — before marking `[done]`, verify the entry exists, restore verbatim from `detailed_plan.md` if missing. (b) git-workflow skill — pre-merge check: grep the task number in `tasks/tasks.md`; if missing, STOP and restore verbatim from `detailed_plan.md`; clarify that task-log commits happen on `dev`, never on a feature branch. (c) planning skill — detailed plan MUST begin with a `## Task description (verbatim)` section quoting the `tasks.md` entry; verify/create the entry before decomposition.
4. Delete untracked leftovers: `.opencode/plans/ll-lr-tikz-stack-viz.md`, `data/example_input_a_a_a_a.txt`.

## Context and analysis

### Loss inventory (verified against git history)

| Item | Evidence | Recovery source |
|------|----------|-----------------|
| Tasks 1-100 | Removed by `ac729de` ("docs: mark task 109 as done", 2026-07-05): 599 lines deleted, replaced with header note "First part of tasks located in `tasks1.md`"; `tasks1.md` never existed on disk or in git history | `ac729de~1:tasks/tasks.md` lines 6-560 (entry 1 at line 6, entry 100 ends before line 561 where 101 begins). Numbers 52-55 were already absent in that version — skipped, not lost |
| Tasks 263, 264 | Executed and merged (`b23a09a feat(263)`, `58c4f40 feat(264)`) but never present in any committed or working-tree version of `tasks/tasks.md` (last commit touching it before this task: `45d5188`, task 262) | `b23a09a:tasks/detailed_plan.md` and `58c4f40:tasks/detailed_plan.md`, "Task description (verbatim)" sections |
| Task 265 | Entry existed only as uncommitted working-tree change | Committed durably on dev in `a745d97` before this branch was created |

### Root causes

1. No entry-before-work gate: a task arriving as a chat request could start without a `tasks.md` entry.
2. Working-tree-only lifetime: "never commit `tasks.md` from a feature branch" meant entries added mid-task stayed uncommitted until `[done]` marking; any reset of `tasks.md` wiped them.
3. No existence check at `[done]` time or at merge time.

### Design decisions

- **Durability rule (user decision):** task entries are committed on `dev` at creation time (`docs(tasks): add task NNN`) before the feature branch exists. The existing rule "never commit `tasks.md` from a feature branch" stays — task-log commits simply never happen on feature branches; they happen on `dev` (entry at start, `[done]` at end).
- **Recovery target for 1-100:** `tasks/tasks1.md` — the header note in `tasks.md` already references that file; restoring it there matches the original intent and keeps `tasks.md` unchanged except for 263/264.
- **Verbatim principle:** restored task text is copied byte-for-byte from the recovery source; only the `[done]` status tag is added to 263/264 (permitted by the strict rule in `tasks.md`).
- **Recovery source of truth going forward:** every `detailed_plan.md` MUST begin with "## Task description (verbatim)" — this is what made 263/264 recoverable; it becomes mandatory.

### Reuse checklist

- No new code. All changes are `.md` files: `tasks/tasks1.md` (new), `tasks/tasks.md`, `AGENTS.md`, `.opencode/skills/git-workflow/SKILL.md`, `.opencode/skills/planning/SKILL.md`.
- Existing structures reused: the "Task description (verbatim)" convention already used in recent detailed plans; the pre-merge checks section of the git-workflow skill; the working loop of AGENTS.md.

## Subtasks

### S1: Restore tasks 1-100 to `tasks/tasks1.md` — [done, c3e9f79]

**Code:** none (docs-only).
**Tests:** skip (docs-only).
**Docs:** new file `tasks/tasks1.md`.

**Spec:**
- Extract entries 1-100 verbatim from `ac729de~1:tasks/tasks.md` (lines 6-560, i.e. from the `1.` entry up to and including the full `100.` entry, stopping before `101.`).
- Prepend a one-line provenance note: `* Recovered verbatim from commit ac729de~1 (tasks.md before ac729de split the file); numbers 52-55 were already absent at that point.`
- Verify: every number 1-100 except 52-55 appears exactly once as an entry head; entry count = 96.

### S2: Restore entries 263 and 264 in `tasks/tasks.md` — [done, 13c74d9 on dev]

Note: committed directly on `dev` (not on this branch) per the task-log durability rule — `tasks.md` is never committed from a feature branch.

**Code:** none (docs-only).
**Tests:** skip (docs-only).
**Docs:** `tasks/tasks.md`.

**Spec:**
- Copy the "Task description (verbatim)" section content from `b23a09a:tasks/detailed_plan.md` (task 263) and `58c4f40:tasks/detailed_plan.md` (task 264).
- Insert as entries `263. [done] ...` and `264. [done] ...` between the existing `262.` entry and the `265.` entry, matching the file's indentation style (one leading space before the number, sub-items indented).
- Task text is verbatim; only the `[done]` tag is added (permitted additive change).

### S3: Harden workflow instructions — [done]

**Code:** none (docs-only).
**Tests:** skip (docs-only).
**Docs:** `AGENTS.md`, `.opencode/skills/git-workflow/SKILL.md`, `.opencode/skills/planning/SKILL.md`.

**Spec:**
- **AGENTS.md working loop:**
  - New step after "Choose exactly ONE task that is not yet done": verify the chosen task has an entry in `tasks/tasks.md`; if not (e.g. task came from a chat request), add the entry verbatim and commit on `dev` (`docs(tasks): add task NNN`) before creating the branch. No work starts without a logged entry.
  - Step 8 ("Mark the task `[done]`"): before marking, verify the entry exists (grep by number); if missing, restore it verbatim from the "Task description (verbatim)" section of `tasks/detailed_plan.md` first.
- **git-workflow skill:**
  - Pre-merge checks: add a check that the task entry exists in `tasks/tasks.md` (`grep -qE '^[[:space:]]*NNN\.' tasks/tasks.md`); if missing, STOP and restore verbatim from `tasks/detailed_plan.md` before merging.
  - Clarify in the pre-commit checklist / rules: task-log commits (adding an entry, marking `[done]`) happen on `dev`, never on a feature branch.
- **planning skill:**
  - Detailed Plan section: the plan MUST begin with a `## Task description (verbatim)` section quoting the `tasks.md` entry verbatim.
  - Before decomposition: verify the task has an entry in `tasks/tasks.md`; if not, create it first (per AGENTS.md working loop).

### S4: Delete untracked leftovers — [done, no commit — untracked file deletion]

Both files were untracked (never in git), so their deletion produces no git change; this plan entry records the action.

**Code:** none.
**Tests:** skip.
**Docs:** none.

**Spec:**
- Delete `.opencode/plans/ll-lr-tikz-stack-viz.md` (superseded by the committed task 262 detailed plan) and `data/example_input_a_a_a_a.txt` (unreferenced test leftover). Both are untracked and referenced nowhere in the repo (verified by grep).
- Remove the now-empty `.opencode/plans/` directory.
