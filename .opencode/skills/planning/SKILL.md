---
name: planning
description: "Use when planning tasks: creating global plans across multiple tasks, decomposing a task into atomic subtasks, or authoring new task descriptions. Covers multi-task planning, detailed plan format, atomic subtask requirements, and task authoring guidelines."
---

# Planning

## Multi-task Planning

When the user asks to work on a **set of related tasks**, do NOT jump directly into implementation. First set up the batch, then plan it:

1. Create a **hub** issue (`gh issue create --label hub`) for the batch and one issue per task (`gh issue create --label task`, body = the user's text verbatim), then attach each task issue to the hub as a GitHub **sub-issue** via the REST API (a link in the hub body is not a sub-issue):

   ```bash
   REPO=$(gh repo view --json nameWithOwner -q .nameWithOwner)
   CHILD_ID=$(gh api "repos/$REPO/issues/<N>" --jq .id)
   gh api -X POST "repos/$REPO/issues/<HUB>/sub_issues" -F sub_issue_id="$CHILD_ID"
   ```

   Verify the parent: `gh api "repos/$REPO/issues/<N>/parent" --jq .number` must print the hub number.

2. Create the high-level global plan in `tasks/global_plan.md` (local mirror), naming the hub and listing each task with its issue number, then mirror it to the hub as a comment whose first line is `<!-- global-plan -->`.

The global plan must:

- Name the **hub** issue it belongs to
- List all tasks to be done with their issue numbers (`#N`) and brief descriptions, with a Status column (`[done #N]` once the task is integrated)
- Identify dependencies between tasks (which must be done before which)
- Identify potential conflicts or overlapping changes (e.g., two tasks modifying the same file)
- Identify shared infrastructure (types, helpers, utilities) that multiple tasks need. Create shared modules to avoid duplication across tasks
- Identify existing reusable abstractions (types, helper functions, generators) that new tasks should use rather than reinvent
- **Reuse analysis**: load the `reusing` skill and run the reuse checklist against existing code and documentation before creating new material
- For each task that involves rendering/visualization, note that rendering and algorithm logic must be in separate projects/modules from the start
- Propose an execution order that minimizes rework and avoids conflicts
- Align tasks with the project architecture

After the global plan is created and mirrored to the hub, proceed with the normal working loop: one task at a time, feature branch per task, detailed plan in `tasks/detailed_plan.md` for each. After each task integrates, mark it `[done #N]` in `tasks/global_plan.md` and re-mirror the file to the hub's `<!-- global-plan -->` comment. The local file is the single source of truth while working; the hub comment is a pure mirror (see the `subtask-loop` skill for the sync command).

## Detailed Plan (Atomic Subtasks)

**Prerequisite — open task issue:** before writing the detailed plan, verify the task is an open `task`-labeled issue (`gh issue view <N>`). If it is not (e.g. the task came from a chat request), create the issue first (`gh issue create --label task`, body = the user's text verbatim) — never plan a task that is not an issue.

**Verbatim description:** `tasks/detailed_plan.md` MUST begin with a `## Task description (verbatim)` section quoting the task issue body verbatim (`gh issue view <N> --json body -q .body`). The issue body is user-authored and immutable — it is the durable record; the local plan file is a machine-local mirror (see the AGENTS.md Git Safety section).

`tasks/detailed_plan.md` MUST decompose the task into atomic subtasks. Each subtask:

- Is small enough to complete in a single focused work session (typically 30–90 minutes)
- Produces a compilable, testable increment — no partial implementations left uncommitted
- Has a unique identifier (e.g., "S1", "S2") used in commit messages and plan tracking

If a task is ambiguous or underspecified **during planning** (conflicting requirements, unclear scope, missing constraints), do not guess. Ask the user for clarification, then transfer the guidance to the task per the `user-guidance-transfer` skill before proceeding with decomposition.

Before writing the detailed plan, load the `reusing` skill and run the reuse checklist: search existing documentation and code for functions, types, and doc sections that can be reused or generalized. Record what is reused in each subtask's **Code** section.

### Subtask Format

Each subtask in `tasks/detailed_plan.md` MUST include these four sections. **A subtask with missing Code, Tests, or Docs sections is incomplete and must not be executed.**

```
### SN: <title>

**Code:** <files to create or modify, types and functions to add>
**Tests:** <test files to create or modify, specific test approaches:
          golden, property-based, [<Fact>], etc.>
**Docs:** <doc files to create or update. Use the mapping table in
         `docs/developer/guides/documentation-conventions.md` to
         determine which docs are affected.>

**Spec:**
- <detailed implementation specification>
```

Example:

```
### S1: Add SPPF DOT visualization

**Code:** New file `src/FLPQ.Printers/SppfDot.fs` with `SppfDot.toDot` function
**Tests:** New `SppfDotTests` section in an existing Printers test file or new
          test file; golden test comparing SPPF DOT output against reference
**Docs:** New `docs/developer/sppf-dot.md`; update `docs/developer/FLPQ.Printers.md`;
        update `docs/main.md`

**Spec:**
- Terminal/nonterminal nodes: shape=oval...
```

### Documentation Mapping

For the full mapping of source changes to required doc actions, see [`docs/developer/guides/documentation-conventions.md`](/docs/developer/guides/documentation-conventions.md). This table is the single source of truth — use it when writing the **Docs** section of each subtask.

### Granularity

If a subtask cannot be committed as a self-contained increment, it is too large — split it further.

### Bounded uncommitted work

Uncommitted work on a feature branch must never exceed one atomic subtask. If a session is interrupted, the loss is bounded to that single subtask.

## Post-Implementation Design Notes

When a task hits algorithmic limitations — partially completed, with skipped tests or remaining work — append a `## Design Notes` section to `tasks/detailed_plan.md`. This serves as persistent design knowledge for future task refinement. The section must be structured as follows:

```
## Design Notes (discovered during implementation)

### <Topic Title>

Design rationale, coordinate spaces, invariants — as confirmed by the user.

### <Failure Topic Title>

- Root causes with concrete examples (e.g., "for input `aa`, range (5,0)→(7,2) has no entries because...")
- What was attempted and why it didn't fully work
- Remaining work: concrete, actionable items
- Skipped tests: list `[<Fact(Skip="...")>]` and the reason
```

Requirements:

- Every algorithmic limitation MUST be traceable to a concrete input, a concrete range/cell in the data structure, and a concrete execution path in the code
- Never write vague descriptions like "PIntermediate entries are missing" — specify which range, which RSM state coordinates, and which reduction path should produce them
- Remaining work items must be actionable (e.g., "Track origin final RSM state through BFS queue by adding a field to the BFS node tuple") — not vague goals
- If the user provided design guidance (e.g., decomposition schema, coordinate system), record it verbatim in the `### <Topic>` section as the authoritative reference

## Task Completeness Verification

See the `subtask-loop` skill — it is the single source of truth for verifying task completion before the task is closed (merge commit carries `Closes #<N>`).

## Task Authoring Guidelines

When creating a new task issue (`gh issue create`, label `task`), follow these rules (they apply to the issue body):

- **Specify output format upfront**. If the task involves TeX or dot visualization, include the exact column layout, math mode conventions, and formatting rules in the task description
- **Keep tasks single-responsibility**. A task should do one thing. If it requires more than 5 sub-items or spans multiple unrelated concerns, split it into multiple tasks
- **Specify equivalence requirements**. For any new algorithm variant, explicitly state "must produce results identical to X" so equivalence tests are built from the start
- **Specify type genericity**. If a module must handle arbitrary symbol types, state it explicitly (e.g., "generic over terminal and nonterminal types")
- **Specify reuse expectations**. If the task builds on existing infrastructure (e.g., "reuse matrix from task 2", "reuse DFA from task 14"), name the dependencies. This prevents reinvention
