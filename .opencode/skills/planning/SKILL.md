---
name: planning
description: Use when planning tasks: creating global plans across multiple tasks, decomposing a task into atomic subtasks, or authoring new task descriptions. Covers multi-task planning, detailed plan format, atomic subtask requirements, and task authoring guidelines.
---

# Planning

## Multi-task Planning

When the user asks to work on a **set of tasks** (several task IDs), do NOT jump directly into implementation. First create a high-level global plan in `tasks/global_plan.md`.

The global plan must:

- List all tasks to be done with their IDs and brief descriptions
- Identify dependencies between tasks (which must be done before which)
- Identify potential conflicts or overlapping changes (e.g., two tasks modifying the same file)
- Identify shared infrastructure (types, helpers, utilities) that multiple tasks need. Create shared modules to avoid duplication across tasks
- Identify existing reusable abstractions (types, helper functions, generators) that new tasks should use rather than reinvent
- For each task that involves rendering/visualization, note that rendering and algorithm logic must be in separate projects/modules from the start
- Propose an execution order that minimizes rework and avoids conflicts
- Align tasks with the project architecture

After the global plan is created, proceed with the normal working loop: one task at a time, feature branch per task, detailed plan in `tasks/detailed_plan.md` for each.

## Detailed Plan (Atomic Subtasks)

`tasks/detailed_plan.md` MUST decompose the task into atomic subtasks. Each subtask:

- Is small enough to complete in a single focused work session (typically 30–90 minutes)
- Produces a compilable, testable increment — no partial implementations left uncommitted
- Has a unique identifier (e.g., "S1", "S2") used in commit messages and plan tracking

If a task is ambiguous or underspecified **during planning** (conflicting requirements, unclear scope, missing constraints), do not guess. Ask the user for clarification, then transfer the guidance to the task per the `user-guidance-transfer` skill before proceeding with decomposition.

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

## Task Authoring Guidelines

When writing a new task for `tasks/tasks.md`, follow these rules:

- **Specify output format upfront**. If the task involves TeX or dot visualization, include the exact column layout, math mode conventions, and formatting rules in the task description
- **Keep tasks single-responsibility**. A task should do one thing. If it requires more than 5 sub-items or spans multiple unrelated concerns, split it into multiple tasks
- **Specify equivalence requirements**. For any new algorithm variant, explicitly state "must produce results identical to X" so equivalence tests are built from the start
- **Specify type genericity**. If a module must handle arbitrary symbol types, state it explicitly (e.g., "generic over terminal and nonterminal types")
- **Specify reuse expectations**. If the task builds on existing infrastructure (e.g., "reuse matrix from task 2", "reuse DFA from task 14"), name the dependencies. This prevents reinvention
