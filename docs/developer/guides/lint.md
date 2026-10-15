# Lint (FSharpLint)

**Tags:** guide, lint, fsharplint, quality
**Kind:** guide

> **Abstract:** Documents the FSharpLint setup for this project: version pinning via `dotnet-tools.json`, the no-partial-config rule, the tuned rules that deviate from FSharpLint defaults (FL0069, FL0081, FL0039), the FL0081/FL0085 interaction and the FL0069 quirk, and lint timing characteristics. For gate invocation procedures, see the `quality-gates` skill.

## Contents

- [Version](#version)
- [Timing](#timing)
- [No Partial Configs](#no-partial-configs)
- [Tuned Rules](#tuned-rules)
- [Nested Function Naming (FL0081) and Tail-Call Diagnostics (FL0085)](#nested-function-naming-fl0081-and-tail-call-diagnostics-fl0085)
- [FL0069 Quirk](#fl0069-quirk)
- [See Also](#see-also)

## Version

The project uses a custom `fsharplint.json` at the repository root.

**Version**: pinned in `dotnet-tools.json` (single source of truth) — currently the
`0.27.1--date20260902-0344.git-d2607f5` pre-release, invoked as `dotnet fsharplint`.
Install/refresh with `dotnet tool restore` (see `tools/README.md`). Do not install
FSharpLint globally or unpinned — local and CI must run the same version.

## Timing

Lint is slow by design of FCS — every file is re-type-checked against the full contents
of the referenced F# assemblies (FSharpPlus ~11 MB dominates): ~24 s/file on
`tests/FLPQ.Printers.Tests` (~17 min for 42 files), ~26 min for the full solution
(205 files) on an 8-core machine — expect ~40–60 min on a 4-core CI runner. This is
environmental, not a hang — see issue #1. Never run two lints in parallel (CPU
contention makes them 2–3× slower).

## No Partial Configs

**Critical**: FSharpLint does **not** support partial configs — the file must contain
the **full** default configuration with only the desired rules changed. The reference
default config lives at:

```
https://raw.githubusercontent.com/fsprojects/FSharpLint/master/src/FSharpLint.Core/fsharplint.json
```

## Tuned Rules

The following rules were changed from FSharpLint defaults to match the project code
style (as documented in the coding conventions):

| Rule | Setting | Default | Reason |
| --- | --- | --- | --- |
| `genericTypesNames` (FL0069) | `naming: CamelCase` | PascalCase | Project uses `'t`, `'nt`, `'a`, `'s`, `'v` |
| `nestedFunctionNames` (FL0081) | `enabled: true`, `naming: CamelCase` | disabled, PascalCase | Project uses camelCase for `let rec loop`/`derive` etc. |
| `recordFieldNames` (FL0039) | `naming: PascalCase` | PascalCase | Per coding conventions: "PascalCase for record fields and union case fields" |

Rule IDs verified against the pinned version's source (`src/FSharpLint.Core/Rules/Identifiers.fs`): FL0067 = `privateValuesNames`, FL0069 = `genericTypesNames`, FL0081 = `nestedFunctionNames`, FL0085 = `ensureTailCallDiagnosticsInRecursiveFunctions`.

## Nested Function Naming (FL0081) and Tail-Call Diagnostics (FL0085)

Enabling `nestedFunctionNames` makes **FL0081** report local function names not matching
the configured convention (`CamelCase`). A separate rule,
`ensureTailCallDiagnosticsInRecursiveFunctions` (**FL0085**), reports any `let rec`
function (local or top-level) missing the `[<TailCall>]` attribute. Both fire on the same
code, which is why enabling the naming rule initially looked like "dual behavior". The
project keeps FL0081 active and sets `ensureTailCallDiagnosticsInRecursiveFunctions` to
`false` to suppress only the tail-call diagnostics.

## FL0069 Quirk

`genericTypesNames` (FL0069) only checks generic parameters written with a quote
(`type Foo<'t>`); unquoted parameters (`type Foo<t>`) are not checked. Verified identical
on 0.27.0 and the pinned pre-release — not a regression.

## See Also

- [Quality Standards](quality-standards.md)
- [Coding Conventions](coding-conventions.md)
- [Tools](tools.md)
