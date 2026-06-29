# Global Plan: Tasks 40—47

## Task Summary

| ID | Description | Type |
|----|-------------|------|
| 40 | Make CYK and Valiant generic over 't, 'nt | Refactoring |
| 41 | Deduplicate buildLR0Table/buildSLR1Table/buildCLR1Table | Refactoring |
| 42 | Valiant must use boolean decomposition instead of explicit set of matrices | Refactoring |
| 43 | Deduplicate checkDotCompiles/checkDotCompilesWithInfo | Refactoring |
| 44 | Refactor LR0Item/LR1Item to camelCase fields | Refactoring |
| 45 | Deduplicate LRParserTests.fs submodules | Refactoring |
| 46 | Check documentation is up to date | Docs |
| 47 | Update code_review.md: remove resolved problems | Docs |

## Dependencies

```
40 (CYK/Valiant generics) ── 42 (Valiant bool decomp)
                                      │
43 (TestUtils dedup) ─────────────────┤ independent
                                      │
41 (LR table dedup) ── 44 (LRItem camelCase) ── 45 (LRParserTests dedup)
                                      │
                                      46 (Docs check) ── 47 (Update code review)
```

- **40 → 42**: Valiant must be generic before switching to BooleanDecomposition
- **41 → 44 → 45**: All touch LRParser.fs and LRParserTests.fs — must be sequential
- **43 independent**: only touches TestUtils.fs
- **46 → 47**: docs check first, then update code review

## Potential Conflicts

| Task | Files Modified | Conflicts With |
|------|---------------|----------------|
| 40 | Cyk.fs, Valiant.fs, Tokenizer.fs, test files | 42 |
| 41 | LRParser.fs | 44 |
| 42 | Valiant.fs, BooleanDecomposition.fs | 40 |
| 43 | TestUtils.fs | None |
| 44 | LRParser.fs, test files | 41, 45 |
| 45 | LRParserTests.fs | 44 |
| 46 | docs/*.md | None (read-only) |
| 47 | tasks/code_review.md | None |

## Execution Order

1. **Task 40** — Make CYK and Valiant generic over 't, 'nt
2. **Task 42** — Valiant uses BooleanDecomposition
3. **Task 43** — Deduplicate TestUtils checkDotCompiles
4. **Task 41** — Deduplicate LRParser table builders
5. **Task 44** — LR0Item/LR1Item camelCase fields
6. **Task 45** — Deduplicate LRParserTests.fs submodules
7. **Task 46** — Check documentation
8. **Task 47** — Update code_review.md
