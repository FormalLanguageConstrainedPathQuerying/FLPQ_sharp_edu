# Detailed Plan: Task 273 — SPPF TikZ (GLL/RNGLR): subscript indices in range and intermediate nodes

## Task description (verbatim)

273. For GLL and RNGLR Tikz-based SPPF rendering use down indices in the following nodes. For intermediate nodes use 'I\_{m,p}' instead of 'I(m,p)'. For ranges in itermediate and range nodes use '[s_i,v_j] \\to [s_k,v_l]' instead of '[si,vj] \\to [sk,vk]'

## Scope decisions

- "GLL and RNGLR Tikz-based SPPF rendering" = `SppfTikz.toTikz`
  (`src/FLPQ.Printers/SppfTikz.fs:11`). It is the single TikZ SPPF renderer for both
  algorithms: `GllRunner.fs:56` and `RnglrRunner.fs:57` are its only call sites.
  Changing it once covers GLL and RNGLR.
- "down indices" = subscript (math-mode) indices. `_` is invalid in LaTeX text mode,
  so the subscripted parts must live inside math spans:
  - range node label: `$[s_{i},v_{j}]\to[s_{k},v_{l}]$` (one math span; today it is
    `[si,vj]$\to$[sk,vk]` with only the arrow in math mode)
  - intermediate node label: `$I_{m,p}$ @ $[s_{i},v_{j}]\to[s_{k},v_{l}]$`
    (`@` stays in text mode between two math spans; today it is `I(m,p) @[si,vj]$\to$[sk,vk]`)
- Braces around the numeric index (`s_{12}`, not `s_12`) so multi-digit positions
  subscript correctly.
- Out of scope: `SppfDot.fs` (DOT renderer — plain-text labels with Unicode arrow,
  different format by design), `BasicSppfTikz.fs` (CYK/Valiant basic SPPF — different
  node types, no range/intermediate nodes), all per-step GSS/RSM/input figures.
- No public API change: same function signatures, only the produced label strings
  change.

## Reuse analysis (reusing skill checklist)

- Q1/Q3: no new module, type, or helper needed beyond a local `rangeLabel` function
  inside `toTikz` — the range notation is duplicated in the `SppfRange` and
  `SppfIntermediate` match arms; extracting one local keeps a single source of truth
  for the range format (Q5).
- Q2: no developer doc describes the SppfTikz label format (verified by grep over
  `docs/`; task 269 changed these same labels without docs), so no doc update is
  required. No API change → no hub/architecture doc update.
- Tests reuse the existing SPPF construction pattern from
  `tests/FLPQ.Printers.Tests/TexCompilationTests.fs:820-827` (ANBN "classic" grammar
  `S -> a S b | eps`, input `aabb`, GLL path index → `Sppf.buildSppfFromExtendedRsm`).
  That SPPF contains both node kinds: range nodes (root + packed alternatives) and
  intermediate nodes (four, from the two `a S b` derivations).

## Subtasks

### S1: Render SPPF range/intermediate labels with subscript indices [done — d65fb8b]

**Code:** `src/FLPQ.Printers/SppfTikz.fs` — in `toTikz`:

- add local `rangeLabel fs fp ts tp = sprintf "$[s_{%d},v_{%d}]\\to[s_{%d},v_{%d}]$" fs fp ts tp`
- `SppfRange` arm → `rangeLabel fs fp ts tp`
- `SppfIntermediate` arm → `sprintf "$I_{%d,%d}$ @ %s" s p (rangeLabel fs fp ts tp)`

**Tests:** `tests/FLPQ.Printers.Tests/TexCompilationTests.fs:833` — the assertion
`Assert.Contains(@"$\to$", tikz)` is stale after this change (the arrow now sits
inside a larger math span, no standalone `$\to$`). Replace it with assertions for
the new format: `Assert.Contains(@"$[s_{", tikz)` and
`Assert.Contains(@"\to[s_{", tikz)`. The same test compiles the output with lualatex,
which verifies the math mode is well-formed end-to-end.

**Docs:** none — no doc describes this label format; no API change (see scope/reuse).

**Spec:**

- Range node label renders as `$[s_{i},v_{j}]\to[s_{k},v_{l}]$` where i,j,k,l are the
  from-state, from-pos, to-state, to-pos of the `SppfRange` node.
- Intermediate node label renders as `$I_{m,p}$ @ $[s_{i},v_{j}]\to[s_{k},v_{l}]$`
  where m,p are the state and position and i,j,k,l the from/to range of the
  `SppfIntermediate` node.
- All other node labels (terminal, nonterminal, epsilon) and edge labels unchanged.
- The whole rendered picture must still compile with lualatex via
  `tex_tikz_template.tex`.

### S2: Add SppfTikz unit tests for the new label format [done — b497bde]

**Code:** none.

**Tests:** new `tests/FLPQ.Printers.Tests/SppfTikzTests.fs` (registered in
`tests/FLPQ.Printers.Tests/FLPQ.Printers.Tests.fsproj` Compile items, after
`BasicSppfDotTests.fs`). Build the SPPF once (ANBN "classic", input `aabb`, same
pattern as `TexCompilationTests.fs:820-827`) and render with
`SppfTikz.toTikz string string`. Facts:

- `range nodes use math-mode subscript indices` — the output matches the regex
  `\$\[s_\{\d+\},v_\{\d+\}\]\\to\[s_\{\d+\},v_\{\d+\}\]\$` (at least one range node).
- `intermediate nodes use I_{m,p} with math-mode subscript indices` — the output
  matches `\$I_\{\d+,\d+\}\$ @ \$\[s_\{\d+\},v_\{\d+\}\]\\to\[s_\{\d+\},v_\{\d+\}\]\$`
  (at least one intermediate node; the aabb SPPF has four).
- `old label formats are absent` — the output contains neither `I(` nor the
  brace-less range form `[s<digit>,v<digit>]` (regex `\[s\d+,v\d+\]`).

**Docs:** none.

**Spec:**

- Tests are plain xunit facts (no TeX category, no lualatex) — fast regression on
  the exact label strings; lualatex compilation is already covered by S1's existing
  test.
- The regexes must be F# verbatim strings with doubled backslashes for the literal
  `\to` and escaped `\$`, `\[`, `\]`, `\{`, `\}` as needed.

## Verification

- `dotnet build` + full test suite (quality gates).
- TeX-category tests compile the new labels with lualatex: `SPPF tikz compiles with lualatex`, GLL and RNGLR merged-summary compilation tests.
