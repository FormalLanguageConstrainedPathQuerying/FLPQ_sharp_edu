# Detailed Plan: Task 40 — Make CYK and Valiant generic over 't, 'nt

## Changes

### 1. Tokenizer.fs
Add a generic tokenizer:
```fsharp
val tokenizeGen: (string -> Symbol<'t, 'nt>) -> string -> Symbol<'t, 'nt> list
```
Keep existing string-specific `tokenize` as a specialized call.

### 2. Cyk.fs
Change all functions from hardcoded `string, string` to generic `'t, 'nt`:
- `CykCell` → `Option<HashSet<Symbol<'t, 'nt>>>`
- `parse`, `parseWithTable`, `parseWithTrace`, `tableToTeX`, `tableToTeXStyled`
- Internal helpers: `findProducingRules`, `findBinaryProductions`, `cykTable`, `tableTrace`, `isAccepted`
- Accept a tokenizer function parameter

### 3. Valiant.fs  
Same generic transformation:
- All functions: `'t, 'nt` instead of `string, string`
- Accept a tokenizer function parameter

### 4. FLPQ.Cli/Program.fs
Update calls to generic CYK/Valiant: pass `Tokenizer.tokenize` as tokenizer.

### 5. Tests
Most tests should work without changes since they use `Grammar.parseGrammar` (returns `Grammar<string,string>`) and type inference handles the rest. Some tests may need explicit type annotations.

## Files

| File | Action |
|------|--------|
| `src/FLPQ.Languages/Tokenizer.fs` | Add generic tokenizer |
| `src/FLPQ.Languages/Cyk.fs` | Generic 't, 'nt |
| `src/FLPQ.Languages/Valiant.fs` | Generic 't, 'nt |
| `src/FLPQ.Cli/Program.fs` | Update calls |
