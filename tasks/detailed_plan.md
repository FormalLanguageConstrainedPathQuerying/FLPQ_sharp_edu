# Detailed Plan: Task 130 — LR Conflict Behavior Tests

## Goal
Add tests verifying LR conflict detection and reporting.

## Changes
All changes in `tests/FLPQ.Languages.Tests/LRParserTests.fs`

1. Test specific conflict types (ShiftReduce vs ReduceReduce) for known grammars
2. Test "reduce on everything" LR(0) produces predictable shift-reduce conflicts
3. Verify conflict count for ambiguous and non-LR grammars
4. Test that visualization references correspond to actual table conflicts

## Implementation
- Add `ConflictBehaviorTests` module to LRParserTests.fs
- No new source code needed — tests only
