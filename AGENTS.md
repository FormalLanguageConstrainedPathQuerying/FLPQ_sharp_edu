# Project description

This project is supplementary materials for the book on formal language constrained path querying. 
* Programming language: F# (.net 10.0 or higher). Root of F# documentation: https://learn.microsoft.com/en-us/dotnet/fsharp/
* Code must be clear, without any nontrivial optimizations.
* Code and book must be as close as possible. Everyone must be able to reproduce the code using the book only. Every implementation must be directly traceable to a specific algorithm or example in the book. Where possible, reference the book’s section, figure, or listing in the source code comments.
* Algorithms implementation designed to examples or reference implementations of algorithms from for the book.
* Algorithms implementation designed to visualize steps and results.
* Language of comments, documentation is English.

# dotnet CLI
* Use dotnet CLI for all manipulations with project
* Dotnet CLI documentation entry point: https://learn.microsoft.com/en-us/dotnet/core/tools/
  * For manipulation with .slnx file: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln
  * To create new projects or solutions: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new

## Dependencies

* Restore: `dotnet restore`
* Use dotnet CLI for management. Documentation: https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-dotnet-cli

## Build
* Release mode: `dotnet build FLPQ.slnx -c Release`
* debug mode: `dotnet build FLPQ.slnx -c Debug`

## Clear
* `dotnet clean` 

## Tests
* `dotnet test`
* Run specific test: `dotnet test --filter <test identifier>`
* Use `[<Trait("Category", <category_name>)>]` and respective filters to create and run groups of tests.
* For visualization: create tests that check that generated output is correct. E.g. generated dot-file can by compiled using graphviz dot, generated TeX output wrapped to appropriate file can be compiled by lualatex or lualatex. 
* When a task specification states that certain constructs "can be used for property-based tests", implement `[<Property>]` tests with FsCheck-generated random inputs. Do not substitute `[<Fact>]` tests enumerating hardcoded examples in place of property-based testing.
* Property-based tests MUST use FsCheck `Arbitrary`/`Gen` types with `[<Property>]`. Never use `System.Random.Shared` in manual `for`-loops to drive randomized test inputs.
* FsCheck generators for shared project types (matrices, graphs, grammars, regexes) must live in a common `Generators.fs` module. Do not duplicate random generation logic across test projects.
* Every new algorithm variant must include property-based equivalence tests proving it returns identical results to at least one existing reference implementation. Example: "standard Valiant ≡ modified Valiant", "Belyanin ≡ Arroyuelo ≡ Kronecker+MS-BFS".

## Format
* To format all F# sources: `dotnet fantomas .`
* To check formatting without modifying files: `dotnet fantomas . --check`
* To run F# linter per-project: `DOTNET_ROOT=/usr/lib/dotnet dotnet-fsharplint lint <project-path>`
* To run F# linter on full solution: `DOTNET_ROOT=/usr/lib/dotnet dotnet-fsharplint lint FLPQ.slnx` (slow, may timeout)

## Prototyping

You can use F# scripts and F# interactive for rapid prototyping.
```
dotnet fsi Script.fsx
```

## Tool output capture

* **Mandatory**: every invocation of `dotnet build`, `dotnet test`, `dotnet fantomas --check`, `dotnet-fsharplint lint`, or similar expensive CLI tools MUST redirect all output (stdout + stderr) to a file in `tmp/`.
* Command pattern: `dotnet test > tmp/test-output.txt 2>&1`
* After capturing, analyze the output file with the Grep or Read tools — do NOT re-run the tool with a different grep/filter.
* File naming convention in `tmp/`:
  * `tmp/build-output.txt` — build output
  * `tmp/test-output.txt` — test output
  * `tmp/fantomas-output.txt` — fantomas format check output
  * `tmp/fsharplint-output.txt` — fsharplint output
* Re-running the same tool is allowed ONLY when source files changed since the last capture.

## Basic code style
* Use PascalCase for types, modules, record fields, and union case fields; camelCase for functions and values.
* Prefer `let` bindings over mutable state.
* Use XML documentation comments (///) for public APIs.
* Use units of measure
* Prefer functional style: types + functions. Eg type Automata and module Automata at the same level with functions like intersect or accept. If necessary, types may be classes, not only immutable types like DU or structs.
* Use structs with explicit names of fields, not tuples.

### Genericity and type safety

All code must be as generic as possible. Never hardcode a concrete type when a generic type parameter would work. Examples (not exhaustive):

* **Algorithms over symbols**: all parsing algorithms (CYK, Valiant, LL, LR), automata operations, and RPQ algorithms must be generic over terminal and nonterminal types (`'t`, `'nt`). Do not hardcode `string`-based `Symbol`, `Terminal`, or `Nonterminal`.
* **Matrix operations**: all linear algebra functions (`mxm`, `kron`, `map2`, `transpose`, etc.) must be generic over element type (`'a`, `'b`, `'c`). Do not assume `bool` or `int`.
* **Graphs and automata**: vertex and edge label types must be generic. A graph over `string`-labeled edges is just one instantiation.
* **Visualization and printing**: rendering functions must be parametrized by symbol-printer functions (`'a -> string`). Never embed `sprintf "%A"` or type-specific formatting inside rendering logic.

Concrete rules:

* When writing a function `f : 'a -> 'b`, ask: can `'a` or `'b` be more general? If a function only needs `map` on `'a`, it should accept any functor, not just `list` or `Matrix`.
* Use `NonEmptyList<'t>` and `NonEmptySet<'t>` from FSharpPlus for any collection that semantically must not be empty (e.g., right-hand side of a production, transition-label set). Never use empty lists/sets with runtime checks when the type can enforce the invariant.
* Unit tests may instantiate generic types at `string` for readability, but the implementation must not depend on it.

### Avoiding code duplication

* Before writing a helper function, check existing code for reusable abstractions. If you find yourself copy-pasting more than 3 non-trivial lines, extract a shared function.
* When implementing a variant of an existing algorithm (e.g., modified Valiant), maximize reuse of shared infrastructure. Write the variant as a thin layer over common functions, not a full rewrite.
* After finishing a module, scan the codebase for duplication (same logic under different names, copy-pasted blocks). Consolidate if found.

### Separation of concerns

* Algorithms collect trace, result, and intermediate data **exclusively as F# data structures** (records, DUs, matrices). They must never call rendering/printing functions.
* All conversion to output formats (TeX, dot, plain text) lives in dedicated printer modules (`src/FLPQ.Printers`). An algorithm returns data; a printer converts data to a string.
* The pattern for visualization is:
  ```fsharp
  let result, trace = algorithm.run input
  let texOutput    = TraceVisualizer.toTex trace
  ```
  Never embed `writeOutputFile` or TeX/dot string generation inside algorithm modules.


# Project structure
* Documentation in `docs/`. Root is [`main.md`](/docs/main.md). Use it for navigation. Do not try to read all sources at once.
  * Each implemented module must have a dedicated documentation file in `docs/` (e.g., `docs/matrix.md`). The file must describe:
    - Type definitions with design rationale
    - All function signatures with behavior, preconditions, and postconditions
    - Key design decisions and their justification
    - Relationship to the book (section/figure references) where applicable
* Sources in `src/`
* Tests and tests-related stuff in `tests/`
  * Mirror the `src/` folder structure inside `tests/` so that each project has a corresponding tests.
* Data for tests and examples in `data/`
* Space for tasks planning in `tasks/`
  * [`tasks.md`](tasks/tasks.md) for user-defined tasks and their status tracking
  * [`detailed_plan.md`](tasks/detailed_plan.md) for detailed planing of the current task and progress tracking.
  * [`global_plan.md`](tasks/global_plan.md) for global planning. Track your global plans here.
  * [`fixes_for_book.md`](tasks/fixes_for_book.md) for book-related problems that you detected and user should fix in the book.
  * [`knowledge_base.md`](tasks/knowledge_base.md) — accumulated knowledge about libraries, frameworks, and tooling (API quirks, workarounds, best practices discovered during implementation).

# Workflow

* Do tasks strictly one at a time. Each task gets its own feature branch, its own detailed plan, and its own merge to dev. Never combine multiple tasks in a single feature branch, even if they appear interdependent or the user asks for several at once.
* Each decision point and decision must be documented before implementation.
  * Documentation must be detailed enough to reproduce identical project from scratch without intermediate steps. E.g. anyone must be able to reimplement the project in another language using the documentation only.
  * Documentation must be detailed enough to realize why a particular decision was made in the project.
  * Commit messages must be detailed enough to realize reasons of changes. Anyone must be able to explain why particular changes were required using only commit message.

## Multi-task planning

* When the user asks to work on a **set of tasks** (several task IDs), do NOT jump directly into implementation. First create a high-level global plan in `tasks/global_plan.md`.
* The global plan must:
  - List all tasks to be done with their IDs and brief descriptions.
  - Identify dependencies between tasks (which must be done before which).
  - Identify potential conflicts or overlapping changes (e.g., two tasks modifying the same file).
  - Identify shared infrastructure (types, helpers, utilities) that multiple tasks need. Create shared modules to avoid duplication across tasks.
  - Identify existing reusable abstractions (types, helper functions, generators) that new tasks should use rather than reinvent.
  - For each task that involves rendering/visualization, note that rendering and algorithm logic must be in separate projects/modules from the start.
  - Propose an execution order that minimizes rework and avoids conflicts.
  - Align tasks with the project architecture.
* After the global plan is created, proceed with the normal working loop: one task at a time, feature branch per task, detailed plan in `tasks/detailed_plan.md` for each.

## Working loop

* Ensure that user-defined tasks, the global plan, and the overall project architecture are aligned with each other. If not, align global plan and architecture with respect to user-defined tasks.
* When all are aligned, choose exactly ONE user-defined task that is not done yet.
* Create a feature-branch from `dev` for this single task. One task — one branch.
* Generate detailed plan for this task to `detailed_plan.md`. Track your progress in detailed plan.
* Update all respective documentation. Commit updates.
* Write tests.
* Development loop
  * Update documentation. All relevant documentation must be updated: all task-related docs (including `fixes_for_book.md`), all project-related documentation (including `README.md`).
  * After implementing a module, create or update `docs/<module>.md` describing its design and logic (type definitions, function signatures, design decisions).
  * Update `tasks/knowledge_base.md` with any non-obvious knowledge gained about libraries, frameworks, or tooling (API quirks, workarounds, best practices discovered during implementation).
  * Write code
  * When creating a new project, add it to `FLPQ.slnx`.
  * Check formatting and compilation
   * Check tests — run `dotnet test` on the full solution. Every new and existing test must pass.
   * Repeat until all tests pass.
   * **Hard gate**: a task is ready to move to dev ONLY when every test in the entire solution passes (zero failures). Run `dotnet test` on the whole solution, not just the new test file. A single failing test is a blocker.
   * **Blocked work protocol**: if you encounter an algorithmic problem that you cannot resolve to 100% correctness, STOP. Do not merge. Do not mark done. Do not comment out or weaken failing tests to make the suite green. Instead:
     1. Stay on the feature branch.
     2. Report the problem concretely to the user:
        - Which tests fail and why.
        - What algorithmic gap exists (e.g., "LR goto entries missing for nested nonterminal calls").
        - What you've tried and what remains unresolved.
     3. Ask the user for guidance: additional subtasks, algorithmic hints, descoping, or splitting the task.
     4. Update `tasks/detailed_plan.md` with a section listing:
        - Which subtasks are blocked and why.
        - What was tried and what failed.
        - What specific help is needed from the user.
        Commit this update so the plan serves as a persistent record of the blocking state.
  * **Duplication check**: before considering a module done, scan the codebase for accidental code duplication (same logic under different names, copy-pasted blocks). Consolidate if found.
  * **Genericity check**: verify that new types use generic parameters (`'t`, `'nt`) where applicable and that non-empty collections use `NonEmptyList`/`NonEmptySet`.
  * **Equivalence test check**: if the module is a variant of an existing algorithm, ensure a property-based equivalence test exists comparing it to the reference implementation.
  * **Separation check**: verify that algorithm modules do not contain TeX/dot string generation or file I/O.
* If a book error is found, it must be recorded in `fixes_for_book.md` with a clear description and suggested correction, and the user should be notified.
* If additional information, that not presented in the book was required for implementation, it must be recorded in `fixes_for_book.md` with a clear description and suggested improvements, and the user should be notified.
* Move changes to `dev`
* **Completion verification**: before marking a task as done, confirm:
   - ALL subtasks from the task description in `tasks.md` are implemented.
   - ALL tests across the entire solution pass (zero failures).
   - There are zero known algorithmic gaps, partial implementations, or skipped test cases.
   - `dotnet test` output shows `Skipped: 0` for all test projects (or skips are explicitly approved and documented in `detailed_plan.md`).
   - Equivalence tests pass against the reference implementation if the task requires it.
* Mark the task as completed in `tasks.md` — **only prepend `[done] ` to the existing task line. Never rewrite the task description. The task text in `tasks.md` is user-authored and immutable.**
   - **CRITICAL: Never run `git checkout tasks/tasks.md`, `git restore tasks/tasks.md`, or any git reset/checkout command on `tasks.md`.** The working-tree version of this file is authoritative — it may contain user-authored task details not yet committed. Git operations that revert to a committed version will destroy uncommitted task descriptions.
   - **Verify before editing**: always read the current working-tree version with the Read tool first. Do not rely on the last committed version from git history.
   - **The `[done]` tag means COMPLETE**: every requirement met, every test passing, every edge case handled. Never mark a task as `[done]` with known failures or unresolved limitations.
* Go to first step

## Task authoring guidelines

When writing a new task for `tasks.md`, follow these rules to minimize rework:

* **Specify output format upfront**. If the task involves TeX or dot visualization, include the exact column layout, math mode conventions, and formatting rules in the task description (see tasks 50 and 51 for examples).
* **Keep tasks single-responsibility**. A task should do one thing. If it requires more than 5 sub-items or spans multiple unrelated concerns, split it into multiple tasks.
* **Specify equivalence requirements**. For any new algorithm variant, explicitly state "must produce results identical to X" so equivalence tests are built from the start.
* **Specify type genericity**. If a module must handle arbitrary symbol types, state it explicitly (e.g., "generic over terminal and nonterminal types").
* **Specify reuse expectations**. If the task builds on existing infrastructure (e.g., "reuse matrix from task 2", "reuse DFA from task 14"), name the dependencies. This prevents reinvention.

## Git

* Main branch is `main` it is protected.
* Branch for stable development results is `dev`.
* Use feature-branch for each task. 
  * Branch naming convention: `feature/XXX-short-description` where `XXX` is the task `ID` from `tasks.md`.
  * Never combine multiple task IDs in a single branch. Each task gets its own branch.
* Commit message format: conventional Commits: feat:, fix:, docs:, test:
* Before each commit:
  * Format code.
  * Run linters and capture output:
    * `dotnet fantomas . --check > tmp/fantomas-output.txt 2>&1` — **must pass** (no formatting issues)
    * `DOTNET_ROOT=/usr/lib/dotnet dotnet-fsharplint lint FLPQ.slnx > tmp/fsharplint-output.txt 2>&1` — **must have zero warnings**
  * Check that compilation is successful (capture output to `tmp/build-output.txt`).
  * **Verify tasks.md is not staged.** Run `git diff --cached --name-only | grep -q tasks.md` — if it matches, unstage it with `git reset HEAD tasks.md`. tasks.md must never be committed from a feature branch.
* Before moving changes from feature-branch to dev:
   * Format code.
   * Check that compilation is successful.
* Run `dotnet test` on the FULL solution (all projects, Debug configuration). Every test must pass — zero failures. Never merge with known failures.
   * **Verify Skip count**: `dotnet test` output must show `Skipped: 0` for all test projects. A non-zero Skip count is a blocker. If a Skip is intentional, it must be documented in `detailed_plan.md` with justification before merge.
* If a test cannot be made to pass, use `[<Fact(Skip="explanation")>]` with a clear reason — never an empty body `()`, tautological assertions, or commented-out asserts. A Skip is visible in test output; an empty body silently produces a false positive.
   * If the task specifies equivalence with a reference implementation (e.g., "must produce identical results as CYK"), the equivalence property tests MUST pass with zero counterexamples.
* Use `Squash and Rebase` strategy to move changes from feature-branch to dev. History of `dev` must be linear. The merge commit message on dev must include the full detailed body from the feature branch commit(s) — a bare subject line is insufficient.
* No emergency fixes.
* All work is local, no `push`-es.

## Git safety

**Before running `git checkout <file>` or `git restore <file>`, always verify with `git diff <file>` that you won't lose uncommitted user-authored content.** This applies especially to:
- `tasks/tasks.md` — user adds task details without committing
- `tasks/fixes_for_book.md` — user may add notes interactively

If `git diff <file>` shows additions (not just modifications you made), do NOT revert. The working-tree version is authoritative for these files.