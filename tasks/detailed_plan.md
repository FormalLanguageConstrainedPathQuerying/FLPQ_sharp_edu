# Detailed Plan: Task 241 — Improve CYK and Valiant Table Rendering

## Overview

Switch CYK, Valiant, and Modified Valiant table rendering from `(nonterminal)` format to `(nonterm, split_point, prod_id)` format using existing `SppfParsingTable` and `sppfEntryCellToTeX`.

## Subtasks

### S1: Add SPPF-aware trace types and collection functions for CYK

**Code:**
- `src/FLPQ.Languages/Cyk.fs` — add `CykSppfTraceStep` type and `parseWithSppfTrace` function
**Tests:** Test that SPPF trace produces identical nonterminal sets to regular trace
**Docs:** None (internal type extension)

**Spec:**
- `CykSppfTraceStep<'nt>` = `{ Table: SppfParsingTable<'nt>; Highlights: Matrix.Highlight list }`
- `parseWithSppfTrace` mirrors `parseWithTrace` but uses `cykSppfCore` and tracks highlights
- Collects table snapshots at each length boundary

### S2: Add SPPF-aware trace types and collection for Valiant

**Code:**
- `src/FLPQ.Languages/Valiant.fs` — add `ValiantSppfTraceStep` and `ModifiedValiantSppfTraceStep` types, `parseWithSppfTrace` and `parseModifiedWithSppfTrace` functions
**Tests:** Idem
**Docs:** None

**Spec:**
- `ValiantSppfTraceStep<'nt>` = `{ Table: SppfParsingTable<'nt>; Target: Submatrix; Multiplied: (Submatrix * Submatrix) list; ChangedCells: (int * int) list }`
- `ModifiedValiantSppfTraceStep<'nt>` = same DU as modified but with SppfParsingTable
- `parseWithSppfTrace` and `parseModifiedWithSppfTrace` collect SPPF table snapshots

### S3: Update rendering functions and CLI runners

**Code:**
- `src/FLPQ.Printers/CykTeX.fs` — add `sppfTableToTeXStyled`
- `src/FLPQ.Printers/ValiantTeX.fs` — add `sppfStepToTeX` and `sppfModifiedStepToTeX`
- `src/FLPQ.Cli/CykRunner.fs` — use SPPF trace
- `src/FLPQ.Cli/ValiantRunner.fs` — use SPPF trace
**Tests:** Golden test updates
**Docs:** None

**Spec:**
- New rendering functions use `ParsingTableTeX.sppfEntryCellToTeX` instead of `ntCellToTeX`
- Runners call SPPF trace functions instead of regular trace functions
- Golden files may need regeneration
