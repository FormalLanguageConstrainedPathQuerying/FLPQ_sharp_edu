---
name: documentation
description: Use when writing or updating documentation: module docs in docs/, recording book errors in fixes_for_book.md, or documenting design decisions. Covers the procedure for identifying, writing, and linking documentation.
---

# Documentation

## Content Requirements

All documentation content requirements — module doc structure, decision docs format, book error recording format, commit message standards, and the source-change-to-doc-action mapping table — live in [`docs/developer/guides/documentation-conventions.md`](/docs/developer/guides/documentation-conventions.md). Load that document for the authoritative specification of *what* documentation must contain.

## Procedure

### 1. Identify affected docs

For every source change, determine which doc files are affected using the mapping table in `docs/developer/guides/documentation-conventions.md`.

### 2. Write or update docs

- For new modules: create `docs/developer/<module-name>.md` following the module documentation structure in the conventions doc
- For changed public APIs: update the existing module doc to reflect the change
- For new CLI features: update `docs/user/cli.md`
- For new files: update `docs/developer/FLPQ.<Project>.md` and `docs/project/architecture.md`
- For new doc pages: update `docs/main.md` with navigation links
- For book errors: record in `tasks/fixes_for_book.md`

### 3. Verify completeness

After writing, verify against the documentation completeness criteria in the conventions doc:

- Every new `.fs` file has a corresponding `docs/developer/` entry
- Every new module appears in its project hub doc
- Every new page is linked from `docs/main.md`
- Every new CLI feature is documented in `docs/user/cli.md`
