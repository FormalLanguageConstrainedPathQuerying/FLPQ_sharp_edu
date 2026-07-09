---
name: subtask-loop
description: Use when executing an atomic subtask from the detailed plan: implement → test → document → quality gates → checks → commit → mark done. Covers the full execution cycle, code quality checks, commit rules, and completion tracking.
---

# Subtask Execution Loop

Each atomic subtask from `tasks/detailed_plan.md` is executed as a self-contained cycle.

## Cycle Steps

### 1. Implement

Write the subtask's outputs: types, functions, and code as specified in the detailed plan.

### 2. Write Tests

Write tests for the subtask's outputs. See the `tests-writer` skill for FsCheck API, golden test patterns, and generator best practices. For F#-specific FsCheck API quirks (Gen shadowing, naming, overloads), see the `fsharp-coder` skill.

Property-based tests:

- When a task specification states that certain constructs "can be used for property-based tests", implement `[<Property>]` tests with FsCheck-generated random inputs
- Do not substitute `[<Fact>]` tests enumerating hardcoded examples in place of property-based testing
- Property-based tests MUST use FsCheck `Arbitrary`/`Gen` types with `[<Property>]`. Never use `System.Random.Shared` in manual `for`-loops to drive randomized test inputs

Equivalence tests:

- Every new algorithm variant must include property-based equivalence tests proving it returns identical results to at least one existing reference implementation
- Example: "standard Valiant ≡ modified Valiant", "Belyanin ≡ Arroyuelo ≡ Kronecker+MS-BFS"

FsCheck generators:

- FsCheck generators for shared project types (matrices, graphs, grammars, regexes) must live in a common `Generators.fs` module
- Do not duplicate random generation logic across test projects

### 3. Update Documentation

Update all task-related docs including `fixes_for_book.md` and module docs (`docs/<module>.md`). See the `documentation` skill for details.

### 4. Commit Gate

Run commit gate (format + build). See `quality-gates` skill.

### 5. Code Quality Checks

- **Duplication check**: scan for accidental code duplication (same logic under different names, copy-pasted blocks). Consolidate if found
- **Genericity check**: verify new types use generic parameters (`'t`, `'nt`) where applicable and non-empty collections use `NonEmptyList`/`NonEmptySet`
- **Equivalence test check**: if the subtask is a variant of an existing algorithm, ensure a property-based equivalence test exists comparing it to the reference implementation
- **Separation check**: verify algorithm modules do not contain TeX/dot string generation or file I/O

### 6. Commit

Commit with message `feat(XXX-SN): description` where `XXX` is the task ID and `SN` is the subtask identifier.

**Before commit:**

- Verify `tasks.md` is not staged:
  ```bash
  git diff --cached --name-only | grep -q tasks.md
  ```
  If it matches, unstage it: `git reset HEAD tasks.md`

See the `git-workflow` skill for the full git workflow.

### 7. Mark Completed

Mark the subtask as completed in `tasks/detailed_plan.md`.

## Blocked Work Protocol

If you encounter an algorithmic problem that you cannot resolve to 100% correctness, **STOP**. Do not commit. Do not merge. Do not comment out or weaken failing tests to make the suite green. Instead:

1. Stay on the feature branch
2. Report the problem concretely to the user:
   - Which tests fail and why
   - What algorithmic gap exists (e.g., "LR goto entries missing for nested nonterminal calls")
   - What you've tried and what remains unresolved
3. Ask the user for guidance: additional subtasks, algorithmic hints, descoping, or splitting the task
4. Update `tasks/detailed_plan.md` with a section listing:
   - Which subtasks are blocked and why
   - What was tried and what failed
   - What specific help is needed from the user
   Commit this update so the plan serves as a persistent record of the blocking state

## Completion Verification

Before marking a task as done, confirm:

- ALL subtasks from the task description in `tasks.md` are implemented
- Task verification passed (lint + tests + coverage). See `quality-gates` skill.
- There are zero known algorithmic gaps, partial implementations, or skipped test cases
- Equivalence tests pass against the reference implementation if the task requires it

## Marking Complete

Mark the task as completed in `tasks.md` — **only prepend `[done] ` to the existing task line. Never rewrite the task description.** The task text in `tasks.md` is user-authored and immutable.

The `[done]` tag means COMPLETE: every requirement met, every test passing, every edge case handled. Never mark a task as `[done]` with known failures or unresolved limitations.
