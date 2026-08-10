## Task 243: Add tikz rendering for BasicSPPF

### S1: Create BasicSppfTikz module

**Code:** New file `src/FLPQ.Printers/BasicSppfTikz.fs`. Update `src/FLPQ.Printers/FLPQ.Printers.fsproj` to add after `BasicSppfDot.fs`.
**Tests:** Basic golden tests: create simple BasicSPPF, verify tikz compiles with lualatex.
**Docs:** None.

**Spec:**
- Module `BasicSppfTikz` in namespace `FLPQ.Printers`
- Function `toTikz : terminalPrinter -> nonterminalPrinter -> BasicSPPF<'t,'nt> -> string`
- Use `\graph [layered layout, nodes={draw}, grow'=down, level sep=1.5cm, sibling sep=1.0cm]` (same as SppfTikz)
- Node shapes: Terminal→circle, Nonterminal→rectangle, Epsilon→circle, Production→oval
- Reuse `AutomatonTikz.escapeLatex` and `AutomatonTikz.tikzFooter`
- Root node: `fill=green!30`
- Edges are unlabeled (same as BasicSppfDot)

### S2: Wire useDot into CYK/Valiant runners

**Code:** `src/FLPQ.Cli/CykRunner.fs`, `src/FLPQ.Cli/ValiantRunner.fs`, `src/FLPQ.Cli/Program.fs`
**Tests:** Existing CLI tests + golden tests from S1.
**Docs:** None.

**Spec:**
- Add `useDot: bool` parameter to `CykRunner.runCyk`, `ValiantRunner.runValiant`, `ValiantRunner.runValiantModified`
- When `not useDot` (default): write `sppf.tikz.tex` using `BasicSppfTikz.toTikz` (do NOT write `sppf.dot`)
- When `useDot`: write `sppf.dot` using `BasicSppfDot.toDot`
- Update `Program.fs` to pass `useDot` to CYK/Valiant/ValiantModified

### S3: Update Summary for BasicSPPF TikZ

**Code:** `src/FLPQ.Printers/SummaryTeX.fs`
**Tests:** None (existing golden summary tests cover this).
**Docs:** None.

**Spec:**
- Modify `sppfDotSection` to `sppfSection` accepting `useTikz: bool`
- When `useTikz`: read `sppf.tikz.tex`, use `wrapTikzCenter`
- When not `useTikz`: read `sppf.dot`, use `includePdf` (existing behavior)
- Update call site in `buildContent` to pass `useTikz`
