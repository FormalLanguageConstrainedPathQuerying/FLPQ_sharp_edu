# Documentation Conventions

**Tags:** guide, documentation, conventions, template, metadata
**Kind:** guide

> **Abstract:** Single source of truth for all documentation requirements: document metadata format (tags, kind, abstract, TOC), section templates by document kind, source-change-to-doc-action mapping table, and completeness verification criteria. Skills reference this document for content requirements ("what"); they describe *how* to execute documentation procedures.

## Contents

- [Document Metadata](#document-metadata)
- [Canonical Document Template](#canonical-document-template)
- [Document Kinds and Section Templates](#document-kinds-and-section-templates)
- [Tag Taxonomy](#tag-taxonomy)
- [Searching by Metadata](#searching-by-metadata)
- [Module Documentation Requirements](#module-documentation-requirements)
- [Decision Documentation](#decision-documentation)
- [Commit Messages](#commit-messages)
- [Book Errors](#book-errors)
- [Documentation Mapping Table](#documentation-mapping-table)
- [Documentation Completeness (Review Criteria)](#documentation-completeness-review-criteria)

## Document Metadata

Every documentation file in `docs/developer/` must begin with a metadata block using **bold-prefix lines** immediately after the title. This is pure Markdown, human-readable, and machine-grep-friendly.

### Metadata Fields

| Field | Format | Required | Description |
|-------|--------|----------|-------------|
| **Tags** | `tag1, tag2, ...` | Yes | Classifying keywords for grep-based search |
| **Kind** | One of: `algorithm`, `data-structure`, `utility`, `visualization`, `hub`, `guide` | Yes | Document category determining the section template |
| **Module** | `F# module name` | Yes (except guides) | The F# module this doc describes |
| **Source** | `` `path/to/file.fs` `` | Yes (except guides) | Path to the source file |
| **Depends on** | `Module1, Module2, ...` | No | Modules this module depends on |
| **Used by** | `Module1, Module2, ...` | No | Modules that depend on this module |
| **Book reference** | `Chapter X, Section sec:label` | No | Book section(s) this module corresponds to |

### Metadata Block Format

```markdown
# Module Title

**Tags:** tag1, tag2, tag3
**Kind:** algorithm
**Module:** ModuleName
**Source:** `src/ProjectName/ModuleName.fs`
**Depends on:** Dependency1, Dependency2
**Used by:** Consumer1, Consumer2
**Book reference:** Chapter X, Section sec:label

> **Abstract:** Brief, precise 2-4 sentence description of the document's content. Covers what the module does, its key data structures or algorithms, and its relationship to other modules.

## Contents
...
```

## Canonical Document Template

Every developer doc follows this structure:

1. **Title** (`# Title`) — module name or clear descriptive title
2. **Metadata block** — bold-prefix lines (Tags, Kind, Module, Source, Depends on, Used by, Book reference)
3. **Abstract** — blockquote (`> **Abstract:** ...`), 2-4 sentences, brief and precise
4. **Table of Contents** (`## Contents`) — bullet list of internal links to all major sections
5. **Kind-specific sections** — see [Document Kinds and Section Templates](#document-kinds-and-section-templates)
6. **Design Decisions** (`## Design Decisions`) — table of decision/rationale pairs
7. **Book Reference** (`## Book Reference`) — book sections, figures, listings
8. **See Also** (`## See Also`) — cross-references to related docs

## Document Kinds and Section Templates

### `kind: algorithm`
Documents describing parsing algorithms, graph algorithms, and language-theoretic procedures.

**Required sections:**
- `## Algorithm` — structured pseudocode, time/space complexity. This section describes the algorithm *before* the type/function details. Must include: input/output specification, step-by-step procedure, complexity analysis.
- `## Type Definitions` — types specific to this algorithm (e.g., trace step types)
- `## Function Signatures` — all public function signatures with behavior, preconditions, postconditions
- `## Design Decisions` — table
- `## Book Reference` — section/figure references
- `## See Also` — cross-references

**Example:** CYK, Valiant, GLL, RNGLR, LL parser, LR parser, Belyanin/Arroyuelo/Kronecker RPQ

### `kind: data-structure`
Documents describing generic data types and their operations.

**Required sections:**
- `## Data Structure` — the abstract concept explained before F# types: what it represents, invariants, complexity characteristics
- `## Type Definitions` — F# types with design rationale for each
- `## Module Functions` — all public functions with behavior, preconditions, postconditions
- `## Design Decisions` — table
- `## Book Reference` — section/figure references
- `## See Also` — cross-references

**Example:** Matrix, SPPF, PathIndex, Automaton, RSM, DerivationTree, Graph

### `kind: utility`
Documents describing parsers, readers, converters, and tool wrappers.

**Required sections:**
- `## Purpose` — what problem this utility solves
- `## Type Definitions` — types (if any)
- `## Function Signatures` — all public functions
- `## Design Decisions` — table
- `## See Also` — cross-references

**Example:** Tokenizer, GraphReader, EbnfParser, RsmToGrammar, ExternalTools

### `kind: visualization`
Documents describing TeX/DOT/Tikz rendering and visualization modules.

**Required sections:**
- `## Overview` — what is visualized, what output formats are produced
- `## Supported Formats` — list of output formats with examples
- `## Function Signatures` — all public rendering functions
- `## Design Decisions` — table
- `## See Also` — cross-references to algorithm docs being visualized

**Example:** AutomatonViz, DerivationTreeViz, GrammarTeX, VisualizationTypes

### `kind: hub`
Project-level overview documents listing modules and their relationships.

**Required sections:**
- `## Project` — project type, path, dependencies
- `## Modules` — table of modules with source/doc links
- `## Role` — role of this project in the overall architecture
- `## Book References` — book chapters relevant to this project

**Example:** FLPQ.LinearAlgebra.md, FLPQ.Languages.md, FLPQ.RPQ.md

### `kind: guide`
Developer guides describing conventions, principles, and standards.

**Required sections:**
- `## Why <topic> matters` — motivation
- `## What our <topic> are` — the rules/principles/standards
- `## See Also` — cross-references to related guides

**Example:** coding-conventions.md, design-guides.md, quality-standards.md

## Tag Taxonomy

### Domain Tags

| Tag | When to use |
|-----|-------------|
| `parsing` | Doc describes a parsing algorithm or parser infrastructure |
| `automaton` | Doc describes finite automata, DFAs, NFAs, RSM blocks |
| `graph` | Doc describes graph types, graph algorithms, MS-BFS |
| `linear-algebra` | Doc describes matrix operations, Kronecker products |
| `grammar` | Doc describes grammar types, transformations (CNF) |
| `cfg` | Doc deals with context-free grammars (CYK, Valiant, LL, LR) |
| `regular` | Doc deals with regular languages/path queries (RPQ, regex) |
| `derivation-tree` | Doc deals with parse trees, derivation trees |
| `visualization` | Doc renders something to TeX/DOT/Tikz |
| `cl` | Doc is CLI-related |

### Algorithm Tags

| Tag | When to use |
|-----|-------------|
| `cyk` | CYK algorithm |
| `valiant` | Valiant algorithm (standard or modified) |
| `ll` | LL(k) parsing |
| `lr` | LR(0)/SLR(1)/CLR(1) parsing |
| `gll` | Generalized LL (GLL) CFG parsing |
| `rnglr` | Right-Nulled Generalized LR |
| `cfpq` | Context-Free Path Querying |
| `rpq` | Regular Path Querying |
| `matrix-multiplication` | Uses matrix multiplication as a core operation |
| `bfs` | Breadth-first search |
| `epsilon-closure` | Epsilon closure computation |
| `subset-construction` | NFA-to-DFA subset construction |
| `chomsky-normal-form` | CNF transformation |

### Data Structure Tags

| Tag | When to use |
|-----|-------------|
| `matrix` | Matrix type |
| `sppf` | Shared Packed Parse Forest |
| `path-index` | Path index matrix for GLL/RNGLR |
| `gss` | Graph-Structured Stack |
| `sparse-matrix` | Sparse/set-based matrix variants |
| `boolean-decomposition` | Decomposition of sets into boolean vectors |
| `rsm` | Recursive State Machine |

### Approach Tags

| Tag | When to use |
|-----|-------------|
| `dynamic-programming` | DP-based algorithm |
| `recursive-descent` | Top-down parsing |
| `shift-reduce` | Bottom-up parsing |
| `kronecker-product` | Uses Kronecker product |
| `fixed-point` | Fixed-point iteration |
| `product-construction` | Automaton product/intersection |
| `derivative` | Brzozowski derivatives |

## Searching by Metadata

Tags enable fast, targeted searches without opening files:

```bash
# Find all algorithm docs
grep "\*\*Kind:\*\* algorithm" docs/developer/*.md

# Find all parsing-related docs
grep "\*\*Tags:\*\*.*parsing" docs/developer/*.md

# Find all docs about CYK
grep "\*\*Tags:\*\*.*cyk" docs/developer/*.md

# Find all docs using dynamic programming
grep "\*\*Tags:\*\*.*dynamic-programming" docs/developer/*.md

# Find all data structure docs
grep "\*\*Kind:\*\* data-structure" docs/developer/*.md

# Find all docs that depend on Matrix
grep "\*\*Depends on:\*\*.*Matrix" docs/developer/*.md

# Find which docs a module is used by
grep "\*\*Used by:\*\*.*PathIndex" docs/developer/*.md
```

Combine searches to narrow results:

```bash
# Find all parsing algorithms that use matrix-multiplication
grep -l "\*\*Tags:\*\*.*matrix-multiplication" docs/developer/*.md | xargs grep -l "\*\*Tags:\*\*.*parsing"
```

### Searching by Metadata

Tags enable fast, targeted searches without opening files:

```bash
# Find all algorithm docs
grep "\*\*Kind:\*\* algorithm" docs/developer/*.md

# Find all parsing-related docs
grep "\*\*Tags:\*\*.*parsing" docs/developer/*.md

# Find all docs about CYK
grep "\*\*Tags:\*\*.*cyk" docs/developer/*.md

# Find all docs using dynamic programming
grep "\*\*Tags:\*\*.*dynamic-programming" docs/developer/*.md

# Find all data structure docs
grep "\*\*Kind:\*\* data-structure" docs/developer/*.md

# Find all docs that depend on Matrix
grep "\*\*Depends on:\*\*.*Matrix" docs/developer/*.md

# Find which docs a module is used by
grep "\*\*Used by:\*\*.*PathIndex" docs/developer/*.md
```

Combine searches to narrow results:

```bash
# Find all parsing algorithms that use matrix-multiplication
grep -l "\*\*Tags:\*\*.*matrix-multiplication" docs/developer/*.md | xargs grep -l "\*\*Tags:\*\*.*parsing"
```

## Module Documentation Requirements

Each implemented module must have a dedicated documentation file in `docs/developer/` (e.g., `docs/developer/matrix.md`). The file must describe:

- **Type definitions with design rationale** — why each type exists and what problem it solves
- **All function signatures** with behavior, preconditions, and postconditions
- **Key design decisions** and their justification
- **Relationship to the book** (section/figure references) where applicable
- **Algorithm description** for algorithm modules — structured pseudocode and complexity analysis before type/function details

Root documentation entry point is [`docs/main.md`](/docs/main.md). Use it for navigation.

## Decision Documentation

Each decision point and decision must be documented before implementation:

- Documentation must be detailed enough to reproduce an identical project from scratch without intermediate steps
- Documentation must make it clear **why** a particular decision was made

## Commit Messages

Commit messages must be detailed enough to understand the reasons for changes. Anyone must be able to explain why particular changes were required using only the commit message.

## Book Errors

If a book error is found, record it in `tasks/fixes_for_book.md` with:

- Clear description of the error
- Suggested correction
- Notification to the user

If additional information not presented in the book was required, record it similarly.

## Documentation Mapping Table

For every source change, the corresponding doc changes are mandatory. Use this table to determine which docs are affected:

| Source change | Required doc actions |
|---|---|
| New module in `src/FLPQ.Printers/` | New `docs/developer/<module-name>.md` — follow the canonical template with metadata, abstract, TOC |
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

## Documentation Completeness (Review Criteria)

Every code change must be reflected in documentation. When reviewing, verify:

- **Metadata completeness**: every doc in `docs/developer/` must have a metadata block (Tags, Kind, Module, Source) at the top
- **Abstract present**: every doc must have an abstract blockquote after the metadata
- **TOC present**: every doc must have a `## Contents` section with internal links
- **Module doc completeness**: every `.fs` file in `src/` must have a corresponding entry in `docs/developer/`. New modules without docs are a finding
- **Hub doc updates**: new files must appear in the project hub doc (`docs/developer/FLPQ.<Project>.md`)
- **Architecture doc updates**: new files must appear in `docs/project/architecture.md`
- **CLI user doc updates**: new CLI features (algorithms, flags, output formats) must appear in `docs/user/cli.md`
- **Cross-references**: new visualization modules must be referenced from their corresponding algorithm docs
- **Navigation**: new doc pages must be linked from `docs/main.md`
- **Algorithm section**: every `kind: algorithm` doc must have a `## Algorithm` section with structured pseudocode

## See Also

- [Coding Conventions](coding-conventions.md)
- [Design Guides](design-guides.md)
- [Quality Standards](quality-standards.md)
- [Reusing Principles](reusing.md)
