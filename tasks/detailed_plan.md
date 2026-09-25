# Detailed Plan: Task 283 — Fix all remaining code review findings

## Task description (verbatim)

```
283. Fix all remaining code review findings
```

## Scope and constraints

This task resolves every item in the **Task 282 Review "Open items"** section of
`tasks/code_review.md` (the deferred, pre-existing findings). The user's explicit request
("Fix all remaining code review findings") supersedes the task-282 guidance
"do not touch GLL and RNGLR for now" — those GLL/RNGLR items are in scope.

The three finding groups:

1. **GLL/RNGLR small fixes** — dead `symbolVisualizer` parameter in
   `GllStepVisualizer.renderStep/renderInit/renderSteps`; missing module doc comment on
   `RnglrStepVisualizer`; hardcoded ANBN literals in `GllRunnerTests` / `RnglrRunnerTests`.
2. **Tuple sizes > 2** (design guide §4: tuples ≤ 2 items, else a named type) —
   `RnglrTypes.fs` stored-state 5-tuple, `Valiant.fs` binary-rules 3-tuple,
   `Sppf.fs` getCoords 4-tuple, `ExternalTools.fs` node-position 3-tuple,
   `GllTypes.fs` 4-tuple hash.
3. **RPQ visualization genericity** — `RpqGraphViz`, `ArroyueloStepVisualizer`,
   `BelyaninStepVisualizer`, `RegexpTeX` hardcode `string` terminals while the algorithms
   and every other step visualizer (Gll/Rnglr/LL/LR) are generic over `'t`/`'nt`.

Constraints:

- Every refactor is **behavior-preserving**: rendered output, parse results, and test
  goldens must stay byte-identical unless a finding explicitly changes an API.
- No new tuple > 2 items anywhere; no tuples-of-tuples workarounds (design guide §4).
- Public API changes keep XML doc comments current (design guide §6).
- The RPQ viz layer must match the existing step-visualizer convention: `renderSteps`
  takes explicit printer functions (`'t -> string`, `'nt -> string`) plus generic data;
  the visualization-step record holds rendered `string` fields only.

## Subtasks

### S1: GLL/RNGLR visualizer cleanups (dead param + module doc)

**Code:** `src/FLPQ.Printers/GllStepVisualizer.fs` — remove the unused first parameter
`(symbolVisualizer: Symbol<'t, 'nt> -> string)` from `renderStep` (~:142), `renderInit`
(~:225), and `renderSteps` (~:300), and drop the two internal pass-throughs of that value
(`renderSteps` → `renderStep`/`renderInit`, ~:314/:324). Update every external call site to
stop passing it: `src/FLPQ.Cli/GllRunner.fs:63`;
`tests/FLPQ.Printers.Tests/TexCompilationTests.fs:521,563,805,912`;
`tests/FLPQ.Printers.Tests/RsmTikzTests.fs:239`;
`tests/FLPQ.Printers.Tests/GssDotVisualizationTests.fs:17`.
`src/FLPQ.Printers/RnglrStepVisualizer.fs` — add a module-level `///` doc comment (matching
the style of `GllStepVisualizer` / `ArroyueloStepVisualizer`) describing the RNGLR per-action
substep visualizer and its book reference (sec:CFPQ_RNGLR).

**Tests:** No new tests — this is dead-code removal + a doc comment. Existing GLL/RNGLR
visualizer suites (`GllStepVisualizationTests`, `RnglrStepVisualizationTests`,
`TexCompilationTests`, `RsmTikzTests`, `GssDotVisualizationTests`) must pass unchanged,
proving the removed parameter was unused and output is byte-identical.

**Docs:** Check `docs/developer/rnglr.md` and any GLL step-viz doc for a signature listing
that includes `symbolVisualizer`; update if present. The new `RnglrStepVisualizer` module doc
needs no separate doc file (module docs are not individually documented).

**Spec:**

- Confirm `symbolVisualizer` is genuinely unused in the `GllStepVisualizer` body (only
  declared + passed through) before removing — grep shows decls at :142/:225/:300 and
  pass-throughs at :314/:324, no other use.
- After removal, the three functions keep their remaining parameters and order unchanged;
  only the leading `symbolVisualizer` is dropped.
- The `RnglrStepVisualizer` module doc states what it renders (GSS figure DOT+TikZ, path
  index, input graph, LR table with the substep's action cell highlighted) and cites
  sec:CFPQ_RNGLR, consistent with sibling visualizers.

### S2: Migrate hardcoded ANBN literals to registry bindings

**Code:** `tests/FLPQ.Cli.Tests/GllRunnerTests.fs` and
`tests/FLPQ.Cli.Tests/RnglrRunnerTests.fs` — replace the hardcoded ANBN classic grammar text
`"S -> a S b | eps"` with `TestGrammarFiles.anbnEbnf` and the 4-token accept input
`"a a b b"` with `TestGrammarFiles.anbnInput` at every site. Do NOT touch the distinct
grammar `"S -> a S b | S S | eps"` / input `"a b"` (not the ANBN classic) or non-accept
inputs such as `"a a a"` (keep those literals; only the grammar half of such calls moves to
`anbnEbnf`).

**Tests:** The affected facts already assert `File.Exists` + `Length > 0` on artifacts and do
not compare against the raw grammar source text, so the pipe-form → registry newline-form
change is safe (same parsed grammar). All GLL/RNGLR runner suites must pass unchanged. Add no
new facts — this is a data-source migration, matching the precedent set in task 282 for
`CliSummaryTests` / `ProgramDispatchTests`.

**Docs:** None (test-only change; registry-eligibility rationale already documented in the
task 282 review).

**Spec:**

- `TestGrammarFiles.anbnEbnf` = `LanguageRegistry.ANBN.Grammars.[0].Text`;
  `TestGrammarFiles.anbnInput` = the 4-token accept string `"a a b b"`. Both are in
  namespace `FLPQ.Cli.Tests`, same as the two test modules — reference directly.
- Replace only where the grammar is exactly the ANBN classic; leave the `S S` variant and
  reject-string inputs' grammar half on `anbnEbnf` but keep their non-accept input literal.

### S3: Convert RnglrTypes stored-state 5-tuple to a named record

**Code:** `src/FLPQ.Languages/RnglrTypes.fs` — introduce
`[<Struct>] type RnglrStoredState<'nt when 'nt: comparison> = { Nt: Nonterminal<'nt>; InvState: int; RangeEndState: int; RangeEndVertex: int; TriggerLrState: int }`. Change the
`RnglrGSS.StoredStates` field and the four `RnglrGSS` functions (`create`, `addEdge` return,
`getStoredStates`, `setStoredStates`) from `Set<Nonterminal<'nt> * int * int * int * int>` to
`Set<RnglrStoredState<'nt>>`. Update the type-site doc comment (:33-40) to name the record.
`src/FLPQ.Languages/Rnglr.fs` — update the construction at :266
(`(invData.Nonterminal, nextInv, endState, endVertex, p.TriggerLrState)` → record literal) and
the destructuring at :381 (`for (storedNt, storedInv, storedEndState, storedEndVertex, storedTriggerLr) in consumedStates` → `for st in consumedStates` using `st.Nt`, `st.InvState`,
etc.).

**Tests:** No new tests — behavior-preserving type refactor. The full RNGLR suite
(`RnglrTests`, `RnglrStepVisualizationTests`, cross-parser equivalence, and the byte-identical
reference-visualization check) must pass unchanged, proving the record carries identical data
and Set semantics (structural equality/hash) are preserved.

**Docs:** Update `docs/developer/rnglr.md` :123 (the `StoredStates` field type in the type
listing) and :126 (the prose describing the stored states as a 5-tuple) to reference
`RnglrStoredState<'nt>` and its named fields.

**Spec:**

- Field names map positionally: Nt←nonterminal, InvState←invState, RangeEndState←rangeEndState,
  RangeEndVertex←rangeEndVertex, TriggerLrState←triggerLrState.
- A `[<Struct>]` record gets F# structural equality + hashing, so `Set<RnglrStoredState<'nt>>`
  behaves identically to the old tuple set (membership, dedup, iteration order by hash).
- No other module references the stored-state tuple shape directly (it is internal to the GSS);
  verify with a grep for the 5-tuple type and `getStoredStates`/`setStoredStates` across src+tests.

### S4: Convert Valiant binary-rules 3-tuple and Sppf getCoords 4-tuple to records

**Code:** `src/FLPQ.Languages/Valiant.fs` — introduce a private
`type ValiantBinaryRule<'nt when 'nt: comparison> = { Lhs: Nonterminal<'nt>; Pair: BinaryPair<'nt>; ProdIdx: int }`. Replace `(Nonterminal<'nt> * BinaryPair<'nt> * int) list` in `InitData.BinaryRules`
(:52), `binaryRulesFromGrammar` (:131, :135), and `mxmSet` (:168, :179) with the record; update
the `Some(r.Lhs, { Left = left; Right = right }, number)` construction and the
`(lhs, pair, prodIdx)` destructuring. `src/FLPQ.Languages/Sppf.fs` — introduce a private
`type SppfNodeCoords = { FromState: int; FromPos: int; ToState: int; ToPos: int }`; change
`getCoords` (:558) to return `SppfNodeCoords option` and update its two constructions (:560-561)
and the two destructurings (`Some(lfs, lfp, lts, ltp)` :576, `Some(rfs, rfp, rts, rtp)` :630).

**Tests:** No new tests — behavior-preserving. Valiant and SPPF suites (including
`SppfValidatorTests`, which exercises `validateIntermediateConnectedness` where `getCoords`
lives) must pass unchanged.

**Docs:** None — both are private/internal types not surfaced in module docs (confirm against
`docs/developer/guides/documentation-conventions.md`; no public API changes).

**Spec:**

- `ValiantBinaryRule` reuses the existing `BinaryPair<'nt>` record for the `Pair` field
  (legitimate composition, not a tuple workaround); keep it `private` like `InitData`.
- `SppfNodeCoords` is a private module type in `Sppf.fs`; field names follow the existing
  `(fs, fp, ts, tp)` = from-state/from-pos/to-state/to-pos convention used in the error messages.

### S5: Convert ExternalTools node-position 3-tuple and fix GllTypes 4-tuple hash

**Code:** `src/FLPQ.Printers/ExternalTools.fs` — introduce a private
`type NodePosition = { Name: string; X: float; Y: float }`; change the intermediate
`ResizeArray<(string * float * float)>` (:146) to `ResizeArray<NodePosition>`, update the
`positions.Add((name, x, y))` (:166) and the final map (:168). The public return type
`Map<string, float * float>` stays (a 2-tuple value is allowed). `src/FLPQ.Languages/GllTypes.fs`
— replace the 4-item `hash (this.RsmState, this.Vertex, this.GssIdx, this.MatchedRange)` (:28)
with nested 2-tuple hashing (e.g. `hash (hash (a, b), hash (c, d))`) or `System.HashCode.Combine`,
matching whatever `GetHashCode` idiom the codebase already uses.

**Tests:** No new tests — behavior-preserving. `ExternalToolsTests` (node-position extraction)
and the GLL suite (Descriptor equality/hash via `GllTypes`) must pass unchanged.

**Docs:** None (private type + internal hash; no public API change).

**Spec:**

- For the GllTypes hash, prefer the idiom already used by other `GetHashCode` overrides in the
  codebase (check a couple) for consistency; the exact hash values need not be stable across
  versions (they are not persisted), only equal-for-equal and well-distributed.

### S6: Generalize the RPQ visualization layer over `'t`/`'nt`

**Code:** Make the four RPQ printer modules generic, matching the Gll/Rnglr/LL/LR convention
(printer params + generic data; step records hold rendered strings only):

- `src/FLPQ.Printers/RegexpTeX.fs` — `toTeX (terminalPrinter: 't -> string) (nonterminalPrinter: 'nt -> string) (r: Regexp<'t, 'nt>) : string`; the single-char check in
  `termToTeX` (:10-14) applies to the *printed* terminal (`terminalPrinter t`), not the raw value.
- `src/FLPQ.Printers/RpqGraphViz.fs` — `renderGraph (terminalPrinter: 't -> string) (graph: NFA<'t, int>) ...` and private `graphEdgeSet`/`graphEdgeLabel` generic over `'t`.
- `src/FLPQ.Printers/ArroyueloStepVisualizer.fs` — `renderSteps (terminalPrinter: 't -> string) (nonterminalPrinter: 'nt -> string) (graph: NFA<'t, int>) (steps: ArroyueloTraceStep<'t, 'nt> list)`;
  private helpers generic; pass printers through to `RegexpTeX.toTeX` and `RpqGraphViz.renderGraph`.
- `src/FLPQ.Printers/BelyaninStepVisualizer.fs` — `renderSteps (terminalPrinter: 't -> string) (dfa: DFA<'t, int>) (graph: NFA<'t, int>) (steps: BelyaninTraceStep<'t> list)`; drop the explicit
  `string` type arguments at :88/:91 (`AutomatonDot.dfaToDotWithHighlights`,
  `AutomatonTikz.dfaToTikzWithHighlights`) so they infer `'t`.
- Update call sites: `src/FLPQ.Cli/ArroyueloRunner.fs:60`, `src/FLPQ.Cli/BelyaninRunner.fs:46`,
  `src/FLPQ.Cli/Helpers.fs:127,134,142` (pass `id`/identity printers for the string terminals),
  and the test files (`ArroyueloStepVisualizationTests`, `BelyaninStepVisualizationTests`,
  `RpqGraphVizTests`, `RegexpTexTests`) — pass `id` (or the existing printer) so inference keeps
  them at `string`.

**Tests:** No new tests required for correctness (behavior-preserving; all call sites stay at
`string`). The existing RPQ viz suites + both lualatex end-to-end compiles must pass unchanged,
proving rendered output is byte-identical. Optionally add one fact instantiating a visualizer at a
non-`string` terminal type to lock in the genericity (only if it can be done with an existing
registry/generator without new fixtures).

**Docs:** Update `docs/developer/rpq-graph-viz.md`, `arroyuelo-step-viz.md`,
`belyanin-step-viz.md`, and `rpq-regexp-viz.md` where they show the now-generic signatures or
imply string-only terminals; state that the layer is generic over `'t`/`'nt` like the other step
visualizers.

**Spec:**

- All underlying data types are already generic (`NFA<'t,'s>`, `DFA<'t,'s>`, `Regexp<'t,'nt>`,
  `ArroyueloTraceStep<'t,'nt>`, `BelyaninTraceStep<'t>`); only these four printer files pin them
  to `string`. `PathSemiringTeX` needs no change (cells are `Set<int list>`, terminal-agnostic).
- The single genuine string-specific logic is `RegexpTeX.termToTeX`'s `String.length t = 1`;
  move that decision onto the printed name so a generic `'t` works.
- Keep the visualization-step records (`ArroyueloVisualizationStep`, `BelyaninVisualizationStep`)
  as-is (rendered `string` fields only) — they are already convention-compliant.

## Execution order

S1 → S2 → S3 → S4 → S5 → S6. All subtasks are independent (no shared edits); the order runs the
small, safe cleanups first and defers the largest cross-cutting refactor (S6) to last so progress
is bounded if it blocks. After all subtasks: full code review, hard gate (`STATUS: PASS`),
squash-merge to `dev`, mark task `[done]`.

## Completion status

All six subtasks are complete and committed on `feature/283-fix-review-findings`:

| Subtask | Commit | Status |
| --- | --- | --- |
| S1 GLL/RNGLR visualizer cleanups | 65380c5 | done |
| S2 ANBN literals → registry bindings | a79ffd4 | done |
| S3 RnglrTypes stored-state 5-tuple → record | d82f48b | done |
| S4 Valiant 3-tuple + Sppf 4-tuple → records | 4110641 | done |
| S5 ExternalTools 3-tuple → record + GllTypes hash | 286ff1c | done |
| S6 RPQ viz layer generic over `'t`/`'nt` | 991a918 | done |

Verification: build 0 errors; full-solution FSharpLint 0 warnings (111 files); suites green —
FLPQ.Languages.Tests 634, FLPQ.Printers.Tests 340, FLPQ.Cli.Tests 232 (RPQ goldens byte-identical,
lualatex compiles pass); Fantomas + mdformat clean; commit gate PASS. Code review (see
`tasks/code_review.md`, Task 283 report) found zero issues — every Task 282 open item resolved.

Note on S5: the plan suggested "nested 2-tuple hashing or `System.HashCode.Combine`, matching the
existing idiom". No other multi-field `GetHashCode` override exists in the codebase to match, so
`System.HashCode.Combine` was chosen (eliminates the tuple entirely; idiomatic for .NET 10).

Note on S6: call sites pass `id` explicitly rather than relying on the F# `string`-as-identity
value quirk that the previous code used implicitly for Belyanin's DFA labels. Behavior is
identical (both are identity on `string`); goldens are byte-identical.
