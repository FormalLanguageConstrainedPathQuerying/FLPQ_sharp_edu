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

* Documentation is about "What" and "Why". Skills are about "How".
* This file is a short entry point for fast cold errors-free start.
* Only one source of truth.
* No duplicates.
* Tools, not instructions.
* Always learn, never forget — encode patterns before session ends

## Project Structure

| Directory | Purpose |
|-----------|---------|
| `docs/` | Documentation: [project](docs/main.md), [developer](docs/developer/guides/coding-conventions.md), [user](docs/user/cli.md) |
| `src/` | Source code |
| `tests/` | Tests — mirror `src/` folder structure |
| `data/` | Test and example data |
| `.opencode/skills/` | Skills — operational "How" procedures for tools and workflows |
| `tasks/` | Task planning: `tasks.md` (user tasks), `detailed_plan.md` (current task plan), `global_plan.md` (multi-task planning), `fixes_for_book.md` (book errors) |

## Workflow

### Core Rules

- Do tasks strictly **one at a time**. Each task gets its own feature branch, its own detailed plan, and its own merge to dev. Never combine multiple tasks in a single feature branch
- Each decision must be documented before implementation. Documentation must be detailed enough to reproduce the project from scratch and understand why each decision was made
- Commit messages must be detailed enough to understand the reasons for changes

### Working Loop

0. If the user requests multiple tasks at once, first create a global plan in `tasks/global_plan.md` (see `planning` skill) before proceeding
1. Ensure user-defined tasks, the global plan, and project architecture are aligned
2. Choose exactly ONE task that is not yet done
3. Create a feature branch from `dev` for this single task
4. Generate a detailed plan in `tasks/detailed_plan.md`, decomposing the task into atomic subtasks
5. Execute each subtask using the `subtask-loop` skill
6. After all subtasks are done, perform code review on the entire repo (see `code-review` skill). Iteratively detect and fix problems until zero findings
7. Merge the feature branch to `dev` (see `git-workflow` skill). Verify `git branch --show-current` is `dev`
8. Mark the task `[done]` in `tasks.md`
9. Return to step 1

### Git Safety

Never revert `tasks/tasks.md` or `tasks/fixes_for_book.md` — the user may have added uncommitted content interactively. The working-tree version is authoritative.

See the `git-workflow` skill for the full procedure.

## CI

Pipeline (`.github/workflows/ci.yml`): restore → install tools → format check → lint → build → test.

Lint configuration: see the `quality-gates` skill.

## Skills

Operational procedures are in standalone skills. Load the relevant skill for each activity:

| Activity | Skill |
|----------|-------|
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
