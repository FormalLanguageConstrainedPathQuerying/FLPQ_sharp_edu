# AGENTS.md

This file is a short TOC of the agent workflow. The project itself is
described in `README.md`, which is the entrypoint for navigating the
repository.

## Start here

1. Load the `workflow-management` skill
   (`.opencode/skills/workflow-management/SKILL.md`) first, before doing
   anything else.
2. Read `README.md` for project navigation.
3. Before exploring code, load the `project-navigation` skill
   (`.opencode/skills/project-navigation/SKILL.md`): navigate via
   `docs/main.md` and `docs/project/architecture.md`, not by scanning the
   whole repository.

## Main Principles

- This file is a short entry point for fast cold errors-free start.
- Only one source of truth. No duplicates. Each thing (in both code and
  documentation) described exactly once. Use generalization (especially for
  code), cross-references, links, other similar techniques to avoid
  duplicates and reuse staff.
- Source-of-truth hierarchy: code, scripts, CI configs > docs. If a fact can
  be extracted from code or scripts (e.g., CI configs), it is not duplicated
  in docs. Docs hold only what cannot be unambiguously reconstructed from
  code: design decisions, non-trivial constraints. Skills stay thin pointers
  to docs, code, or CI — they never re-describe them.
- Tools, not instructions. If you can do something with existing tool --- do
  it. No thinking, no long instructions, no manual analysis. You want to
  analyze code coverage? Just run coverage tool and analyze report.
- Never stay silent about problems. Any problem discovered or detected must
  be addressed. If it is in current scope or the fix is easy (e.g. a
  misprint), fix it — even if pre-existing. If it is significantly out of
  scope, report it to the user and propose creating an issue.
- Always learn, never forget — encode patterns before session ends. If you do
  complex analysis, investigation, or research to make decision, write key
  findings to appropriate document to avoid long hard thinking in the future.
  Do not write all details, only main findings for fast progressive
  disclosure.

## Skills

Loading the relevant skill is a **hard requirement** before starting any
activity listed below. The table is not advisory — load the skill using the
`skill` tool before writing code, tests, or docs for that activity.

### Domain

| Skill | When to use |
| --- | --- |
| `.opencode/skills/project-navigation` | Starting navigation before exploring code |
| `.opencode/skills/fsharp-coder` | Writing F# code: language patterns, quirks, library APIs |
| `.opencode/skills/tests-writer` | Writing tests (FsCheck, golden, property-based) |
| `.opencode/skills/tex-writer` | Generating TeX/LaTeX/Tikz output |
| `.opencode/skills/dotnet-tooling` | dotnet CLI commands |
| `.opencode/skills/debugging` | Debugging (print traces, test hangs, seq laziness) |

### Workflow / process

| Skill | When to use |
| --- | --- |
| `.opencode/skills/workflow-management` | Start of every session — drives the overall task loop |
| `.opencode/skills/planning` | Global plans, atomic subtasks, task authoring |
| `.opencode/skills/subtask-loop` | Executing one atomic subtask |
| `.opencode/skills/git-workflow` | Branching, commits, merging |
| `.opencode/skills/quality-gates` | Quality checks (format, lint, build, test, coverage) — the pre-merge gate that must pass |
| `.opencode/skills/code-review` | Whole-repo review before merge |
| `.opencode/skills/documentation` | Writing/updating documentation |
| `.opencode/skills/reusing` | Finding existing code/docs to reuse, not duplicate |
| `.opencode/skills/user-guidance-transfer` | Persist user guidance from blocked tasks as issue comments |

### When `skill()` Fails

If the `skill` tool returns "not found" but the skill file exists on disk,
read the file directly: `.opencode/skills/<name>/SKILL.md`. This can happen
when the tool roster is out of sync with the filesystem (known gap:
`debugging`).
