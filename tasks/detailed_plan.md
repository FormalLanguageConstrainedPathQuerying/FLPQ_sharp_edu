# Detailed Plan: Task 132 — LL(k>1) Tests, Empty-Input, Test Moves

## Steps

### 1. Add LL(k>1) parsing tests (LLParserTests.fs)
- Create a grammar that requires k=2 lookahead (e.g., S->aAa | aBb, A->x, B->y)
- Test that k=1 fails (conflict at table build)
- Test that k=2 succeeds and correctly parses
- Add a second grammar requiring k>1 lookahead

### 2. Add modified Valiant empty-input test (ValiantTests.fs)
- Test `parseModified` with empty input string
- Verify it returns correct acceptance for grammar with/without epsilon

### 3. Move 4 NFA/DFA backward-compatibility tests
- From `tests/FLPQ.GraphAnalysis.Tests/GraphTests.fs` (lines 49-67)
- To `tests/FLPQ.Languages.Tests/AutomatonTests.fs`
- Tests: NFA.states, NFA.transitions, DFA.states, DFA.transitions

### 4. Remove FLPQ.Languages reference from GraphAnalysis.Tests
- Edit `tests/FLPQ.GraphAnalysis.Tests/FLPQ.GraphAnalysis.Tests.fsproj`

### 5. Verify CliSummaryTests.fs location
- Already in FLPQ.Cli.Tests (confirmed)
