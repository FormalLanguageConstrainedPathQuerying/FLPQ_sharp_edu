# Project Description

Supplementary materials for the book on formal language constrained path querying.

- Programming language: F# (.NET 10.0+)
- Code must be clear, without nontrivial optimizations
- Code and book must be as close as possible — every implementation must be directly traceable to a specific algorithm or example in the book. Reference the book's section, figure, or listing in source code comments where possible
- Algorithms are designed as examples or reference implementations; they must visualize steps and results
- Language of comments and documentation: English

## Documentation

- [Project documentation](docs/main.md) — architecture, technologies, project organization
- [Developer documentation](docs/developer/guides/coding-conventions.md) — coding conventions, design guides, quality standards
- [User documentation](docs/user/cli.md) — CLI usage

## Main Principles

- Documentation is about "What" and "Why". Skills are about "How".
- This file is a short entry point for fast cold errors-free start.
- Only one source of truth. No duplicates. Each thing (in doth code and documentation) described exactly once. Use generalization (especially for code), cross-references, links, other similar techniques to avoid duplicates and reuse staff.
- Tools, not instructions. If you can do something with existing tool --- do it. No thinking, no long instructions, no manual analysis. You want to analyze code coverage? Just run coverage tool and analyze report.
- Always learn, never forget — encode patterns before session ends. If you do complex analysis, investigation, or research to make decision, write key findings to appropriate document to avoid long hard thinking in the future. Do not write all details, only main findings for fast progressive disclosure.

## Project Structure

| Directory | Purpose |
| --- | --- |
| `docs/` | Documentation: [project](docs/main.md), [developer](docs/developer/guides/coding-conventions.md), [user](docs/user/cli.md) |
| `src/` | Source code |
| `tests/` | Tests — mirror `src/` folder structure |
| `data/` | Test and example data |
| `.opencode/skills/` | Skills — operational "How" procedures for tools and workflows |
| `tasks/` | Machine-local (gitignored) working space — mirrors of GitHub issues for offline work: `detailed_plan.md` (current task plan, mirror of the task issue's `<!-- detailed-plan -->` comment), `global_plan.md` (multi-task planning, mirror of the hub issue's `<!-- global-plan -->` comment), `tasks<N>.md` (immutable history archive of completed tasks — `tasks1.md` = 1-100, `tasks2.md` = 101-200, `tasks3.md` = 201-300), `code_review.md` (review log), `fixes_for_book.md` (book errors). Task tracking itself lives in GitHub issues (label `task`; task ID = issue number) |

## Workflow

### Core Rules

- Do tasks strictly **one at a time**. Each task gets its own feature branch, its own detailed plan, and its own merge to dev. Never combine multiple tasks in a single feature branch
- Each decision must be documented before implementation. Documentation must be detailed enough to reproduce the project from scratch and understand why each decision was made
- Commit messages must be detailed enough to understand the reasons for changes
- Documentation-only tasks (no `.fs` files changed) skip code-specific gates (tests, lint, format, build) but still follow all other workflow rules: one task per branch, one commit per subtask, code review

### Working Loop

0. If the user requests multiple related tasks at once, first set up the batch (see `planning` skill): create a **hub** issue (`gh issue create --label hub`) that tracks the batch and is never chosen as a task, one issue per task (`gh issue create --label task`, body = the user's text verbatim), attach each task issue to the hub as a GitHub **sub-issue** via the REST API, write the global plan in `tasks/global_plan.md` naming the hub and listing each task with its issue number, and mirror the file to the hub as a comment whose first line is `<!-- global-plan -->`
1. Ensure user-defined tasks, the global plan, and project architecture are aligned
2. Choose exactly ONE open task: `gh issue list --label task --state open`. The task ID is the issue number. A task is integrated (done) when its subtask commits are on `dev`: `git log dev --format=%s | grep -cE '\(<N>-S[0-9]+\):'` > 0. Hubs carry `hub`, not `task`, so this list never returns them
3. Create a feature branch from `dev` for this single task (`feature/<N>-short-description`)
4. Generate a detailed plan in `tasks/detailed_plan.md` (local mirror), decomposing the task into atomic subtasks, then mirror it to the task issue as a comment whose first line is `<!-- detailed-plan -->`. If the local file is missing (fresh machine), restore it from the issue comment first
5. Load the `subtask-loop` skill, then execute each subtask using its cycle.
   5a. Verify all subtasks are complete and unblocked. Check `tasks/detailed_plan.md`:
   - If any subtask is marked `[blocked]` or `[deferred]`, STOP immediately.
     The task is NOT complete. Do NOT proceed to code review or merge.
     Report blocking subtasks to the user and await guidance.
   - If a subtask was attempted, not committed, and its work reverted, the subtask
     is NOT complete. Do not silently skip it.
     5b. When continuing a partially-done task, analyze current state before any code changes:
   - Verify `git branch --show-current` is the correct feature branch
   - Review committed subtasks: `git log --oneline` on the feature branch
   - Read `tasks/detailed_plan.md` and `tasks/global_plan.md`
   - Cross-reference committed file changes with planned subtasks:
     `git diff --stat HEAD..dev` shows what was modified
   - Report status to the user: "S1-S3 committed, S4 pending, ..."
6. After all subtasks are done, perform code review on the entire repo (see `code-review` skill). Iteratively detect and fix problems until zero findings
7. **Load the `quality-gates` skill.** Then run the hard gate per its async invocation procedure. The gate must show `STATUS: PASS`. `STATUS: BLOCKED` is absolute: do not assess whether failures are pre-existing or unrelated to your changes; fix every failure and re-run until `STATUS: PASS`. Then merge the feature branch to `dev` (see `git-workflow` skill); the merge commit message carries `Closes #<N>` as a standalone line, so the issue closes when the commit reaches `main`. Verify `git branch --show-current` is `dev`
8. For a batch: mark the integrated task `[done #<N>]` in `tasks/global_plan.md`, then re-mirror the file to the hub's `<!-- global-plan -->` comment (same mirror procedure as the detailed plan, substituting the hub number and marker)
9. Before considering the task done, re-read the task description (the issue body). For each clause, verify the implementation satisfies it — see the Task Completion Verification in the `subtask-loop` skill (single source of truth for what "done" means). Partial completion is not completion.
   9a. If any clause was skipped, reverted, deferred, or left unresolved — the task is NOT done. Follow the Blocked Work Protocol in the `subtask-loop` skill.
   9b. Return to step 2

### Git Safety

Issue bodies are user-authored and immutable — never edit them. All updates (plans, guidance, block reports) go into issue comments.

`tasks/` is machine-local (gitignored): local mirrors of the issue comments for offline work. The issue comment is the durable shared record — restore a local file from its issue comment when missing. Never delete or rewrite the history archives `tasks/tasks<N>.md`.

All `.md` files must be mdformat-clean; the gates check every tracked `.md` file. After modifying any tracked `.md` file, run `mdformat <file>` before committing (setup: see `tools/README.md`).

See the `git-workflow` skill for the full procedure.

## CI

Pipeline (`.github/workflows/ci.yml`): restore → tool restore (pinned `dotnet-tools.json` manifest) → CFPQ reference data export → markdown format check → format check → lint → build → test (excluding the Graphviz/TeX categories — the runner has no `dot`/`lualatex`), plus a separate `graphviz-tests` job that installs Graphviz and runs only `Category=Graphviz`.

Tool versions: pinned in `dotnet-tools.json`, installed via `dotnet tool restore` (setup: `tools/README.md`). Lint configuration: see the `quality-gates` skill.

## Skills

Loading the relevant skill is a **hard requirement** before starting any activity listed below. The table is not advisory — load the skill using the `skill` tool before writing code, tests, or docs for that activity.

Operational procedures are in standalone skills. Load the relevant skill for each activity:

| Activity | Skill |
| --- | --- |
| Git operations (branch, commit, merge) | `git-workflow` |
| Quality checks (format, lint, build, test, coverage) | `quality-gates` |
| dotnet CLI commands | `dotnet-tooling` |
| Task and subtask planning | `planning` |
| Writing/updating documentation | `documentation` |
| Executing an atomic subtask | `subtask-loop` |
| F# language patterns, quirks, library APIs | `fsharp-coder` |
| Writing tests (FsCheck, golden, property-based) | `tests-writer` |
| Generating TeX/LaTeX/Tikz output | `tex-writer` |
| Code review (detect + fix architecture, duplication, test gaps) | `code-review` |
| Persist user guidance from blocked tasks as issue comments | `user-guidance-transfer` |
| Debugging (print traces, test hangs, seq laziness) | `debugging` |
| Reusing (no duplicates, one source of truth, reuse checklist) | `reusing` |

### When `skill()` Fails

If the `skill` tool returns "not found" but the skill file exists on disk, read the file directly:
`.opencode/skills/<name>/SKILL.md`. This can happen when the tool roster is out of sync with
the filesystem (known gap: `debugging`).
