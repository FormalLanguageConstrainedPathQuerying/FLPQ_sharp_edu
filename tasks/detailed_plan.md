# Task 264: Scale LL/LR step TikZ figures with adjustbox instead of resizebox

## Task description (verbatim)

Use `\begin{adjustbox}{max width=\textwidth}...\end{adjustbox}` instead of `\resizebox{0.98\textwidth}{!}{%%` in LL and LR steps visualization to scale tikz figure.

## Context and analysis

### Current behavior

In the merged summary, each LL/LR step's stack-tree figure (`tree_and_stack.tikz.tex`)
is wrapped by `SummaryTeX.stackStepSection` → `wrapTikzCenter`
(`src/FLPQ.Printers/SummaryTeX.fs:219`, helper at `:45-51`), which emits:

```latex
\begin{center}
\resizebox{0.98\textwidth}{!}{%%
<tikz>
}
\end{center}
```

`\resizebox{0.98\textwidth}{!}` scales the figure to **exactly** 0.98\textwidth — small
figures are upscaled and node text/labels are scaled along with the picture, blurring
font metrics. `\begin{adjustbox}{max width=\textwidth}` only shrinks figures wider than
`\textwidth` and never upscales, preserving natural size and font rendering.

### Scope

`wrapTikzCenter` is shared by four call sites:

| Call site | Line | After change |
|-----------|------|--------------|
| `stackStepSection` (LL/LR step figures) | 225 | **adjustbox** (this task) |
| LR automaton header | 168 | resizebox (unchanged) |
| GLL input string | 177 | resizebox (unchanged) |
| SPPF section | 368 | resizebox (unchanged) |

The task is scoped to "LL and LR steps visualization", so only `stackStepSection`
switches. Other call sites keep `wrapTikzCenter` (GLL-wide adjustbox migration is the
separate, still-open task 244).

### Reuse checklist

- No existing adjustbox wrapper for TikZ in `SummaryTeX`. `MatrixTeX.toTeXStyled`
  (`src/FLPQ.Printers/MatrixTeX.fs:164`) uses `\begin{adjustbox}{max width=\textwidth}`
  inline for matrices — different context (math-mode matrix), not reusable as a
  tikzpicture wrapper.
- New helper `wrapTikzAdjustbox` follows the existing `wrap*` naming pattern in
  `SummaryTeX` (`wrapMath`, `wrapCenter`, `wrapTikzCenter`, `wrapTabularResized`).
- The summary preamble already loads `\usepackage{adjustbox}`
  (`data/tex_summary_template.tex:8`) — no template change needed.

## Subtasks

### S1: Wrap LL/LR step figures with adjustbox in merged summary — [done, a5eea65]

**Code:** `src/FLPQ.Printers/SummaryTeX.fs`:
- Add public `wrapTikzAdjustbox : string -> string` — wraps TikZ content in a centered
  `adjustbox` with `max width=\textwidth` (spec below).
- `stackStepSection`: use `wrapTikzAdjustbox` instead of `wrapTikzCenter` for the
  TikZ-mode step picture. DOT mode (`--use-dot`, `\includegraphics`) unchanged.

**Tests:** `tests/FLPQ.Cli.Tests/CliSummaryTests.fs`:
- Extend ``LL summary default mode embeds step stack-trees as inline TikZ``: assert the
  merged TeX contains `\begin{adjustbox}{max width=\textwidth}` and does NOT contain
  `\resizebox{0.98\textwidth}` (the LL summary has no other TikZ figures, so absence is
  a precise check).
- Add ``SLR(1) summary step stack-trees use adjustbox scaling``: run the SLR1 pipeline,
  assert the merged TeX contains `\begin{adjustbox}{max width=\textwidth}` (step
  figures). Only presence is asserted — the LR automaton section legitimately keeps
  `\resizebox{0.98\textwidth}`.
- Existing end-to-end compilation tests (``LL summary merged TeX compiles with
  lualatex``, ``SLR(1) summary merged TeX compiles with lualatex``) verify the new
  wrapper produces compilable output.

**Docs:** `docs/developer/summary-tex.md`:
- Abstract: add `wrapTikzAdjustbox` to the helper list.
- Function signatures: add `val wrapTikzAdjustbox: string -> string`.
- Design decisions: add a row — LL/LR step figures use adjustbox (shrink-only, no
  upscaling, font metrics preserved); other TikZ inclusions keep resizebox.

**Spec:**
- `wrapTikzAdjustbox` output for input `tikz`:
  ```latex
  \begin{center}
  \begin{adjustbox}{max width=\textwidth}
  <tikz>
  \end{adjustbox}
  \end{center}
  ```
- `wrapTikzCenter` is unchanged and still used for the LR automaton, GLL input string,
  and SPPF sections.
- No changes to standalone per-step artifacts (`tree_and_stack.tikz.tex` stays a bare
  tikzpicture, per task 107.5).

### S2: Fix flaky RPQ.Tests stack overflow blocking the hard gate — [done, c4ada33]

**Trigger:** The pre-merge hard gate for this task was BLOCKED at FLPQ.RPQ.Tests
("Test Run Aborted / Stack overflow" in `Regexp.derive`, called from
`regexToDfa` → `buildDfaFromRegex` in the ``Belyanin and Arroyuelo produce identical
results with random regex`` property). The crash was rare (a few runs out of ~30) but
absolute per gate rules. User directive: analyze and fix even though flaky.

**Root cause analysis:**
- `Regexp.derive`'s local `mkAlt` deduplicated only **one level** of alternation
  nesting. For regexes like `(a*)(aa)*`, each derivation step produced
  `RAlt(RAlt(...), newAlt)` — the duplicate alternative was buried below the depth
  dedup could see, so every step yielded a strictly larger, syntactically distinct
  derivative. The set of syntactic derivatives is infinite for such shapes, so
  `buildDfaFromRegex`'s state-discovery loop never terminated; after ~5,400 iterations
  the states were deep enough that `derive` stack-overflowed the test host.
- Exhaustive search over **all 2,881,200** regexes of depth ≤ 3 over {a, b, eps}
  (the full space of the RPQ test generator) found **202 shapes** with unbounded
  closures before the fix; 0 after (max closure: 13 states / 45 nodes).

**Code:** `src/FLPQ.Languages/EbnfParser.fs`:
- New `flattenAlts : Regexp<'t,'nt> -> Regexp<'t,'nt> list` — flattens nested
  alternations into a list of direct alternatives.
- `mkAlt` (moved to module level, private) now collects both arguments through
  `flattenAlts`, drops `REmpty`, removes duplicates with `List.distinct`, and
  recombines as a left-nested `RAlt` chain (no `REmpty` leaves — an earlier draft
  folded over `REmpty` and added a spurious third DFA state to `a* a*`).

**Tests:** `tests/FLPQ.Languages.Tests/EbnfParserTests.fs`:
- New `RegexpDerivativeTests` module: builds DFAs for three previously unbounded
  shapes (`(a*)(aa)*`, `((aa)eps|a)*`, `(a*+b*)*`) and cross-checks acceptance
  against a direct exponential membership interpreter on all strings of length ≤ 5.
  Before the fix these calls never returned; after, DFAs are deterministic with < 20
  states and match the interpreter exactly.

**Docs:** `tasks/fixes_for_book.md` — recorded the flaw (the book may present the
same one-level-dedup `mkAlt`; if so it must be updated to flatten alternations).

## Verification (after all subtasks)

1. Generate the LL summary via CLI; inspect merged TeX — every step figure wrapped in
   `\begin{adjustbox}{max width=\textwidth}`, no `\resizebox{0.98\textwidth}` anywhere.
2. Generate the SLR1 summary — step figures in adjustbox, LR automaton still resizebox.
3. Both merged TeX files compile with lualatex (covered by existing tests).
4. Full test suite via hard gate — `STATUS: PASS` (achieved 2026-09-10 after S2;
   FLPQ.RPQ.Tests step OK, coverage 90.9%, lint 0 warnings).
