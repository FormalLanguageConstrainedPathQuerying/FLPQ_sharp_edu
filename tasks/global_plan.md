# Global Plan: Tasks 50—51

## Task Summary

| ID | Description | Type |
|----|-------------|------|
| 50 | Add LL(k) parsing table visualization to TeX | Feature |
| 51 | Add LR(0), SLR(1), CLR(1) parsing table visualization to TeX | Feature |

## Dependencies

```
50 (LL table TeX)     independent
51 (LR table TeX)     independent
```

Both tasks are independent — they modify different modules (LLParser vs LRParser) and add separate test functions. No shared code beyond TeX compilation test infrastructure.

## Potential Conflicts

| Task | Files Modified | Conflicts With |
|------|---------------|----------------|
| 50 | LLParser.fs, TexCompilationTests.fs or new test file | None |
| 51 | LRParser.fs, TexCompilationTests.fs or new test file | None (touches different functions) |

## Shared Infrastructure

Both tasks use the existing `TestUtils.checkTexCompiles` and the TeX template in `tests/.../tex_template.tex`.

## Execution Order

1. **Task 50** — LL(k) table TeX visualization
2. **Task 51** — LR(0)/SLR(1)/CLR(1) table TeX visualization
