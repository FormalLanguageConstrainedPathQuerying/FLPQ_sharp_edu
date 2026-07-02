# Detailed Plan: Task 88 — Fix nonterminals rendering in TeX

## Goal

Generate unique `N_i`-style names directly during CNF conversion instead of using regex to reformat `N_CNF_{i}` names in three separate printer modules.

## Changes

### 1. `src/FLPQ.Languages/Grammar.fs` (line 385)
- Change `freshStringNonterminal` from `$"N_CNF_{i}"` to `$"N_{i}"`
- The index `i` is already unique (counter increments per call in `toCnf`)

### 2. `src/FLPQ.Printers/ValiantTeX.fs` (lines 10–11)
- Remove `shortNtName` function with regex
- Replace uses with `string n` directly
- Remove `System.Text.RegularExpressions` import (if no longer used)

### 3. `src/FLPQ.Printers/GrammarTeX.fs` (lines 10–11)
- Remove `shortNtName` function with regex
- Replace uses with `string n` directly  
- Remove `System.Text.RegularExpressions` import

### 4. `src/FLPQ.Printers/CykTeX.fs` (lines 11–12)
- Remove `shortNtName` function with regex
- Replace `shortNtName nt` with `string nt` in `shortSymbolPrinter`
- Remove `System.Text.RegularExpressions` import

## Verification
- No remaining regex `N_CNF_` patterns in source
- All tests pass
- Check formatting
