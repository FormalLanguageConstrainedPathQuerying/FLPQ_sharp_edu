# Task 256: Use math mode for node labels in BasicSPPF (CYK/Valiant) Tikz rendering

## Scope

`BasicSppfTikz.toTikz` renders BasicSPPF nodes for CYK and Valiant. Currently all
node labels are built with raw strings and then passed through
`AutomatonTikz.escapeLatex`, which escapes `$`, `_`, `{`, `}` into literal text.
As a result:

- A nonterminal `N_1` (CNF naming from `Grammar.freshStringNonterminal`) renders as
  literal `N\_1`, not as $N_1$ (math subscript).
- A terminal `a` at positions `(l, r)` renders as literal `a\_\{l,r\}`, not as
  $a_{l,r}$ (math subscript).

Task 256 changes **only** the Tikz renderer (`BasicSppfTikz`) so nonterminal names
and terminals use LaTeX math mode. The DOT renderer (`BasicSppfDot`) is **not**
changed.

## Reuse Analysis

- `AutomatonTikz.escapeLatex` — kept for the `Epsilon` and `Production` labels,
  which are not part of this task (behavior unchanged).
- `AutomatonTikz` / `LRAutomatonTikz` already insert `$\begin{aligned}...\end{aligned}$`
  content into `as={...}` **without** escaping (see `LRAutomatonTikz.stateContentToTikzAs`
  and `AutomatonTikz.nodeOptions`). Math mode inside `as={...}` is an established
  pattern in this codebase — BasicSppfTikz follows the same convention.
- The `tex_tikz_template.tex` used by compilation tests already loads `amsmath`,
  so `$...$` math renders correctly.

---

### S1: Use math mode for nonterminal and terminal labels in BasicSppfTikz + update tests

**Code:** `src/FLPQ.Printers/BasicSppfTikz.fs` — change the label construction for
          `BasicSppfNodeInfo.Terminal` and `BasicSppfNodeInfo.Nonterminal` to use
          math mode, and stop escaping those two labels
**Tests:** `tests/FLPQ.Printers.Tests/BasicSppfDotTests.fs` — update the tikz
          assertions to expect math-mode labels
**Docs:** none (rendering-only change; the convention is documented in S2)

**Spec:**
- `Terminal(Terminal t, l, r)` label becomes `$<name>_{l,r}$`, e.g. `$a_{0,1}$`.
- `Nonterminal(Nonterminal nt, l, r)` label becomes `$<name>$ [l,r]`, e.g. `$N_1$ [0,2]`.
  Only the *name* is math-mode; the `[l,r]` span stays as literal text (per task:
  "math mode for nonterminal names").
- These two labels must NOT be passed through `AutomatonTikz.escapeLatex` (math mode
  requires literal `$`, `_`, `{`, `}`).
- `Epsilon` and `Production` labels keep their current escaping (unchanged behavior).
- `BasicSppfDot.toDot` remains untouched.
- Update `BasicSppfDotTests.fs` tikz assertions:
  - `Assert.Contains("S [0,2]", tikz)` → `Assert.Contains("$S$ [0,2]", tikz)`.
  - Add assertions for the math-mode terminal label (e.g. `$a_{0,1}$`) so the
    convention is pinned by a test.

**Status:** [done]

---

### S2: Document the BasicSPPF visualization and the math-mode convention

**Code:** none
**Tests:** none
**Docs:** Create `docs/developer/basic-sppf-viz.md` (referenced from
        `docs/developer/FLPQ.Printers.md:36` but currently missing) describing
        `BasicSppfDot` and `BasicSppfTikz`, including the math-mode label
        convention for the Tikz renderer

**Spec:**
- `kind: visualization` following the canonical template (metadata, abstract, TOC).
- Document `BasicSppfDot.toDot` and `BasicSppfTikz.toTikz` signatures and output.
- Record the design decision: Tikz uses math mode (`$N_1$`, `$a_{l,r}$`) for
  nonterminal/terminal labels; DOT uses plain text labels (no math mode).

**Status:** [done]
