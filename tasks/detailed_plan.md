# Detailed Plan: Task 146 — Set FSharpLint up

## Goal
Install FSharpLint dotnet tool, run with default config on the entire solution, and generate a structured report `linter_report.md` grouped by problem types with a brief summary.

## Subtasks
1. [done] Install dotnet-fsharplint as a global dotnet tool — v0.27.0 installed
2. [done] Run `dotnet fsharplint lint FLPQ.slnx` with default config (42 enabled rules), capture output to `/tmp/fsharplint_output.txt` (8,283 lines)
3. [done] Parse the output and generate structured `linter_report.md`:
   - Summary: 82 files linted, 38 clean, 44 with warnings, 1,323 total warnings
   - Grouped by 7 rule types: FL0069 (1,196), FL0039 (111), FL0085 (30), FL0034 (6), FL0045 (5), FL0067 (3), FL0058 (2)
   - Per-file breakdown for all files with warnings
4. [done] Commit linter_report.md
5. [pending] Merge to dev, mark task as [done]
