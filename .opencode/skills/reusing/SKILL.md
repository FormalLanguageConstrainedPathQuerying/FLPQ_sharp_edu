---
name: reusing
description: Use when planning or implementing any new feature, function, or documentation. Enforces no-duplicates and one-source-of-truth principles for code and docs. Covers the reuse checklist, search procedures, and integration with planning.
---

# Reusing

Before adding anything new — a function, a type, a doc section, a skill entry — check whether existing material can be reused or generalized. This prevents duplication and preserves a single source of truth.

See `docs/developer/guides/reusing.md` for the principles (what and why).

## Reuse Checklist

Run these five questions **before** writing new code or documentation. Answer each by searching, not guessing.

1. **Can I reuse existing stuff?** — Is there already a function, type, doc section, or skill that covers this?
2. **Can I reference existing docs?** — Instead of duplicating information, can I link to the canonical source?
3. **Can I call an existing function?** — Does a helper already exist that I can compose with?
4. **Can I generalize existing stuff?** — Can an existing function be made more generic (more type parameters, fewer constraints) to cover this case?
5. **Can I extract a shared helper?** — If two places need similar logic, can I factor it into one reusable function?

## Procedure

### 1. Search documentation for existing structure

Documentation describes code structure and serves as a navigation map. Use it before searching source files.

- Read `docs/main.md` to find the relevant project hub or guide
- Read the hub doc (e.g., `docs/developer/FLPQ.Languages.md`) to see what modules already exist
- Read the relevant guide (e.g., `docs/developer/guides/coding-conventions.md`) for conventions and patterns
- Use keywords from the task description to search: `grep -r "<keyword>" docs/`

### 2. Search code for existing functions and types

- Use `Grep` with function names, type names, or descriptive keywords from the task
- Use `Glob` to find files by pattern (e.g., `src/**/*<keyword>*.fs`)
- Check shared modules: `common.py`, `Generators.fs`, utility modules
- Check existing skills for procedural patterns that can be reused

### 3. Evaluate findings against the checklist

For each candidate found:

- **Exact match** (Q1): use it directly, no new code needed
- **Near match** (Q4): generalize the existing function to cover both cases; update callers
- **Partial overlap** (Q5): extract shared logic into a helper; refactor both call sites
- **Different domain** (Q3): compose from existing primitives rather than writing new logic

### 4. Apply to documentation

- If information already exists in a canonical doc, reference it instead of duplicating
- Skills describe *how*; docs describe *what* and *why*. Never put procedural instructions in docs or declarative content in skills
- When updating multiple files, ensure only one file owns each piece of information; others reference it

### 5. Record decisions in the plan

In `tasks/detailed_plan.md`, note what was reused and what was created new. This serves as a trace for future reuse checks.

## Integration with Planning

### Global planning (`tasks/global_plan.md`)

When creating a global plan across multiple tasks:

1. Load this skill before writing the plan
2. For each task, run the reuse checklist against existing code and docs
3. Identify shared infrastructure that multiple tasks need — create it once as a shared module
4. Identify existing abstractions that tasks should use rather than reinvent
5. Record reuse decisions in the global plan

### Detailed planning (`tasks/detailed_plan.md`)

When decomposing a task into subtasks:

1. Load this skill before writing the detailed plan
2. For each subtask, run the reuse checklist
3. In the **Code** section of each subtask, note what existing functions/types will be reused
4. If generalization is needed (Q4), create a dedicated subtask for it before the consuming subtasks
5. If a shared helper should be extracted (Q5), create a dedicated subtask for it

## When to Load This Skill

- During any planning activity (global or detailed)
- Before implementing a new function, type, or module
- Before writing documentation that might duplicate existing content
- When code review finds duplication or missing cross-references
