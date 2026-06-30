# Detailed Plan: Task 65 — Refactoring

## Overview

Refactor 7 code quality issues: unify input types across parsing algorithms, remove code duplication in LRParser, Valiant, CLI, fix broken property test, and add missing tests.

## Sub-tasks

### 1. Unify parsing algorithm input types (Valiant: `'t list` → `Symbol<'t,'nt> list`)

**Current state**:
- CYK, LL, LR accept `Symbol<'t, 'nt> list` (tokenized via `Tokenizer.tokenize`)
- Valiant alone accepts raw `'t list` (tokenized via `Tokenizer.tokenizeStrings`)

**Plan**:
- In `Valiant.fs`, extract terminals from `Symbol<'t, 'nt> list` at the entry point:
  ```fsharp
  let private extractTerminals (tokens: Symbol<'t, 'nt> list) : 't list =
      tokens |> List.choose (function T(Terminal t) -> Some t | _ -> None)
  ```
- Change all 6 public Valiant functions to accept `Symbol<'t, 'nt> list`
- Internally call `extractTerminals` to get `'t list` for existing logic
- Update `Tokenizer.fs`: remove `tokenizeStrings` (no longer needed)
- Update all test files calling Valiant: replace `Tokenizer.tokenizeStrings s` with `Tokenizer.tokenize s`
  - `ValiantTests.fs` — all calls
  - `Program.fs` — `runValiant`
- Update `docs/valiant.md` if it mentions the old type

### 2. Remove duplicated LR0/LR1 state-exploration logic in LRParser.fs

**Current state**: `buildLR0` (lines 164-194) and `buildLR1` (lines 228-257) are structurally identical except for `gotoLR0` vs `gotoLR1` and the final-state check.

**Plan**:
- Extract common logic into a private helper:
  ```fsharp
  let private buildAutomaton
      (aug: Grammar<'t, 'nt>)
      (startItems: 'items)
      (gotoFn: ('items -> Symbol<'t, 'nt> -> 'items))
      (isAcceptItem: 'items -> bool)
      : DFA<Symbol<'t, 'nt>, 'items>
  ```
- The helper:
  1. Takes computed start items (already closure-applied)
  2. Takes a goto function (gotoLR0 or gotoLR1 wrapper)
  3. Takes a predicate to identify accept items
  4. Runs the BFS state exploration loop
  5. Builds and returns the DFA
- `buildLR0` and `buildLR1` become thin wrappers that compute start items, then call `buildAutomaton`

### 3. Fix MsBfsTests.fs property test, create common random graph generator

**Current state**: The `[<Property>]` test at line 126 uses `System.Random.Shared` in a `for` loop rather than FsCheck generators.

**Plan**:
- Create a proper FsCheck `Arbitrary<Matrix<bool>>` generator for random graphs:
  - Generate small boolean matrices (n=1..6 vertices, density ~20-30%)
  - Ensure self-loops are avoided (no (i,i) edges) for clean BFS semantics
- Add to existing `MsBfsTests.fs` (or create as a generator type like in MatrixTests)
- Rewrite the property test to use the generator: `let ``msBfs equals independent single-source BFS`` (m: Matrix<bool>, sources: int[]) = ...`
- The graph generator should be in `MsBfsTests.fs` for now (it's simple enough). If needed for RPQ tests later, it can be moved.
- Generate sources as a non-empty array of valid vertex indices

### 4. Add grammars 7 and 8 to ValiantTests

**Current state**: Valiant tests cover grammars 1-6 but not 7-8.

**Plan**:
- Add property-based tests for grammar 7:
  - `Valiant and CYK agree on acceptance for grammar 7` (use AbStringGenerators with spaces, or create ExprStringGenerators-like generator for grammar 7/8 language)
  - `Valiant and CYK tables match for grammar 7`
  - `Modified Valiant and standard Valiant agree on acceptance for grammar 7`
  - `Modified Valiant and standard Valiant tables match for grammar 7`
- Add same set for grammar 8
- Reuse `ExprStringGenerators` since grammar 6, 7, 8 define the same language

### 5. Combine `writeDotFile`/`writeTexFile` in Program.fs

**Plan**:
- Replace two functions with one:
  ```fsharp
  let private writeOutputFile path content =
      let dir = Path.GetDirectoryName path
      if not (Directory.Exists dir) then
          Directory.CreateDirectory dir |> ignore
      File.WriteAllText(path, content)
  ```
- Update all call sites (2 in `writeStepsVisualization`, 4 in `runCyk`, `runValiant`)

### 6. Merge `complete`/`completeTrace` and `compute`/`computeTrace` in Valiant.fs

**Current state**: `parseWithTrace` has ~80 lines copied from `complete` plus trace recording.

**Plan**:
- Keep only the tracing variants internally. The non-tracing `parse` and `parseWithTable` can use the same algorithm but with an `option` of trace accumulator (or just ignore it).
- Actually a cleaner approach: make `complete` and `compute` always collect steps into an optional `ResizeArray<ValiantTraceStep<'nt>>` parameter. When it's `None`, no tracing happens.
  ```fsharp
  let rec private complete
      ...
      (traceAcc: ResizeArray<ValiantTraceStep<'nt>> option)
      ...
  ```
- When `traceAcc` is `Some`, recompose and add a step after each submatrix completion.
- `parseWithTable` passes `None`, `parseWithTrace` passes `Some(resizeArray)`.
- Remove `completeTrace` and `computeTrace` entirely.

### 7. Extract duplicated Valiant init block

**Current state**: ~25 lines of init code repeated 4 times (lines 310-332, 383-404, 520-542, 600-622).

**Plan**:
- Create a private record type or tuple to hold initialization results:
  ```fsharp
  type private InitData<'t, 'nt> = {
      tByNt: Dictionary<Nonterminal<'nt>, Matrix<bool>>
      pByPair: Dictionary<Nonterminal<'nt> * Nonterminal<'nt>, Matrix<bool>>
      tokensArr: 't[]
      tableSize: int
      n: int
      allNt: Nonterminal<'nt> list
      binaryRules: (Nonterminal<'nt> * (Nonterminal<'nt> * Nonterminal<'nt>)) list
      pairs: (Nonterminal<'nt> * Nonterminal<'nt>) list
      terminalRules: Map<'t, Nonterminal<'nt> list>
  }
  ```
- Create `let private initValiant (tokens: Symbol<'t,'nt> list) (cnf: Grammar<'t,'nt>) : InitData = ...`
- Replace all 4 instances with a single call.

## Execution Order

1. Subtask 7 (extract init block) — foundation for subtask 6
2. Subtask 6 (merge complete/completeTrace) — simpler after init extraction
3. Subtask 1 (unify input types) — main API change
4. Subtask 2 (LR parser dedup) — independent
5. Subtask 3 (MsBfsTests property) — independent
6. Subtask 4 (Valiant tests grammars 7-8) — depends on subtask 1
7. Subtask 5 (CLI dedup) — independent, very small

## Files to Modify

| File | Sub-tasks |
|------|-----------|
| `src/FLPQ.Languages/Valiant.fs` | 1, 6, 7 |
| `src/FLPQ.Languages/Tokenizer.fs` | 1 |
| `src/FLPQ.Languages/LRParser.fs` | 2 |
| `src/FLPQ.Cli/Program.fs` | 1, 5 |
| `tests/FLPQ.Languages.Tests/ValiantTests.fs` | 1, 4 |
| `tests/FLPQ.GraphAnalysis.Tests/MsBfsTests.fs` | 3 |
| `tests/FLPQ.Languages.Tests/LRParserTests.fs` (if exists) | 2 |
| `docs/valiant.md` | 1, 6, 7 |
| `docs/lr-parser.md` | 2 |

## Verification

- `dotnet fantomas .` — format
- `dotnet build -c Release` — compile (0 warnings)
- `dotnet test` — all tests pass
