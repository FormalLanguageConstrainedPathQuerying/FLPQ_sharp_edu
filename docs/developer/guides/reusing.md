# Reusing Principles

See also: [Coding Conventions](coding-conventions.md) | [Design Guides](design-guides.md) | [Quality Standards](quality-standards.md)

## What our reusing principles are

### No Duplicates

Each piece of knowledge, logic, or configuration exists in exactly one place. If the same information appears in two files, one is a duplicate and must be eliminated — either by referencing the canonical source or by merging into a single location.

This applies to:

- **Code**: functions, types, constants, generators
- **Documentation**: conventions, output formats, step descriptions, tool behavior
- **Skills**: procedural instructions that duplicate existing skills or documentation

### One Source of Truth

For every topic — a tool's behavior, a coding convention, a design decision — exactly one file is the authoritative source. Other files reference it rather than restating it.

| Topic | Canonical Source |
|-------|-----------------|
| Tool conventions (no timeout, output format, STATUS values) | `tools/README.md` |
| Per-tool details (steps, thresholds, output examples) | `docs/developer/guides/tools.md` |
| Coding conventions (naming, genericity, immutability) | `docs/developer/guides/coding-conventions.md` |
| Documentation requirements (module doc structure, mapping table) | `docs/developer/guides/documentation-conventions.md` |
| Quality gate procedures (when to run, how to interpret) | `.opencode/skills/quality-gates/SKILL.md` |

## Why reusing matters

- **Maintainability**: a change in one place propagates everywhere. Duplicates diverge silently and become stale
- **Consistency**: a single source prevents contradictory information across files
- **Traceability**: every implementation is directly traceable to its canonical definition
- **Faster onboarding**: new contributors navigate documentation to find what exists rather than searching code for duplicates

## What the reuse checklist is

Five questions that determine whether new material should be added or existing material reused:

1. **Can I reuse existing stuff?** — Is there already a function, type, doc section, or skill that covers this?
2. **Can I reference existing docs?** — Instead of duplicating information, can I link to the canonical source?
3. **Can I call an existing function?** — Does a helper already exist that I can compose with?
4. **Can I generalize existing stuff?** — Can an existing function be made more generic to cover this case?
5. **Can I extract a shared helper?** — If two places need similar logic, can I factor it into one reusable function?

## What patterns enable reuse

### Code patterns

- **Generic types**: functions parameterized over `'t`, `'nt` rather than hardcoded `string` — one implementation serves multiple alphabets
- **Shared helpers**: common utilities (command execution, project discovery, output management) in `common.py` or shared F# modules
- **Composition**: building new behavior from existing primitives rather than writing new logic
- **Non-empty collections by type**: `NonEmptyList<'t>`, `NonEmptySet<'t>` enforce invariants at compile time, eliminating runtime checks

### Documentation patterns

- **Reference over duplication**: a skill describes *how* to do something and references the doc for *what* it is. The doc describes *what* and *why*.
- **Cross-references**: `See also` links between related guides; skills reference canonical docs instead of restating conventions
- **Single ownership**: each piece of information has one owning file; other files link to it

## What documentation structure enables navigation

Documentation mirrors code structure and serves as a fast lookup map:

- `docs/main.md` — root entry point, links to all hubs and guides
- `docs/developer/guides/` — technical guides (conventions, standards, principles)
- `docs/developer/FLPQ.<Project>.md` — project hub docs listing modules
- `docs/developer/<module-name>.md` — per-module documentation

When searching for existing functionality, read the relevant hub doc first to see what modules exist, then use keywords to search module docs. This is faster than scanning source files directly.
