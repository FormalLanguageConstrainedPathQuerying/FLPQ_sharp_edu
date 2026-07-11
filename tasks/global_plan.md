# Global Plan: Tasks 165–166

## Task Summary

| ID | Description | Type | Dependencies |
|----|-------------|------|--------------|
| 165 | Fix fsharplint invocation in `tools/hard_gate.py` — wrong binary path | Tooling fix | None |
| 166 | Fix RNGLR SPPF DOT visualization — missing terminals and orphan range nodes | Code fix + Tests | None |

## Dependencies Graph

```
Task 165 → (independent, tooling fix)
Task 166 → (independent, SPPF DOT bug fix)
```

## Execution Order

1. **Task 165** — Fix fsharplint path (quick fix)
2. **Task 166** — Fix RNGLR SPPF DOT visualization

## Rationale

- Both tasks are independent
- Task 165 is a quick tooling fix — do it first
- Task 166 requires investigation of SPPF DOT generation for RNGLR

## Conflict Analysis

- **165 vs 166**: Task 165 modifies `tools/hard_gate.py` only. Task 166 touches SPPF printing and possibly RNGLR code. No conflicts.
- Task 165 changes are in `tools/` directory (Python). Task 166 changes are in `src/` and `tests/` (F#). No overlap.

## Shared Infrastructure

None — tasks are fully independent.
