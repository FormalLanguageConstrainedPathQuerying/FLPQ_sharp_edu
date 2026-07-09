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
- States its **inputs** (dependencies on prior subtasks or existing code) and **outputs** (new types, functions, tests, docs)

### Granularity

If a subtask cannot be committed as a self-contained increment, it is too large — split it further.

### Bounded uncommitted work

Uncommitted work on a feature branch must never exceed one atomic subtask. If a session is interrupted, the loss is bounded to that single subtask.

## Task Authoring Guidelines

When writing a new task for `tasks/tasks.md`, follow these rules:

- **Specify output format upfront**. If the task involves TeX or dot visualization, include the exact column layout, math mode conventions, and formatting rules in the task description
- **Keep tasks single-responsibility**. A task should do one thing. If it requires more than 5 sub-items or spans multiple unrelated concerns, split it into multiple tasks
- **Specify equivalence requirements**. For any new algorithm variant, explicitly state "must produce results identical to X" so equivalence tests are built from the start
- **Specify type genericity**. If a module must handle arbitrary symbol types, state it explicitly (e.g., "generic over terminal and nonterminal types")
- **Specify reuse expectations**. If the task builds on existing infrastructure (e.g., "reuse matrix from task 2", "reuse DFA from task 14"), name the dependencies. This prevents reinvention
