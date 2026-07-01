# Detailed Plan: Task 69 — Terminal list input for all parsing algorithms

## Goal

Change all parsing algorithm inputs from `Symbol<'t,'nt> list` to `Terminal<'t> list`.
Input must not contain nonterminals.

## Changes

### Source files

1. **`src/FLPQ.Languages/Grammar.fs`** (or Tokenizer.fs):
   - Add `Terminal.toSymbols: Terminal<'t> list -> Symbol<'t,'nt> list` helper

2. **`src/FLPQ.Languages/Cyk.fs`**:
   - Change `parse`/`parseWithTable`/`parseWithTrace`: `tokens: Symbol<'t,'nt> list` → `tokens: Terminal<'t> list`
   - Convert to symbols internally

3. **`src/FLPQ.Languages/Valiant.fs`**:
   - Change all public functions: `tokens: Symbol<'t,'nt> list` → `tokens: Terminal<'t> list`
   - Remove `extractTerminals`, use direct list mapping
   - Convert to symbols internally where needed

4. **`src/FLPQ.Languages/LLParser.fs`**:
   - Change `parseWithSteps`/`parse`: `tokens: Symbol<'t,'nt> list` → `tokens: Terminal<'t> list`
   - Convert to symbols internally

5. **`src/FLPQ.Languages/LRParser.fs`**:
   - Change `parseWithSteps`/`parse`: `tokens: Symbol<'t,'nt> list` → `tokens: Terminal<'t> list`
   - Convert to symbols internally

### Test files
Replace `Tokenizer.tokenize` with `Tokenizer.tokenizeTerminals` everywhere.

### CLI
Update `Program.fs` to use `Tokenizer.tokenizeTerminals`.
