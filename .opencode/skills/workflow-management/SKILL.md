---
name: workflow-management
description: "Load first at the start of every session, before doing anything else. Drives the overall working loop for a task or set of tasks on dev: global plan, one task at a time, feature branch, detailed plan, subtask execution, code review, quality gate, merge to dev."
---

# Workflow Management

The top-level loop that orchestrates all other workflow skills. For a single
task, load this skill first; it points to the skills that handle each step.

## Core Rules

- Do tasks strictly **one at a time**. Each task gets its own feature branch
  (created from `dev`), its own detailed plan, and its own merge to `dev`.
  Never combine multiple tasks in a single feature branch.
- Each decision must be documented before implementation. Documentation must
  be detailed enough to reproduce the project from scratch and understand why
  each decision was made.
- Commit messages must be detailed enough to understand the reasons for
  changes.
- Documentation-only tasks (no `.fs` files changed) skip code-specific gates
  (tests, lint, format, build) but still follow all other workflow rules: one
  task per branch, one commit per subtask, code review.

## Working Loop

0. If the user requests multiple related tasks at once, set up the batch
   before any implementation (procedure: `planning` skill): create a **hub**
   issue (`gh issue create --label hub`) that tracks the batch and is never
   chosen as a task, one issue per task (`gh issue create --label task`, body
   = the user's text verbatim), attach each task issue to the hub as a GitHub
   **sub-issue** via the REST API, write the global plan in
   `tasks/global_plan.md` naming the hub and listing each task with its issue
   number, and mirror the file to the hub as a comment whose first line is
   `<!-- global-plan -->`.
1. Ensure user-defined tasks, the global plan, and project architecture are
   aligned.
2. Choose exactly ONE open task: `gh issue list --label task --state open`.
   The task ID is the issue number. A task is integrated (done) when its merge
   commit is on `dev`: `git log dev --format=%s | grep -cE '^[a-z]+\(<N>\):'` >
   0 (squash-merge convention — one `feat(N):` commit per task). Hubs carry
   `hub`, not `task`, so this list never returns them.
3. Create a feature branch from `dev` for this single task
   (`feature/<N>-short-description`; procedure: `git-workflow` skill).
4. Generate a detailed plan in `tasks/detailed_plan.md` (local mirror),
   decomposing the task into atomic subtasks (procedure: `planning` skill),
   then mirror it to the task issue as a comment whose first line is
   `<!-- detailed-plan -->`. If the local file is missing (fresh machine),
   restore it from the issue comment first.
5. Load the `subtask-loop` skill, then execute each subtask using its cycle.
   5a. Verify all subtasks are complete and unblocked. Check
   `tasks/detailed_plan.md`:
   - If any subtask is marked `[blocked]` or `[deferred]`, STOP immediately.
     The task is NOT complete. Do NOT proceed to code review or merge.
     Report blocking subtasks to the user and await guidance.
   - If a subtask was attempted, not committed, and its work reverted, the
     subtask is NOT complete. Do not silently skip it.
     5b. When continuing a partially-done task, analyze current state before any
     code changes:
   - Verify `git branch --show-current` is the correct feature branch
   - Review committed subtasks: `git log --oneline` on the feature branch
   - Read `tasks/detailed_plan.md` and `tasks/global_plan.md`
   - Cross-reference committed file changes with planned subtasks:
     `git diff --stat HEAD..dev` shows what was modified
   - Report status to the user: "S1-S3 committed, S4 pending, ..."
6. After all subtasks are done, perform code review on the entire repo (see
   the `code-review` skill). Iteratively detect and fix problems until zero
   findings.
7. **Load the `quality-gates` skill.** Then run the hard gate per its async
   invocation procedure. The gate must show `STATUS: PASS`. `STATUS: BLOCKED`
   is absolute: do not assess whether failures are pre-existing or unrelated
   to your changes; fix every failure and re-run until `STATUS: PASS`. Then
   merge the feature branch to `dev` (see the `git-workflow` skill); the merge
   commit message carries `Closes #<N>` as a standalone line, so the issue
   closes when the commit reaches `main`. Verify `git branch --show-current` is
   `dev`.
8. For a batch: mark the integrated task `[done #<N>]` in
   `tasks/global_plan.md`, then re-mirror the file to the hub's
   `<!-- global-plan -->` comment (same mirror procedure as the detailed plan,
   substituting the hub number and marker).
9. Before considering the task done, re-read the task description (the issue
   body). For each clause, verify the implementation satisfies it — see the
   Task Completion Verification in the `subtask-loop` skill (single source of
   truth for what "done" means). Partial completion is not completion.
   9a. If any clause was skipped, reverted, deferred, or left unresolved — the
   task is NOT done. Follow the Blocked Work Protocol in the `subtask-loop`
   skill.
   9b. Return to step 2.

## Git Safety

Issue bodies are user-authored and immutable — never edit them. All updates
(plans, guidance, block reports) go into issue comments.

`tasks/` is machine-local (gitignored): local mirrors of the issue comments for
offline work. The issue comment is the durable shared record — restore a local
file from its issue comment when missing. Never delete or rewrite the history
archives `tasks/tasks<N>.md`.

All `.md` files must be mdformat-clean; the gates check every tracked `.md`
file. After modifying any tracked `.md` file, run `mdformat <file>` before
committing (setup: see `tools/README.md`).
