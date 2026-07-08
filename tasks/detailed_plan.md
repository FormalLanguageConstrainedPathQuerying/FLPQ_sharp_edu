# Task 148: Fix problems detected by linter

## Summary of warnings (from tmp/fsharplint-output.txt)

### By rule:
- **FL0039** - Record field naming (camelCase → PascalCase): ~130 warnings across 15+ files
- **FL0085** - Tail call diagnostics (rec without [<TailCall>]): ~25 warnings across 10 files
- **FL0045** - Member naming (camelCase → PascalCase): 5 warnings (Automaton.fs x4, SummaryTeX.fs x1)
- **FL0034** - Lambda can be replaced with composition: 3 warnings (Graph.fs, MsBfs.fs, BelyaninRPQ.fs)
- **FL0058** - Tuple of wildcards → single wildcard: 2 warnings (Gll.fs)
- **FL0067** - Private value naming (K → k): 3 warnings (Gll.fs x1, Rnglr.fs x2)

### By file (src only):
| File | FL0039 | FL0085 | FL0045 | FL0034 | FL0058 | FL0067 | Total |
|------|--------|--------|--------|--------|--------|--------|-------|
| Matrix.fs | 11 | 0 | 0 | 0 | 0 | 0 | 11 |
| Graph.fs | 2 | 0 | 0 | 1 | 0 | 0 | 3 |
| Grammar.fs | 6 | 4 | 0 | 0 | 0 | 0 | 10 |
| Automaton.fs | 8 | 0 | 4 | 0 | 0 | 0 | 12 |
| RSM.fs | 12 | 0 | 0 | 0 | 0 | 0 | 12 |
| FirstFollow.fs | 0 | 1 | 0 | 0 | 0 | 0 | 1 |
| EbnfParser.fs | 0 | 9 | 0 | 0 | 0 | 0 | 9 |
| DerivationTree.fs | 0 | 2 | 0 | 0 | 0 | 0 | 2 |
| Cyk.fs | 2 | 0 | 0 | 0 | 0 | 0 | 2 |
| Valiant.fs | 12 | 4 | 0 | 0 | 0 | 0 | 16 |
| LLParser.fs | 7 | 1 | 0 | 0 | 0 | 0 | 8 |
| LRParser.fs | 13 | 0 | 0 | 0 | 0 | 0 | 13 |
| GllTypes.fs | 15 | 0 | 0 | 0 | 0 | 0 | 15 |
| Gll.fs | 4 | 3 | 0 | 0 | 2 | 1 | 10 |
| RnglrTypes.fs | 10 | 0 | 0 | 0 | 0 | 0 | 10 |
| RnglrLR.fs | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Rnglr.fs | 3 | 2 | 0 | 0 | 0 | 2 | 7 |
| MsBfs.fs | 0 | 0 | 0 | 1 | 0 | 0 | 1 |
| VisualizationTypes.fs | 2 | 0 | 0 | 0 | 0 | 0 | 2 |
| SummaryTeX.fs | 0 | 0 | 1 | 0 | 0 | 0 | 1 |
| ExternalTools.fs | 4 | 0 | 0 | 0 | 0 | 0 | 4 |
| DerivationTreeDot.fs | 0 | 3 | 0 | 0 | 0 | 0 | 3 |
| BelyaninRPQ.fs | 0 | 0 | 0 | 1 | 0 | 0 | 1 |
| ArroyueloRPQ.fs | 0 | 1 | 0 | 0 | 0 | 0 | 1 |

## Subtasks

### S1: Simple fixes (FL0034, FL0058, FL0067) - no cascading changes
- FL0034: Replace lambda with operator in Graph.fs:102, MsBfs.fs:30, BelyaninRPQ.fs:33
- FL0058: Replace tuple wildcards in Gll.fs:578, Gll.fs:590
- FL0067: Rename K → k in Gll.fs:92, Rnglr.fs:56, Rnglr.fs:288

### S2: FL0045 Member naming fixes
- Automaton.fs: states → States, transitions → Transitions (x2 for NFA and DFA)
- SummaryTeX.fs: toString → ToString

### S3: FL0085 Tail call diagnostics
For each recursive function, either:
- Add `[<TailCall>]` if the function IS tail-recursive
- Refactor to be tail-recursive with accumulator
- If NOT tail-recursive and cannot be made so (tree traversal returning values), add `<WarningsAsErrors>FS3569</WarningsAsErrors>` to project files

### S4: FL0039 Record field naming - LinearAlgebra & GraphAnalysis
- Matrix.fs, Graph.fs, MsBfs.fs (foundational types, many dependents)

### S5: FL0039 Record field naming - Languages core
- Grammar.fs, Automaton.fs, RSM.fs, FirstFollow.fs, EbnfParser.fs, DerivationTree.fs

### S6: FL0039 Record field naming - Parsing algorithms
- Cyk.fs, Valiant.fs, LLParser.fs, LRParser.fs

### S7: FL0039 Record field naming - GLL/RNGLR
- GllTypes.fs, Gll.fs, RnglrTypes.fs, Rnglr.fs, RnglrLR.fs

### S8: FL0039 Record field naming - Printers & RPQ
- VisualizationTypes.fs, SummaryTeX.fs, ExternalTools.fs, BelyaninRPQ.fs

### S9: Update AGENTS.md and CI
- Add linter instructions to AGENTS.md
- Add linter step to CI workflow

## Progress
- S1: pending
- S2: pending
- S3: pending
- S4: pending
- S5: pending
- S6: pending
- S7: pending
- S8: pending
- S9: pending
