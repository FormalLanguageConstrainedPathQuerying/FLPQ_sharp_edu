* This file contains user-defined tasks. Do not modify them. Only track status of tasks in this file.
* **Strict rule: When marking a task as done, ONLY prepend `[done] ` to the existing task line. NEVER rewrite, reformulate, shorten, or replace the task description itself. The task text is immutable — only the status tag may change.**
* The book is in TeX. Root of the book in `../../`.
* In some tasks Russian may be used to simplify references to the book that is in Russian.

* First part of tasks located in `tasks1.md`

101. [done] Add trees visualization for LL. Currently LL visualize only stack frames, but parts of trees out of stack missed. Add these parts visualization.
102. Add golden tests for CYK merged summary generation. Generate merged tex file for CYK for several examples. Save result TeX files as reference (final combined tex only. not intermediate parts), cretae tests that generates tex for respective grammar and compare result with reference.
103. [done] Switch compilation of all TeX stuff to lualatex
104. [done] Add Tikz-based visualization for automata.
     1.   Use Tikz. Documentation: https://tikz.dev/gd-usage-tikz. Minimal example with two graphs: top-bottom and left-right layout.
     ```
        \documentclass{standalone}
        
        \usepackage{tikz}
        \usetikzlibrary{graphs, graphdrawing}
        \usegdlibrary{layered} % Required layout library
        
        \begin{document}
        
        \begin{tikzpicture}
          \graph [layered layout, nodes={draw, circle}] {
            a -> {b, c, d};
            b -> {e, f};
            c -> f;
            d -> g;
            {e, f, g} -> h;
          };
        \end{tikzpicture}
        
        \begin{tikzpicture}
          \graph [layered layout, nodes={draw, circle}, grow'=right] {
            a -> {b, c, d};
            b -> {e, f};
            c -> f;
            d -> g;
            {e, f, g} -> h;
          };
        \end{tikzpicture}
        
        
        \end{document}
     ```
     2. Make it parametrizable by vertex content rendering, vertex shapes. Default shape is circle.
     3. Default layout: left to right.
     4. Start node has lable above Start, fill green!30
     5. Final node has style double, double distance=1.5pt, fill red!30. 
     6. Example 
     ```
     \begin{tikzpicture}
        \graph [layered layout, nodes={draw, circle}] {
          a [label=above:Start, fill=green!30] -> c [double, double distance=1.5pt,fill=red!30];
        };
      \end{tikzpicture}
     ```
105. [done] Add Tikz based vizualization for LR automata. Special style for automata tikz renderer.
     1.  Use rectangles for all states.
     2.  Render content of states fully: render LR items similar to grammar. Reuse functions that used for grammar rendering. Create parametrizable if necessary.
     3.  Add state number to vertex content.
     4.  Use aligned environment to align vertex content.
     5.  Example:
    ```
    \begin{tikzpicture}
       \graph [layered layout, nodes={draw, rectangle}, grow'=right] {
         a [as=$\begin{aligned}
     S &\rightarrow a\ S\ b\ S \\
     S &\rightarrow \varepsilon \\
     \end{aligned}$, label=above:Start, fill=green!30] -> c [as=$\begin{aligned}
     S &\rightarrow a\ S\ b\ S \\
     S &\rightarrow \varepsilon \\
     \end{aligned}$, double, double distance=1.5pt,fill=red!30];
       };
     \end{tikzpicture}

    ```
106. [done] Use tikz rendering in LR steps visualization by default. Add CLI opton that allows user to swithch LR automata rendering to dot. 
107. [done] Improve Tikz-based automata visualization
     1.   Recheck tikz-based automata compilation tests. Current verson of tikzpicture does not compile, but tests passed. It is a bug in tests. It caused by edge labels. Fix is below (babel library).
     2.   For loop edges generate `loop above` attribute. Example: `s3 ->["a",loop above] s3;`
     3.   Add babel and arrows.meta libraries: `\usetikzlibrary{graphs, graphdrawing, quotes, babel, arrows.meta}`
     4.   Increase arrow heads: `\tikzset{>={Latex[width=3mm,length=3mm]}}`
     5.   For embedding into merged summary wrap tikzpicture into resizebox: `\resizebox{0.98\textwidth}{!}{...}` Generate standelone tikzpicture without resizebox.
     6.   Increase spacing  between nodes. Add `, level sep=2cm, sibling sep=1.5cm` to \graph options. Example: `\graph [layered layout, nodes={draw, rectangle}, grow'=right, level sep=2cm, sibling sep=1.5cm] `
108. [done] Rework LL tree building and rendering. 
     1.   Make derivation tree mutable. It allows to store leafs in stack frames. When nonterminal leaf pops from stack and replaced with rhs, it will be possible to add childs to popped node. 
     2.   Rework steps rendering. Draw combined stack-tree ctructure where some leafs of some partial trees are stack frames.
     3.   Resulting tree for LL must be converted to current immutable version.
109. [done] Add golden tests of LL and LR steps visulization. Generate dot files for several inputs, store them as reference, crete tests that generates dot files for respective input and compare result with reference.
110. [done] In CLI. Check that specified output directory is empty. Clean it up if no.
111. [done] Fix Valiant Seq.head crash on grammar without binary rules (P0.1). Deduplicate Valiant initialization — `parseModifiedWithTable` and `parseModifiedWithTrace` inline the same ~35 lines as `initValiant`; make them reuse `initValiant` or a shared helper (1.4, 2.6). Deduplicate empty-input epsilon-acceptance check — same 6-line block appears in `Cyk.parseWithTable`, `Valiant.parseWithTable`, `Valiant.parseModifiedWithTable`; extract shared `Grammar.isEpsilonAccepted` (2.8, P0.5). Equivalence: modified Valiant results must be identical to standard Valiant after refactoring.
112. [done] Replace `string` (`obj.ToString()`) with printer-function parameters in `SymbolTeX.terminalContent` / `nonterminalContent` / `toLaTeX` (4.1, P0.2). Add `terminalPrinter: 't -> string` and `nonterminalPrinter: 'nt -> string` parameters. Cascade the change to all call sites: `GrammarTeX`, `ParsingTableTeX`, `CykTeX`, `ValiantTeX`, `LRAutomatonTikz`, `LRRunner`, `LLRunner`, `CykRunner`, `ValiantRunner`, `SummaryTeX`. Golden tests must produce identical output after refactoring.
113. [done] Deduplicate CYK core algorithm: `cykTable` and `tableTrace` share ~25 lines of identical triple-loop CYK computation differing only in trace-step wrapping; extract core into a parameterized helper accepting an `onCellFound` callback (2.7, P0.4). Replace mutable `HashSet<Nonterminal<'nt>>` in CYK with immutable `Set` operations — uses `Set.fold` or `Set.union` for accumulation (2.3, P2.8). Equivalence: `cykTable` and `tableTrace` must produce identical acceptance results after refactoring.
114. [done] Create shared `Generators.fs` module for FsCheck `Arbitrary`/`Gen` types used across test projects: matrices, graphs, grammars, automata, regexes. Consolidate existing `MatrixGenerators`, `LinearAlgebraGenerators`, `SetMatrixGenerators`, `RandomGraphGenerators`, `RPQGenerators`, `IntersectionGenerators` into this module. Eliminate `MyGen = FsCheck.FSharp.Gen` / `MyArb = FsCheck.FSharp.Arb` duplication (10 files). The module must live in a shared test-utility project referenced by all test projects (5.1, P1.1).
115. [done] Extend RPQ cross-algorithm equivalence property tests to cover complex regex patterns: `RStar`, `RAlt`, `RSeq`, epsilon, multi-symbol chains, and combinations thereof (currently only single `(0,"a",1)` DFA / `RTerm(Terminal "a")` regex is tested across Belyanin/Arroyuelo/Kronecker). Use FsCheck generators for random regex patterns. Equivalence: all three RPQ algorithms must produce identical matrices for the same regex and graph (5.2, P1.2).
116. [done] Add dedicated unit tests for `Tokenizer.fs` — currently has zero tests despite being used by virtually every other test. Cover: empty string, whitespace-only, multi-character terminals, terminal/nonterminal classification edge cases, grammar-like input strings, EBNF-like input. Use property-based tests with FsCheck-generated strings (5.3, P1.3).
117. [done] Create `FLPQ.Cli.Tests` project and add it to `FLPQ.slnx`. Move `CliSummaryTests.fs` from `FLPQ.Printers.Tests` into the new project. Add unit tests for `CykRunner`, `ValiantRunner`, `LLRunner`, `LRRunner`, `Helpers`, `AlgorithmTypes`, `Summary`. Add error-path tests: invalid grammar file, missing input file, unsupported algorithm name, bad lookahead value, empty output directory handling. Remove reverse dependency `Printers.Tests → Cli` (1.8, 5.4, P1.4, P2.5).
118. [done] Add large-input stress tests across all algorithm families: CYK with input length 50+, Valiant with grammars having 20+ nonterminals, NFA→DFA with 30+ states, RPQ with graphs of 50+ vertices, LR with grammars producing 100+ automaton states. Tests must verify termination within reasonable time and correctness against small-input reference. FsCheck generators for stress tests should use higher bounds (50–200) than current conservative limits (5–15) (5.5, P1.5).
119. [done] Deduplicate automaton infrastructure: extract shared `alphabet` function from `Nfa.alphabet` and `Dfa.alphabet` (both iterate transition matrix identically, 2.9, P1.6). Replace `Dfa.alphabet` temporary-NFA construction with the shared function (1.2, P3.1). Replace `Nfa.toDfa` BFS body with a call to `LRAutomaton.buildAutomaton` using adapter functions — the two share identical state-space exploration structure (2.10, P1.7). Deduplicate `LRAutomaton.buildLR0`/`buildLR1` near-identical BFS construction (~60 duplicated lines) by extracting a common BFS framework parameterized by closure function and item construction (1.3, P2.2). Equivalence: NFA→DFA conversion must produce identical DFA after refactoring; LR0/LR1 automata must remain unchanged.
120. [done] Reuse LR automaton in CLI runners — currently built once for rendering (dot/tikz) and again inside `buildLR0Table`/`buildSLR1Table`/`buildCLR1Table` (2–3× per invocation). Modify table-construction functions to return the built automaton as part of `LRTable` (or a separate return value), so `LRRunner` can reuse it for rendering. Equivalence: LR parsing results must be identical after refactoring (2.13, P1.10).
121. [done] Fix naming and style issues: rename `lr0AutomatontoTikz` / `lr1AutomatontoTikz` → `lr0AutomatonToTikz` / `lr1AutomatonToTikz` (3.5, P1.11). Fix `LRSymbol` DU case and module name collision — rename module to `LRSymbolHelpers` (3.6, P1.12). Make `LRAutomatonTikz.lr0AutomatonToTikz`/`lr1AutomatonToTikz` accept `labelPrinter`/`stateVisualizer`/`shape` parameters consistent with `AutomatonTikz.dfaToTikz` signature (3.7, P1.13). Rename `isCompleted`→`isCompletedLR0`, `isCompleted1`→`isCompletedLR1` (3.8, P2.13). Replace single-letter parameter names `g`→`grammar`, `m`→`matrix` in `GrammarTeX`, `BooleanDecomposition`, `MatrixTeX`, `Graph`, `LLTableTeX` (3.9, P2.14). Rename `Submatrix` fields `A`/`B`→`row`/`col` (2.4, P2.9). Remove unused `aug` parameter from `lr0AutomatonToTikz`/`lr1AutomatonToTikz` (3.12, P3.6). Remove LR parser magic number `10000`.
122. [done] Make `Matrix<'a>.data` private — expose `get`/`set` functions in `Matrix` module. Update all direct `matrix.data.[i,j] <- value` mutations in `Automaton.fs`, `Valiant.fs`, `Cyk.fs`, `BelyaninRPQ.fs`, `KroneckerRPQ.fs`, `MsBfs.fs`. Replace color strings (`"yellow"`, `"red"`) in `Matrix.Highlight` and `Matrix.SubmatrixBlock` with semantic labels (e.g., `CurrentCell`, `SubmatrixRegion`); map labels to colors in `FLPQ.Printers` only. Update `Cyk.fs` and `ValiantTeX.fs` to use semantic labels (1.1, 1.7, P2.1, P2.4). Equivalence: all matrix operations must produce identical results; rendering output must be identical.
123. [done] Deduplicate miscellaneous helpers: keep single public `readIfExists` in `FLPQ.Printers` (used by both `SummaryTeX` and CLI `Helpers`, 2.11, P1.8). Move `collectSteps` (step directory enumeration) into `FLPQ.Printers` as a public function, use from both `SummaryTeX` and CLI `Helpers` (2.12, P1.9). Extract shared `termPrinter` lambda from `LLStepVisualizer`/`LRStepVisualizer` into a shared visualizer helper module (2.14, P2.11). Use `DerivationTreeDot.escapeLabel` in `AutomatonDot` instead of inline `.Replace` (2.15, P2.12). Make `Grammar.nonterminalsOf`/`terminalsOf` public, remove duplicate private implementations from `LLTableTeX` (2.1, P2.6). Remove unused definitions: `Automaton.buildDfaMatrix`, `GrammarTests.nonterminalsOfCnf` (2.5, P2.10). Remove dead test code: `AutomatonTests.StringArb`, `RsmToGrammarTests`/`EbnfParserTests` unused `MyGen`/`MyArb` imports (5.9, P3.10). Fix `ExternalToolsTests` cleanup to not swallow exceptions silently — log warnings instead (5.10, P3.11). Simplify `Summary.AlgorithmKind` to carry string directly as `TablePerStep | LL | LR` with `toString` member, eliminating double-mapping (2.17, P3.16).
124. [done] Resolve `Rhs.toList` / `Rhs.toSymbols` / `Rhs.length` epsilon ambiguity. `toSymbols` returns `[]` for epsilon (consistent with `length=0`), but `toList` returns `[Epsilon]`. Rename functions to clearly signal behavior: `toNonEpsilonList` for the current `toSymbols`, `toListWithEpsilon` for the current `toList`. Update all call sites. Equivalence: behavior must be identical after rename (2.2, P2.7).
125. [done] Move `VisualizationStep` type (containing rendered DOT/TeX strings `treeAndStack`, `input`) from `FLPQ.Languages.VisualizationTypes` to `FLPQ.Printers` — it contains rendered output, not pure data, and is produced/consumed by Printers. Standardize trace-type locations: all trace-step types (`CykTraceStep`, `ValiantTraceStep`, `ModifiedValiantTraceStep`, `LLParsingStep`, `LRParsingStep`) should live in their respective algorithm projects' types modules, not randomly split between algorithm files and `VisualizationTypes.fs` (1.6, 3.2, P2.3, P3.4).
126. [done] Add XML documentation comments (`///`) to all undocumented public APIs: `Matrix.fs` (9 functions), `BooleanDecomposition.fs` (3 functions), `Graph.fs` (10+ functions), `Automaton.fs` Nfa/Dfa modules (8+ functions), `SummaryTeX.fs` (9 functions), `Tokenizer.fs` (4 functions), `RSM.fs` (7 functions), `DerivationTree.fs` (2 functions). Documentation must describe behavior, preconditions, postconditions, and parameter meanings (3.10, P2.15).
127. [done] Refactor `SummaryTeX.fs` from mutable `let mutable lines = []` / `lines <- lines @ [...]` imperative pattern to functional pipelines using `List.collect`, sequence expressions, or `StringBuilder` for performance-critical sections. The output must be identical (verified by golden tests) (3.11, P2.16).
128. [done] Add property-based equivalence tests: `toCnf` must preserve language (generate random grammars, check that both original and CNF accept/reject the same random strings up to length N). `FirstFollow` must compute correct FIRST/FOLLOW sets (verify against brute-force derivation). NFA→DFA conversion must preserve language (generate random NFAs, check random strings against both). Add property tests for `AutomatonDot` and `RsmBuilder` (output must be parseable/computable). Fix `BooleanDecompositionTests` property test that silently passes when `decompose` returns empty Map for non-empty input — add assertion that non-empty input must produce non-empty decomposition (5.6.2, 5.7, P2.17, P2.18).
129. Fill golden test gaps: add golden tests for LL table TeX output, Matrix TeX output, Automaton dot/Tikz output, Derivation tree dot output, Valiant/modified Valiant trace TeX output. Each test generates output for a known input, saves as reference, and compares. Also add Tex/DOT runtime compilation checks (integrate with `ExternalToolsTests` pattern) for modules that lack them (5.8, 5.6.1, P2.19).
130. [done] Add LR conflict behavior test: verify that "reduce on everything" LR(0) table produces conflicts predictably (not silently). Test that `LRTable.conflicts` list is populated when the grammar is ambiguous or non-LR. Verify that conflict reporting in visualization matches actual table conflicts (6.1, 5.6.3, P2.20).
111. [done] Replace `Nonterminal<'nt> * Nonterminal<'nt>` tuple keys in Valiant with a named struct `BinaryPair<'nt> = { left: Nonterminal<'nt>; right: Nonterminal<'nt> }` (4.6, P3.9). Define type alias `RsmDfa<'t,'nt> = DFA<RsmSymbol<'t,'nt>, int>` and use consistently in `RsmBlock` and `RsmBuilder` (1.5, P3.2). Make `RsmSymbol` consistent with `Symbol` DU pattern — either remove `[<RequireQualifiedAccess>]` or add it to `Symbol` as well (3.4, P3.5). Equivalence: all existing tests must pass after refactoring.
132. [done] Add LL(k>1) parsing tests — generate test grammars requiring k>1 lookahead, verify correct parsing tables and acceptance/rejection (5.6.4, P3.12). Add modified Valiant empty-input test (5.6.5, P3.13). Move 4 NFA/DFA backward-compatibility member tests from `GraphAnalysis.Tests/GraphTests.fs` to `FLPQ.Languages.Tests/AutomatonTests.fs`; remove `FLPQ.Languages` reference from `GraphAnalysis.Tests` (1.9, P3.3). Move `CliSummaryTests.fs` to `FLPQ.Cli.Tests` (already in task 117, ensure no residual reference).
