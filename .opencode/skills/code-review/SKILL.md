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

Code review checks code against rules defined in **canonical source documents**. The reviewer must read each source and verify compliance. Do not rely on a memory of these documents — open and consult them during review. New rules added to any source are automatically picked up next time review runs.

### Constraint Sources Map

Before reviewing, read each canonical source. For categories marked **Auto**, the tool has already verified compliance — skip manual checking but note the tool in the report. For **Manual** categories, check code directly against the rule stated in the referenced source section.

| # | Review Category | Canonical Source | Auto/Manual |
|---|----------------|-----------------|-------------|
| 1 | Naming case (PascalCase/camelCase) | `docs/developer/guides/coding-conventions.md` § Casing | Auto — FSharpLint |
| 2 | Code style idioms (`x=true`→`x`, `List.map f (List.map g x)`→`map(g>>f)x`, etc.) | `fsharplint.json` hints | Auto — FSharpLint |
| 3 | Tab characters, redundant keywords, unused bindings | `fsharplint.json` | Auto — FSharpLint |
| 4 | Formatting (indentation, line breaks, spacing) | Fantomas config | Auto — Fantomas |
| 5 | XML doc comments on public API | `docs/developer/guides/coding-conventions.md` § Documentation | Manual |
| 6 | Genericity — no hardcoded `string` in algorithm types | `docs/developer/guides/coding-conventions.md` § Maximal genericity, § Genericity over hardcoded types | Manual |
| 7 | Non-empty collections by type (NonEmptyList/NonEmptySet vs list/Set with runtime check) | `docs/developer/guides/coding-conventions.md` § Non-empty collections by type | Manual |
| 8 | Separation — algorithms produce F# data, printers render it | `docs/developer/guides/design-guides.md` § Separation of data from presentation | Manual |
| 9 | One algorithm per file | `docs/developer/guides/design-guides.md` § One algorithm, one file | Manual |
| 10 | Variants as thin layers over shared infrastructure | `docs/developer/guides/design-guides.md` § Variants as thin layers | Manual |
| 11 | Compile-time safety — types make illegal states unrepresentable | `docs/developer/guides/design-guides.md` § Compile-time safety over runtime checks | Manual |
| 12 | No code duplication — if >3 non-trivial lines copied, extract shared function | `docs/developer/guides/design-guides.md` § Avoid code duplication + `docs/developer/guides/reusing.md` § No Duplicates | Manual |
| 13 | Language registry — single source of truth for grammars, RSMs, accept/reject strings, generators | `docs/developer/guides/language-registry.md` + `.opencode/skills/tests-writer/SKILL.md` § Using the Language Registry, § Do NOT | Manual |
| 14 | No stubbed tests — no `Assert(true)`, empty bodies `()`, commented-out checks, `Skip` without explanation, or tautological assertions | `.opencode/skills/tests-writer/SKILL.md` § Test Requirements | Manual |
| 15 | Property vs Fact — `[<Property>]` tests use FsCheck-generated inputs; `()` + hardcoded data → `[<Fact>]` | `.opencode/skills/tests-writer/SKILL.md` § Test Requirements | Manual |
| 16 | FsCheck generators in shared `Generators.fs`, not duplicated across test projects | `.opencode/skills/tests-writer/SKILL.md` § Shared generators | Manual |
| 17 | Equivalence tests for every algorithm variant | `docs/developer/guides/quality-standards.md` § Equivalence tests for all variants | Manual |
| 18 | Test coverage — every `src/` module has at least one correspondent in `tests/` | This section | Manual |
| 19 | Documentation completeness — module docs, hub docs, architecture docs, navigation links | `docs/developer/guides/documentation-conventions.md` § Documentation Mapping Table, § Documentation Completeness | Manual |
| 20 | Book traceability — every implementation references specific book section/figure/listing | `AGENTS.md` § Code must be clear + `docs/developer/guides/coding-conventions.md` § Documentation | Manual |
| 21 | Code clarity — no nontrivial optimizations; clear reference implementations | `AGENTS.md` § Code must be clear | Manual |
| 22 | Naming semantics — misleading names, type-level inconsistencies (e.g., `list` where `NonEmptyList` is correct) | `docs/developer/guides/coding-conventions.md` § Non-empty collections + § Maximal genericity | Manual |

### Scope of Automated Checks

FSharpLint (0 warnings required) checks: naming case, code style hints, tab characters, redundant keywords, unused underscore-prefixed bindings, and structural patterns. It does **not** check: XML doc comments, genericity, type choice (NonEmptyList vs list), architecture, duplication, test quality, documentation completeness, book traceability, or domain-specific rules (language registry). A "0 warnings" lint pass is necessary but far from sufficient.

Fantomas (0 diffs required) checks: formatting consistency only. It does not validate any semantic property of the code.

The build and test suite ensure code compiles and tests pass. They do not enforce conventions, detect duplication, or validate architecture.

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
- Each finding must reference its constraint source (row number from the [Constraint Sources Map](#constraint-sources-map) above)
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
