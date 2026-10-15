---
name: tests-writer
description: "Use when writing tests: FsCheck property-based tests, golden/snapshot tests, test generators, and FsCheck.Xunit integration. The full guide (FsCheck API, Arbitrary/Gen patterns, generator registration, language registry workflow, golden test pattern) lives in docs/developer/guides/testing.md."
---

# Writing Tests

Before writing tests, read
[`docs/developer/guides/testing.md`](/docs/developer/guides/testing.md) —
it is the single source of truth for FsCheck 3.x API and FsCheck.Xunit
integration, test requirements (property-based, equivalence, shared
generators), the Language Registry workflow, and the golden (snapshot) test
pattern.

For F#-language FsCheck quirks (Gen shadowing, naming, overloads), see the
`fsharp-coder` skill.
