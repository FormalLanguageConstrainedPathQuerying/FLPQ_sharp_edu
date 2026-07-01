# Detailed Plan: Task 71 — MyGen/MyArb cleanup

## Goal

Replace any remaining `System.Random.Shared` usage with FsCheck `MyGen`/`MyArb` in property test generators.

## Investigation

Searched all `.fs` files for `System.Random.Shared` and `System.Random` — no matches found.
The only "Random" references are:
- `RandomGraphGenerators` module name (already uses `MyGen`/`MyArb`)
- `Path.GetRandomFileName()` in test utilities (not property test generation)

## Conclusion

This task was already completed in a previous refactoring (task 65.3). No code changes needed.
