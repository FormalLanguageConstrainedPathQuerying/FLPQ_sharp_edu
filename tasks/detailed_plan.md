# Detailed Plan: Task 84 — Fix grammar rendering in TeX

## Goal

Fix broken arrow (`\rightarrow`) in grammar TeX rendering. The `\r` in `\rightarrow` is interpreted as carriage return in non-verbatim F# string literals, producing corrupted output.

## Root Cause

File: `src/FLPQ.Printers/GrammarTeX.fs:32`
```fsharp
sb.AppendLine(sprintf "%s &\rightarrow %s \\\\" lhs rhs) |> ignore
```

In a regular (non-verbatim) F# string `""`, `\r` is the carriage return escape sequence. So `\rightarrow` becomes `<CR>ightarrow`.

## Fix

Change to verbatim string:
```fsharp
sb.AppendLine(sprintf @"%s &\rightarrow %s \\" lhs rhs) |> ignore
```

Note: In verbatim strings `@""`, `\\` represents two literal backslashes. The original `\\\\` becomes `\\` (since `\\` in a non-verbatim string is one literal backslash, so `\\\\` is two literal backslashes = `\\` in TeX). In a verbatim string, `\\` is two literal backslashes, which is `\\` in TeX — which is the correct TeX line ending.

## Verification

- The generated TeX should show `S &\rightarrow S\ S \\` instead of `S &` + `<CR>` + `ightarrow S\ S \\`
- Run existing tests to ensure no regressions
