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
  * For manipulation with sln file: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln
  * To create new projects or solutions: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new

## Dependencies

* Restore: `dotnet restore`
* Use dotnet CLI for management. Documentation: https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-dotnet-cli

## Build
* Release mode: `dotnet build -c Release`
* debug mode: `dotnet build -c Debug`

## Clear
* `dotnet clean` 

## Tests
* `dotnet test`
* Run specific test: `dotnet test --filter <test identifier>`
* Use `[<Trait("Category", <category_name>)>]` and respective filters to create and run groups of tests.
* For visualization: create tests that check that generated output is correct. E.g. generated dot-file can by compiled using graphviz dot, generated TeX output wrapped to appropriate file can be compiled by pdflatex or lualatex. 
* When a task specification states that certain constructs "can be used for property-based tests", implement `[<Property>]` tests with FsCheck-generated random inputs. Do not substitute `[<Fact>]` tests enumerating hardcoded examples in place of property-based testing.

## Format
* To format all F# sources: `dotnet fantomas .`
* To check formatting without modifying files: `dotnet fantomas . --check`

## Prototyping 

You can use F# scripts and F# interactive for rapid prototyping.
```
dotnet fsi Script.fsx
```

## Basic code style
* Use PascalCase for types and modules, camelCase for functions and values.
* Prefer `let` bindings over mutable state.
* Use XML documentation comments (///) for public APIs.
* Use units of measure
* Prefer functional style: types + functions. Eg type Automata and module Automata at the same level with functions like intersect or accept. If necessary, types may be classes, not only immutable types like DU or structs.
* Use structs with explicit names of fields, not tuples.


# Project structure
* Documentation in `docs/`. Root is [`main.md`](/docs/main.md)
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
  * Check formatting and compilation
  * Check tests  
  * Repeat until all tests pass
* If a book error is found, it must be recorded in `fixes_for_book.md` with a clear description and suggested correction, and the user should be notified.
* If additional information, that not presented in the book was required for implementation, it must be recorded in `fixes_for_book.md` with a clear description and suggested improvements, and the user should be notified.
* Move changes to `dev`
* Mark the task as completed in `tasks.md`
* Go to first step

## Git

* Main branch is `main` it is protected.
* Branch for stable development results is `dev`.
* Use feature-branch for each task. 
  * Branch naming convention: `feature/XXX-short-description` where `XXX` is the task `ID` from `tasks.md`.
  * Never combine multiple task IDs in a single branch. Each task gets its own branch.
* Commit message format: conventional Commits: feat:, fix:, docs:, test:
* Before each commit: 
  * Format code.
  * Check that compilation is successful.
* Before moving changes from feature-branch to dev:
  * Format code.
  * Check that compilation is successful.
  * Check that all tests are turned on.
  * Check that all tests passed successfully.
* Use `Squash and Rebase` strategy to move changes from feature-branch to dev. History of `dev` must be linear.
* No emergency fixes.
* All work is local, no `push`-es.