---
name: code-review
description: Use when performing code review: detect and fix architecture problems, code duplication, signature inconsistencies, naming issues, stubbed tests, test gaps, and genericity violations across all projects. Iterative fix loop until zero findings.
---

# Code Review

Performed at task completion — after all subtasks are committed and quality gates pass, before marking `[done]`. Covers the **entire repo** (`src/` and `tests/`).

## Core Loop

```
Review → Detect problems → Fix all → Commit fixes → Repeat until zero findings
```

**Every detected problem must be fixed.** No exceptions. No deferred fixes. No reporting without fixing.

## Review Categories

### 1. Architecture

- Is the architecture clear and consistent?
- Is logic implemented in the right place?
  - No helper functions (e.g., in tests) that actually fix problems in main logic implementation
  - No logic that can be generalized and moved up
- Are algorithm modules free of TeX/dot string generation and file I/O? (Separation: rendering lives in `FLPQ.Printers`, algorithms live in domain projects)

### 2. Code-Level Problems

- **Duplicates**: identical or near-identical code blocks, functions, or files. Every copy must be consolidated into exactly one shared location
- **Signature inconsistencies**: same concept expressed with different signatures across modules
- **Unclear structure**: modules, types, or functions that obscure intent rather than clarify it
- **Naming**: names that mislead, shadow types, break conventions, or fail to distinguish intent

**Convention checks** (from `docs/developer/guides/coding-conventions.md`):

| Rule | Expected |
|------|----------|
| Types, modules, record fields, union case fields | PascalCase |
| Functions and values | camelCase |
| Immutability-first | `let` bindings; mutable only when algorithm explicitly requires it |
| Types + modules at the same level | `type Foo = ...` then `module Foo = ...` |
| Public API documentation | XML doc comments (`///`) on all public members |
| Maximal genericity | Generic over `'t`, `'nt` where possible; never hardcoded to `string` |
| Non-empty collections by type | `NonEmptyList<'t>` / `NonEmptySet<'t>` for collections that must not be empty |
| Book traceability | Every implementation directly traceable to a specific algorithm, listing, or example in the book; comments reference section/figure/listing |

### 3. Tests

- **No stubbed tests**: no `Assert(true)`, no commented-out checks, no empty test bodies (`()`), no tests without assertions
- **Appropriate property checks**: tests verify all relevant properties of the result, not just a single dimension
- **Equivalence tests**: every algorithm variant has property-based equivalence tests against a reference implementation
- **FsCheck generators**: shared generators live in `FLPQ.TestUtilities/Generators.fs`, not duplicated across test projects
- **Property test correctness**: `[<Property>]` tests must use FsCheck-generated inputs; tests iterating over hardcoded data with `[<Property>]` are mislabeled and must be converted to `[<Fact>]`
- **Golden tests**: verify that golden files capture correct output (not buggy first-run output)
- **Test coverage gaps**: every new public module or function in `src/` must have at least one test targeting it. For each new `.fs` file in `src/`, verify at least one corresponding test file or test function exists in `tests/`

### 4. Documentation

Verify doc completeness per `docs/developer/guides/documentation-conventions.md` — module doc entries, hub doc updates, architecture doc updates, CLI user doc updates, cross-references, and navigation links.

### 5. Genericity and Type Safety

- Types use generic parameters (`'t`, `'nt`) where applicable — no hardcoded `string` in algorithm implementations
- Non-empty collections use `NonEmptyList`/`NonEmptySet` at the type level, not runtime checks
- Unit tests may instantiate at `string` for readability, but the implementation must never depend on it

### 6. Clarity and Book Alignment

- Code is clear, without nontrivial optimizations
- Every implementation is directly traceable to a specific algorithm or example in the book
- Comments are in English and reference the book's section, figure, or listing

## Fix Protocol

1. **Detect** all problems in one review pass across the entire repo
2. **Fix** every problem. No partial fixes. No "fix later" notes
3. **Commit** fixes with descriptive message: `fix(review): description of what was fixed`
4. **Re-review** the entire repo from scratch
5. **Repeat** until a review pass finds zero problems

If a problem cannot be fixed (e.g., requires architectural decision beyond the current scope), STOP and escalate to the user. Never leave unfixed problems in the codebase.

## Report

After the loop completes (zero findings), update `tasks/code_review.md`:

- Add a new report dated today, following the existing section structure (§1 Architecture, §2 Code Quality, §3 Naming, §4 Tests, §5 Visualization, §6 Genericity, §7 Suggestions, §8 Resolved)
- Mark issues that were fixed in this review session
- Record any issues from prior reports that still remain open (these were checked and confirmed still present)
- **Do not** remove or rewrite prior reports — append the new report at the top, below the header

## Prerequisites

Before running code review:

1. All subtasks committed
2. Quality gates pass (format check, lint, build, tests, coverage) — see `quality-gates` skill
3. No uncommitted changes in the working tree

## Completion

Code review is complete when:

- A full-repo review pass finds zero problems
- All fixes are committed
- `tasks/code_review.md` is updated
- The task can proceed to `[done]` in `tasks.md`
