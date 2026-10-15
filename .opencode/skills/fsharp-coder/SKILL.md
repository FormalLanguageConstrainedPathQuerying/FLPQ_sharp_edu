---
name: fsharp-coder
description: "Use when writing F# code: language-specific patterns, idioms, gotchas, and library API quirks. The full catalog (FsCheck API quirks, closure patterns, record ambiguity, NonEmptySet API, optional parameters, pattern annotations, Argu flags, CLI entry points) lives in docs/developer/guides/fsharp-quirks.md."
---

# F# Coding Patterns and Quirks

Before writing or reviewing F# code, read
[`docs/developer/guides/fsharp-quirks.md`](/docs/developer/guides/fsharp-quirks.md) —
it is the single source of truth for the project's F# language patterns and
library API quirks (FsCheck Gen shadowing/naming/overloads, closure capture,
record type ambiguity, FSharpPlus `NonEmptySet`, FS0691/FS0718, pattern
annotations, Argu no-payload flags, testable CLI entry points, code search
tips).

For FsCheck testing methodology (Property attribute, generators, golden tests),
see the `tests-writer` skill.
