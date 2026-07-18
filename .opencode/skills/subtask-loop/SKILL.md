---
name: subtask-loop
description: Use when executing an atomic subtask from the detailed plan: implement → test → document → pre-commit checks → quality checks → commit → mark done. Covers the full execution cycle, code quality checks, commit rules, and completion tracking.
---

# Subtask Execution Loop

Each atomic subtask from `tasks/detailed_plan.md` is executed as a self-contained cycle.

## Cycle Steps

Execute these steps in order. **Do not skip steps. Do not proceed past a step until it is verified complete.**

## Documentation-Only Subtasks

When a subtask modifies only `.md` files (no `.fs` files), the following cycle steps are adapted:

| Step | Action |
|------|--------|
| 1. Implement | Write documentation |
| 2. Write Tests | **Skip** — no code to test |
| 3. Update Docs | The implementation IS the documentation; verify navigation links are updated |
| 4. Pre-Commit Check | **Skip** — no source files to format or build |
| 5. Code Quality Checks | **Skip** — no code to check |
| 6. Commit | **Follow exactly** — one commit per subtask, single SN identifier |
| 7. Mark Done | **Follow exactly** |

The absence of code changes **never** justifies batching multiple subtasks into a single commit.

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

Update all task-related documentation per the `documentation` skill. The skill references `docs/developer/guides/documentation-conventions.md` for the complete mapping of source changes to required doc updates and the completeness verification criteria.

**Hard gate — this step is not complete until:**

- [ ] At least one documentation file was created or updated for this subtask
- [ ] Documentation completeness is verified per the `documentation` skill's procedure

### 4. Pre-Commit Check

First, run fantomas manually to format all files:

```bash
dotnet fantomas .
```

Stage any formatted files (`git add`). Then run the quality check tool:

```bash
python3 tools/quality_check.py
```

This runs format check + build without timeout. Read and deeply analyze `tmp/quality-check.txt`. If STATUS: BLOCKED — fix all problems and re-run.

**Hard gate — do not proceed to step 5 unless this passes.**

### 5. Code Quality Checks

- **Duplication check**: scan for accidental code duplication (same logic under different names, copy-pasted blocks). Consolidate if found
- **Genericity check**: verify new types use generic parameters (`'t`, `'nt`) where applicable and non-empty collections use `NonEmptyList`/`NonEmptySet`
- **Equivalence test check**: if the subtask is a variant of an existing algorithm, ensure a property-based equivalence test exists comparing it to the reference implementation
- **Separation check**: verify algorithm modules do not contain TeX/dot string generation or file I/O

### 6. Commit

**Hard gate — before committing, verify:**

- [ ] Exactly ONE subtask's worth of changes is being committed. If multiple subtasks have been completed since the last commit, STOP. Unstage all files. Commit each subtask individually, one at a time.
- [ ] The commit message uses a single subtask identifier: `feat(XXX-SN): ...`, not ranges like `S1-S6`.

Commit with message `feat(XXX-SN): description` where `XXX` is the task ID and `SN` is the **single** subtask identifier.

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

Never silently skip a subtask. If a subtask was attempted, reverted, and its planned changes were not committed, the subtask is incomplete. Do not mark it done. Do not proceed. Report it as blocked.

### Requirement Cross-Check

Before marking a subtask complete, re-read the task specification in `tasks/tasks.md`. Verify each clause against what was actually implemented:

- [ ] Every clause in the task description is traceable to code that was committed
- [ ] No requirement was silently skipped or deferred without user approval
- [ ] No code was reverted without resolution
- [ ] If ANY of the above fails, the subtask is blocked — follow the Blocked Work Protocol

## Per-Subtask Execution Tracking

For each subtask, use the `todowrite` tool to track cycle steps as separate items. **No subtask may be committed with any step still `pending`.**

Example for subtask S1:

```
- "S1: Implement" → in_progress → completed
- "S1: Write tests" → in_progress → completed
- "S1: Update documentation" → in_progress → completed
- "S1: Pre-Commit Check (format + build)" → in_progress → completed
- "S1: Quality checks" → in_progress → completed
- "S1: Commit" → in_progress → completed
```

## Multi-Subtask Discipline

When a task has multiple subtasks (S1, S2, S3, ...), execute them **strictly sequentially**:

1. Complete all cycle steps for S1 (Implement → Tests → Docs → Pre-Commit Check → Quality → Commit → Mark Done)
2. Only after S1 is committed, start S2
3. Never mark multiple subtasks `completed` in `todowrite` before committing each individually

A `todowrite` listing "S1: Implement [completed], S2: Implement [completed], S1: Write tests [completed]" indicates skipped commits — each subtask must be fully committed before the next begins.

## Blocked Work Protocol

If you encounter an algorithmic problem that you cannot resolve to 100% correctness, **STOP**. Do not commit. Do not merge. Do not comment out or weaken failing tests to make the suite green. Instead:

1. Stay on the feature branch
2. Report the problem concretely to the user:
   - Which tests fail and why
   - What algorithmic gap exists (e.g., "LR goto entries missing for nested nonterminal calls")
   - What you've tried and what remains unresolved
3. Ask the user for guidance: additional subtasks, algorithmic hints, descoping, or splitting the task
4. **Transfer user guidance to the task** per the `user-guidance-transfer` skill — append `**[USER GUIDANCE]**` annotation to the task in `tasks/tasks.md`
5. Append a `## Design Notes` section to `tasks/detailed_plan.md`. See the `planning` skill for the full template. Minimum required content:

   - **Correct Design**: algorithmic design as confirmed by the user — coordinate spaces, invariants, decomposition schema. Quote the user's design guidance verbatim where available.
   - **Blocked Subtasks**: which subtasks are blocked and why
   - **Root Causes**: why each failure occurs, with concrete examples. Every limitation MUST be traceable to a concrete input string, a concrete range/cell in the data structure, and a concrete execution path in the code. Never write vague descriptions.
   - **Approaches Tried**: what was attempted and why it didn't fully work
   - **Remaining Work**: concrete, actionable items (e.g., "Track origin final RSM state through BFS queue by adding a third field to the BFS node tuple") — not vague goals
   - **Skipped Tests**: list any tests skipped with `[<Fact(Skip="...")>]` and the reason

   Commit this summary so the plan serves as a persistent design record for future task refinement.

## Task Completion Verification

After ALL subtasks are committed, run the hard gate:

```bash
python3 tools/hard_gate.py
```

See the `quality-gates` skill for the full procedure. While the gate runs, `tmp/hard-gate.txt` shows `STATUS: IN_PROGRESS` — the gate has not finished yet. Wait for the process to exit, then check the **exit code** (`echo $?`). If non-zero — for ANY reason — STOP. Do not merge. Do not mark the task as done. Read the output file only to identify what to fix, not to decide whether the failure applies to you. Re-run until exit code 0, then proceed to merge.

## Marking Complete

Mark the task as completed in `tasks.md` — **only prepend `[done] ` to the existing task line. Never rewrite the task description.** The task text in `tasks.md` is user-authored and immutable.

The `[done]` tag means COMPLETE: every requirement met, every test passing, every edge case handled. Never mark a task as `[done]` with known failures or unresolved limitations.
