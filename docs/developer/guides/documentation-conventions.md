# Documentation Conventions

See also: [Coding Conventions](coding-conventions.md) | [Design Guides](design-guides.md) | [Quality Standards](quality-standards.md)

This document is the **single source of truth** for all documentation requirements in the project. Skills reference this document for content requirements; they describe *how* to execute documentation procedures.

## Why documentation conventions matter

Documentation must be detailed enough to reproduce an identical project from scratch — anyone must be able to reimplement the project in another language using the documentation only. Documentation must make it clear **why** a particular decision was made. Consistent documentation structure ensures that every module, decision, and book interaction is traceable and complete.

## What our documentation requirements are

### Module Documentation Structure

Each implemented module must have a dedicated documentation file in `docs/developer/` (e.g., `docs/developer/matrix.md`). The file must describe:

- **Type definitions with design rationale** — why each type exists and what problem it solves
- **All function signatures** with behavior, preconditions, and postconditions
- **Key design decisions** and their justification
- **Relationship to the book** (section/figure references) where applicable

Root documentation entry point is [`docs/main.md`](/docs/main.md). Use it for navigation.

### Decision Documentation

Each decision point and decision must be documented before implementation:

- Documentation must be detailed enough to reproduce an identical project from scratch without intermediate steps
- Documentation must make it clear **why** a particular decision was made

### Commit Messages

Commit messages must be detailed enough to understand the reasons for changes. Anyone must be able to explain why particular changes were required using only the commit message.

### Book Errors

If a book error is found, record it in `tasks/fixes_for_book.md` with:

- Clear description of the error
- Suggested correction
- Notification to the user

If additional information not presented in the book was required, record it similarly.

### Documentation Mapping Table

For every source change, the corresponding doc changes are mandatory. Use this table to determine which docs are affected:

| Source change | Required doc actions |
|---|---|
| New module in `src/FLPQ.Printers/` | New `docs/developer/<module-name>.md` — describe types, functions, design decisions, book references |
| New module in `src/FLPQ.Languages/` | New `docs/developer/<module-name>.md` |
| New module in `src/FLPQ.LinearAlgebra/` | New `docs/developer/<module-name>.md` |
| New module in `src/FLPQ.GraphAnalysis/` | New `docs/developer/<module-name>.md` |
| New module in `src/FLPQ.RPQ/` | New `docs/developer/<module-name>.md` |
| New file in any `src/` project | Update `docs/developer/FLPQ.<Project>.md` — add module to list |
| New file in any `src/` project | Update `docs/project/architecture.md` — add file to project file listing |
| New CLI runner | Update `docs/developer/FLPQ.Cli.md` — add runner description |
| New CLI runner with new output format | Update `docs/user/cli.md` — add algorithm to listing, describe output |
| New Algorithm DU case | Update `docs/user/cli.md` — add to algorithm list |
| Changed public API (new parameter, renamed function) | Update existing `docs/developer/<module>.md` — reflect API change |
| New doc page | Update `docs/main.md` — add link under appropriate section |
| New visualization module | Update the corresponding algorithm doc to cross-reference the visualizer |
| Book discrepancy found | Update `tasks/fixes_for_book.md` |

### Documentation Completeness (Review Criteria)

Every code change must be reflected in documentation. When reviewing, verify:

- **Module doc completeness**: every `.fs` file in `src/` must have a corresponding entry in `docs/developer/`. New modules without docs are a finding
- **Hub doc updates**: new files must appear in the project hub doc (`docs/developer/FLPQ.<Project>.md`)
- **Architecture doc updates**: new files must appear in `docs/project/architecture.md`
- **CLI user doc updates**: new CLI features (algorithms, flags, output formats) must appear in `docs/user/cli.md`
- **Cross-references**: new visualization modules must be referenced from their corresponding algorithm docs
- **Navigation**: new doc pages must be linked from `docs/main.md`
