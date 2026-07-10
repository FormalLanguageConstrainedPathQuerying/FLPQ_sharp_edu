---
name: subtask-loop
description: Use when executing an atomic subtask from the detailed plan: implement → test → document → quality gates → checks → commit → mark done. Covers the full execution cycle, code quality checks, commit rules, and completion tracking.
---

# Subtask Execution Loop

Each atomic subtask from `tasks/detailed_plan.md` is executed as a self-contained cycle.

## Cycle Steps

Execute these steps in order. **Do not skip steps. Do not proceed past a step until it is verified complete.**

### 1. Implement

Write the subtask's outputs: types, functions, and code as specified in the detailed plan.

### 2. Write Tests

Write tests for the subtask's outputs. See the `tests-writer` skill for FsCheck API, golden test patterns, and generator best practices. For F#-specific FsCheck API quirks (Gen shadowing, naming, overloads), see the `fsharp-coder` skill.

**Hard gate — this step is not complete until:**

- [ ] At least one test file was created or an existing test file was modified for this subtask
- [ ] The affected test project builds and all tests pass (run `dotnet test` on the affected project)

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

Update all task-related docs including `fixes_for_book.md` and module docs (`docs/<module>.md`). See the `documentation` skill for content requirements.

**Hard gate — this step is not complete until:**

- [ ] At least one documentation file was created or updated for this subtask
- [ ] For new modules: a corresponding `docs/developer/<module>.md` file exists
- [ ] Modified public APIs are reflected in their existing module docs
- [ ] New CLI features are reflected in `docs/user/cli.md`
- [ ] New files are listed in `docs/developer/FLPQ.<Project>.md` and `docs/project/architecture.md`
- [ ] New doc pages are linked from `docs/main.md`
- [ ] If a book error was found, it is recorded in `tasks/fixes_for_book.md`

### 4. Commit Gate

Run commit gate (format + build). See `quality-gates` skill for the exact commands.

**Hard gate — do not proceed to step 5 unless this passes.**

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

If at any point in steps 1–7 you hit an unresolvable problem that prevents 100% completion, **STOP the cycle immediately** and follow the Blocked Work Protocol below. Do NOT attempt to "complete" the subtask with partial results, reverted work, or known limitations. Do NOT proceed to the next subtask.

## Subtask Outcome

A subtask has exactly two valid outcomes:

- **Resolved**: implemented, tested, docs updated, committed. Record the commit hash in `tasks/detailed_plan.md`.
- **Blocked**: an algorithmic or design problem prevents 100% completion. Do NOT commit partial work. Do NOT proceed to the next subtask. Follow the Blocked Work Protocol.

There is no third state. "Reverted and left as a known limitation" is not a valid outcome — it means the subtask is blocked. Report it.

## Per-Subtask Execution Tracking

For each subtask, use the `todowrite` tool to track cycle steps as separate items. **No subtask may be committed with any step still `pending`.**

Example for subtask S1:

```
- "S1: Implement" → in_progress → completed
- "S1: Write tests" → in_progress → completed
- "S1: Update documentation" → in_progress → completed
- "S1: Commit gate (format + build)" → in_progress → completed
- "S1: Quality checks" → in_progress → completed
- "S1: Commit" → in_progress → completed
```

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

## Task Completion Verification

After ALL subtasks are committed, run these gates in order. Each references the `quality-gates` skill for exact commands — **do not duplicate command recipes here.**

| Gate | Owned by | Description |
|------|----------|-------------|
| Commit gate (per subtask) | `subtask-loop` | Tests written, docs updated — verified at steps 2-3 above |
| Lint gate | `quality-gates` | `dotnet-fsharplint lint` — 0 warnings in modified projects |
| Test gate | `quality-gates` | `dotnet dotnet-coverage collect dotnet test` — 0 failures, 0 skipped |
| Coverage gate | `quality-gates` | Parse `tmp/coverage.cobertura`, verify FLPQ source ≥ 80% |
| Code review | `code-review` | Zero findings across entire repo |

**Hard rule: a task is NOT `[done]` until every gate in this table passes.**

## Marking Complete

Mark the task as completed in `tasks.md` — **only prepend `[done] ` to the existing task line. Never rewrite the task description.** The task text in `tasks.md` is user-authored and immutable.

The `[done]` tag means COMPLETE: every requirement met, every test passing, every edge case handled. Never mark a task as `[done]` with known failures or unresolved limitations.
