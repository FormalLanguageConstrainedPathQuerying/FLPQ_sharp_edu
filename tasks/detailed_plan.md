# Task 96: Golden tests for grammar to TeX rendering

## Goal

Create golden (snapshot/reference) tests for grammar-to-TeX rendering.
Generate TeX files for several grammars in BNF and CNF, save them as reference,
create tests that generate TeX for the respective grammar and compare the result
with the reference.

## Grammars to test

1. **grammar1** (BNF): `S -> a S b S | eps` — simple grammar with epsilon
2. **grammar1** (CNF): same grammar converted to Chomsky Normal Form
3. **grammar7** (BNF): expression grammar with E/T/F nonterminals
4. **grammar7** (CNF): same grammar in CNF
5. **grammar9** (BNF): LL(2)-compatible grammar with multiple nonterminals
6. **grammar9** (CNF): same grammar in CNF

Each grammar tested with both `grammarToTeX` (no numbers) and `grammarToTeXWithNumbers` (0-based numbers).

## Design

### Golden files location

`tests/FLPQ.Printers.Tests/GoldenData/grammar_tex_<name>.tex`

Naming convention: `grammar_tex_<grammar>_<variant>.tex`
- `<grammar>`: `grammar1_bnf`, `grammar1_cnf`, `grammar7_bnf`, `grammar7_cnf`, `grammar9_bnf`, `grammar9_cnf`
- `<variant>`: `plain` (no numbers), `numbered` (with numbers)

### Test approach

A helper function `verifyGolden`:
- Computes the golden file path under `GoldenData/`
- If the golden file does NOT exist, writes the generated content to it and fails with a descriptive message ("Golden file created. Review it and re-run tests.")
- If the golden file exists, reads it and compares with generated content via `Assert.Equal`

### Golden file generation workflow

1. Run tests once — golden files are created automatically
2. Review the generated golden files
3. Commit golden files
4. Subsequent test runs compare against golden files

### CNF generation

Use `Grammar.toCnf Grammar.freshStringNonterminal` for string-based grammars.
This produces nonterminals named `N_1`, `N_2`, etc.

## Files to create/modify

- **Create**: `tests/FLPQ.Printers.Tests/GoldenData/` directory
- **Create**: `tests/FLPQ.Printers.Tests/GrammarTeXGoldenTests.fs`
- **Modify**: `tests/FLPQ.Printers.Tests/FLPQ.Printers.Tests.fsproj` — add compile item and content items
- **Update**: `tasks/detailed_plan.md`
- **Update**: `docs/FLPQ.Printers.md` — add note about golden tests

## Steps

1. Create `GoldenData/` directory
2. Write `GrammarTeXGoldenTests.fs` with the golden test infrastructure and test cases
3. Update `FLPQ.Printers.Tests.fsproj`
4. `dotnet build -c Release`
5. `dotnet test --filter GrammarTeXGolden` — first run creates golden files
6. Review generated golden files
7. `dotnet test --filter GrammarTeXGolden` — second run verifies
8. `dotnet fantomas .`
9. `dotnet test` — full test suite passes
10. Commit
