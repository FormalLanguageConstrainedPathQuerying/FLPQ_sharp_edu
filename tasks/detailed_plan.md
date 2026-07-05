# Detailed Plan: Task 110 — CLI: Clean Output Directory

## Problem

The CLI currently writes output to the specified directory without checking if it's empty. If the user re-runs the CLI with the same output directory, old files from a previous run may remain, causing confusion or stale artifacts.

## Design

Add a `cleanOutputDir` function to `Helpers.fs`:
- If directory does not exist → create it
- If directory exists and is empty → do nothing
- If directory exists and has content → delete and recreate it

Call this function from `Program.fs` before dispatching to algorithm runners.

## Files to Modify

1. `src/FLPQ.Cli/Helpers.fs` — add `cleanOutputDir` function
2. `src/FLPQ.Cli/Program.fs` — call `cleanOutputDir` before algorithm dispatch

## Implementation

```fsharp
let cleanOutputDir (dir: string) =
    if Directory.Exists dir then
        if Directory.GetFileSystemEntries(dir).Length > 0 then
            Directory.Delete(dir, true)
            Directory.CreateDirectory dir |> ignore
    else
        Directory.CreateDirectory dir |> ignore
```
