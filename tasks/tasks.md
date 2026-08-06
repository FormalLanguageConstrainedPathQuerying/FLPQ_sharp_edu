* This file contains user-defined tasks. Do not modify them. Only track status of tasks in this file.
* **Strict rule: When marking a task as done, ONLY prepend `[done] ` to the existing task line (between task number and task formulation). NEVER rewrite, reformulate, shorten, or replace the task description itself. The task text is immutable — only the status tag may change.**
* **User guidance annotations**: When a task requires user clarification (Blocked Work Protocol), guidance may be appended after the task's full formulation as `**[USER GUIDANCE]**: <text>` on an indented line. These are the only permitted additive changes to task descriptions. See the `user-guidance-transfer` skill.
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
129. [done] Fill golden test gaps: add golden tests for LL table TeX output, Matrix TeX output, Automaton dot/Tikz output, Derivation tree dot output, Valiant/modified Valiant trace TeX output. Each test generates output for a known input, saves as reference, and compares. Also add Tex/DOT runtime compilation checks (integrate with `ExternalToolsTests` pattern) for modules that lack them (5.8, 5.6.1, P2.19).
130. [done] Add LR conflict behavior test: verify that "reduce on everything" LR(0) table produces conflicts predictably (not silently). Test that `LRTable.conflicts` list is populated when the grammar is ambiguous or non-LR. Verify that conflict reporting in visualization matches actual table conflicts (6.1, 5.6.3, P2.20).
131. [done] Replace `Nonterminal<'nt> * Nonterminal<'nt>` tuple keys in Valiant with a named struct `BinaryPair<'nt> = { left: Nonterminal<'nt>; right: Nonterminal<'nt> }` (4.6, P3.9). Define type alias `RsmDfa<'t,'nt> = DFA<RsmSymbol<'t,'nt>, int>` and use consistently in `RsmBlock` and `RsmBuilder` (1.5, P3.2). Make `RsmSymbol` consistent with `Symbol` DU pattern — either remove `[<RequireQualifiedAccess>]` or add it to `Symbol` as well (3.4, P3.5). Equivalence: all existing tests must pass after refactoring.
132. [done] Add LL(k>1) parsing tests — generate test grammars requiring k>1 lookahead, verify correct parsing tables and acceptance/rejection (5.6.4, P3.12). Add modified Valiant empty-input test (5.6.5, P3.13). Move 4 NFA/DFA backward-compatibility member tests from `GraphAnalysis.Tests/GraphTests.fs` to `FLPQ.Languages.Tests/AutomatonTests.fs`; remove `FLPQ.Languages` reference from `GraphAnalysis.Tests` (1.9, P3.3). Move `CliSummaryTests.fs` to `FLPQ.Cli.Tests` (already in task 117, ensure no residual reference).
133. [done] Simplify Valiant (and modified Valiant) algorithms.
     1.   Use operations over set-based matrices instead of boolean (each cell is a set of nonterminals, like in CYK). op_add --- set union. op_mult --- op_mult(S1, S2) = {N# | N3 -> N1 N2 in Grammar, N1 in S1, N2 in S2}
     2.   Do not use boolean decomposition in valiant and its visualization (for modified version too).
     3.   For Valiant visualization: forward step is a one step in recursive submatrices decomposition. Show decomposition. Backward steps are submatrices multiplications. Show multiplied submatrices and changed cells.
     4.   For modified Valiant visualization: layer handling (both forward and backward). Highlight layers with colors.
     5.   In CLI add params to run Valiant and modified Valiant algorithms.
134. [done] Improve Valiant visualization. Trace only doMultiplications. For each doMultiplications, for each task visualize all three submatrices. 
135. Investigate output for valiant summary: `-a valiant -g data/example_grammar.bnf -i data/example_input.txt -s` Steps 5, 6, 7, 19, 20, 21, 25, 26, 27 contains only one submatrix, while valiant visualization step is doMultiplications with three submatrices. Detect the problem. Fix it. 
136. [done] Set test coverage calculation up. Use dotnet-coverage: https://learn.microsoft.com/en-us/dotnet/core/additional-tools/dotnet-coverage. 
     1.   Improve CI to check coverage. Total line coverage must be not less than 80%. 
     2.   Investigate local coverage report. Report functions with low coverage.
     3.   Do not try to improve coverage. Just improve CI and analize current coverage.
137. [done] Implement GLL for RSM as described in DAMDID_GLL_CFPQ. Reference implementation of algorithm from book section sec:CFPQ_GLL (06_GLL_Based.tex) and paper DAMDID_GLL_CFPQ/sections/gll.tex. GLL builds a path index (matrix) during execution; SPPF is immutable, built once from the index as a separate step. Input string is a special case of a graph: "aababb" -> vertices 0..6 with edges i-[symbol]->i+1. Reference implementation of algorithm from book section sec:CFPQ_GLL (06_GLL_Based.tex) and paper DAMDID_GLL_CFPQ/sections/gll.tex. GLL builds a path index (matrix) during execution; SPPF is immutable, built once from the index as a separate step. Input string is a special case of a graph: "aababb" -> vertices 0..6 with edges i-[symbol]->i+1.
     1.   Implement SPPF node types and SPPF as a wrapper over Graph. Types:
          - SppfNodeInfo<'t,'nt>: DU = SppfTerminal(Terminal<'t> * leftPos: int * rightPos: int) | SppfNonterminal(Nonterminal<'nt> * leftPos: int * rightPos: int) | SppfEpsilon(pos: int) | SppfIntermediate(state: int * pos: int) | SppfRange(fromState: int * fromPos: int * toState: int * toPos: int).
          - SppfEdgeLabel: DU = SingleChild | LeftChild | RightChild | PackedAlternative.
          - SPPF<'t,'nt>: record = { graph: Graph<SppfNodeInfo<'t,'nt>, Option<SppfEdgeLabel>>; rootIndices: int list }.
          Each SPPF node is a Graph vertex. Edges encode the tree/forest structure. Between a specific (parent, child) pair there cannot be two edges of different types, so edge label is Option<SppfEdgeLabel> (not NonEmptySet). Packaging of alternatives: one RangeNode has multiple PackedAlternative edges to different child vertices. SPPF roots are range nodes corresponding to ranges of interest (their indices in rootIndices). SPPF is built once after GLL (subtask 5) and is fully immutable.
     2.   Implement path index (matrix) based on Matrix<Set<...>>. Types:
          - RangeKey: struct = { fromState: int; fromVertex: int; toState: int; toVertex: int }.
          - RangeDescriptor: DU = EmptyRange | NonEmptyRange of RangeKey.
          - PathIndexEntry<'t,'nt>: DU = PTerminal of Terminal<'t> | PNonterminal of Nonterminal<'nt> | PIntermediate of state: int * pos: int.
          - PathIndex<'t,'nt>: record = { matrix: Matrix<Set<PathIndexEntry<'t,'nt>>>; stateCount: int; vertexCount: int }.
          Size K x K where K = |Q| * |V|. Mapping RangeKey -> linear index: idx(q, v) = q * vertexCount + v. Cell I[fromKey, toKey] stores a set of entries. Operations: add, get, indexOf.
     3.   Implement GSS as a wrapper over Graph. Types:
          - GssVertexInfo: struct = { state: int; vertex: int; mutable storedPops: Set<RangeDescriptor> }. storedPops lives directly in the vertex (as in UCFS GssNode.popped).
          - GssEdgeInfo: struct = { returnState: int; matchedRange: RangeDescriptor }.
          - GSS: record = { graph: Graph<GssVertexInfo, Option<NonEmptySet<GssEdgeInfo>>> }.
          Vertices are all possible pairs (q, v): |Q| * |V| vertices, pre-allocated via Graph.fromEdges with an empty |Q|*|V| x |Q|*|V| matrix. Edges use NonEmptySet because multiple edges between same (source, target) pair are possible (different return addresses). Module GSS: indexOf, addEdge (adds edge + returns set of storedPops for immediate processing), pop (saves range to storedPops, returns all outgoing edges). storedPops is mutated in-place through Graph.vertexMap (vertexMap is immutable by keys but record values are mutable).
     4.   Implement GLL core that builds the path index. Function buildPathIndex: RSM<'t,'nt> -> inputGraph: Graph<int, Option<'t>> -> startVertices: Set<int> -> PathIndex<'t,'nt>. Descriptor: struct { rsmState: int; vertex: int; gssIdx: int; range: RangeDescriptor }. Algorithm (Listing lst:gll_rsm_cfpq, paper section "GLL-Based CFPQ Algorithm"):
          - Initialization: for each start vertex, GSS vertex at (q0', vs), descriptor (q0', vs, gssIdx, EmptyRange) into queue Q. GSS is pre-allocated.
          - Main loop: queue Q, Set<Descriptor> of handled. While Q not empty:
            1. Terminal transitions. For each (q0, t, q1) in transition set and (v0, t, v1) in graph edges: create descriptor (q1, v1, s0, R^{p,u}_{q1,v1}). Add PTerminal(t) to I[(q0,v0)][(q1,v1)] and PIntermediate(q0, v0) to I[(p,u)][(q1,v1)].
            2. Nonterminal transitions (calls). For each (q0, N, q1): GSS.addEdge with return address q1 and current range. storedPops are handled immediately: for each storedPop, combine with current range, add PIntermediate to index, create continuation descriptor. Also create descriptor for the start state of block N.
            3. Final state (return). If q0 is final in some block: GSS.pop saves the recognized range to storedPops, returns outgoing edges. For each edge (qret, R^{q2,v2}_{q3,w0}): add PNonterminal(N) to I[(q3,w0)][(qret,v0)] and PIntermediate(q3, w0) to I[(q2,v2)][(qret,v0)]. Create descriptor (qret, v0, parentGssIdx, R^{q2,v2}_{qret,v0}).
          - Helper stringToGraph: string -> Graph<int, Option<char>> for string input.
     5.   Implement SPPF construction from path index. Function buildSppfFromIndex: PathIndex<'t,'nt> -> rootRanges: RangeKey list -> SPPF<'t,'nt>. Top-down traversal of the index starting from each rootRange:
          - For range R^{qi,vi}_{qj,vj} look at I[(qi,vi)][(qj,vj)].
          - PTerminal(t) -> create SppfTerminal.
          - PNonterminal(N) -> create SppfNonterminal, recursively process the range inside block N (from its start to final state over the same graph positions).
          - PIntermediate(q, v) -> create SppfIntermediate with LeftChild edge to range R^{qi,vi}_{q,v} and RightChild edge to range R^{q,v}_{qj,vj}, recursively process both.
          - Range nodes are reused: on first visit to a range, create SppfRange vertex; on subsequent visits, only add PackedAlternative edge to the existing Range vertex.
          For intermediate nodes that always have two children, use exactly LeftChild and RightChild. For NonterminalNode always SingleChild. Build by accumulating vertices and edges, then assemble Graph via Graph.fromEdges at the end.
     6.   Implement extraction of a single derivation tree from SPPF. Function extractDerivationTree: SPPF<'t,'nt> -> rootIdx: int -> DerivationTree<'t,'nt>. Top-down recursive traversal. For a RangeNode with multiple PackedAlternative children, pick the first one. Mapping to DerivationTree:
          - SppfTerminal(t, l, r) -> Leaf(Symbol.T(t))
          - SppfEpsilon(pos) -> Leaf(Symbol.Epsilon)
          - SppfNonterminal(nt, l, r) -> Node(nt, [child]) via SingleChild edge
          - SppfIntermediate -> recursively process left (LeftChild) and right (RightChild) children, concatenate their child lists
          - SppfRange -> follow the first PackedAlternative edge that has children (some alternatives may be epsilon)
     7.   Implement tests in tests/FLPQ.Languages.Tests/GllTests.fs:
          1. Equivalence with CYK ([<Property>]). For random grammars and random strings, GLL accepts/rejects the same strings as CYK (via Grammar.toCnf). Include ambiguous and left-recursive grammars. Reuse grammar/string generators from FLPQ.TestUtilities/Generators.fs. Acceptance: there exists a path from any start vertex to some final vertex reachable via the extended RSM start block (i.e., I contains entries for ranges from (q0',vs) to (q1',vf) or similar).
          2. Extracted tree yield. For accepted strings: DerivationTree.leaves (extractDerivationTree sppf rootIdx) = input string (as list of characters).
           3. Comparison with classical LL. For an unambiguous grammar without left recursion, the number of nonterminal nodes in the GLL-SPPF-extracted tree equals the number in the classical LLParser tree, adjusted for the extended RSM (the S' block adds exactly one extra nonterminal node S' above the original start symbol).

138. [done] Implement RNGLR (Right-Nulled Generalized LR) for RSM. Book reference: sec:CFPQ_RNGLR. RNGLR is the LR-based counterpart to GLL (task 137), sharing SPPF, PathIndex, and tree extraction infrastructure. RNGLR builds a path index during execution; SPPF is built as a separate step from the index.
     1.   Reuse SPPF types from GLL (SppfNodeInfo, SppfEdgeLabel, SPPF from GllTypes.fs). Reuse PathIndex, RangeKey, RangeDescriptor, PathIndexEntry types. Reuse buildSppfFromIndex and extractDerivationTree from Gll.fs (adapt to RNGLR's entry format if needed).
     2.   Extend LR items and LR table construction to operate over RSM blocks (not Grammar rules).
          - An LR item over RSM: { blockNonterminal: Nonterminal<'nt>; rsmState: int } — a local state in one RSM block DFA, plus a lookahead symbol for LR(1).
          - LR closure: for an item at state q in block N, if the DFA has a nonterminal transition q --RNonterm(M)--> qNext, add items at the start state of block M. Lookahead for LR(1): compute FIRST of the suffix after the nonterminal transition.
          - LR goto: advance items by following terminal or nonterminal transitions in their respective blocks.
          - LR automaton: DFA over Symbol<'t,'nt> where each DFA state is a Set of items. Reuse Automaton.buildAutomaton with item sets (similar to existing LR0/LR1 construction but over RSM block states).
          - Extend FIRST/FOLLOW for RSM: FIRST of an RSM state = set of terminals reachable without consuming input (accounting for nullable nonterminals). FOLLOW: reuses existing followK on the grammar derived from the RSM.
          - LR table: maps (automatonState, symbol) to Shift | Reduce | Accept. Reduction by N is triggered when an item's rsmState is final in block N.
     3.   Implement layered GSS with symbol-labeled edges. Types (new file RnglrTypes.fs):
          - RnglrGssVertex: struct = { lrState: int; inputVertex: int }. Vertex = (LR automaton state, input graph position). Pre-allocated: |Q_lr| * |V| vertices.
          - RnglrGssEdge: struct = { symbol: Symbol<'t,'nt> }. Edge label = what grammar symbol was shifted at this step.
          - RnglrGSS: record = { graph: Graph<RnglrGssVertex, Option<NonEmptySet<RnglrGssEdge>>> }. Multiple edges between same (source, target) pair possible (different shift symbols) → NonEmptySet.
          Designed as a labeled directed graph (i.e., an automaton) to enable intersection with the inverted RSM during reduction.
     4.   Implement layered processing: shift-then-reduce. At input vertex v, build the layer:
          a. SHIFT phase: for each active GSS node (lrState, v), look up terminal transitions in LR table. For each shift action on terminal t where the input graph has edge (v, t, vNext): create new GSS node (shiftTarget, vNext), add edge from (shiftTarget, vNext) to (lrState, v) labeled Symbol.T(t). Enqueue (shiftTarget, vNext) for layer vNext.
          b. REDUCE phase (after all shifts at v complete). For each GSS node (lrState, v) where LR table says reduce by N:
             - Invert the RSM block DFA for N: reverse all transitions, swap start (old final states become starts, old start becomes final). The result is an NFA over RsmSymbol<'t,'nt> (can have multiple start states).
             - Construct the GSS-NFA: take the connected component of the GSS reachable backwards from (lrState, v), treating GSS edges labeled Symbol.T(t) / Symbol.N(M) as transitions labeled RTerm(Terminal t) / RNonterm(Nonterminal M).
             - Intersect GSS-NFA × inverted-RSM-NFA using Nfa.intersect from Automaton.fs. In the product, find all pairs (gssVertex, invRsmFinal) reachable from ((lrState, v), eachInvRsmStart). The gssVertex in each pair is a predecessor (lrStatePre, vPre) — where nonterminal N started.
              - For each predecessor: look up goto(lrStatePre, N) in LR table → gotoTarget. Add GSS edge from (gotoTarget, v) to (lrStatePre, vPre) labeled Symbol.N(N). Enqueue (gotoTarget, v) for further reductions at the same layer v (reductions may cascade).
          c. Passing reductions (analogous to GLL's storedPops). When a reduction by N traverses backwards through intermediate GSS vertices, cache the result at each traversed vertex:
             - RnglrGssVertex gains a field: storedReductions : Map<Nonterminal<'nt>, Set<int * int>>, mapping nonterminal N to the set of (lrStatePre, gotoTarget) pairs reachable from this vertex via reduction by N.
             - After the reduce phase processes a reduction by N from (lrState, v), each intermediate GSS vertex visited during the GSS-NFA × inverted-RSM intersection gets its storedReductions updated with N → (lrStatePre, gotoTarget).
             - When the SHIFT phase (or a prior reduce phase) adds a new edge to a GSS vertex, that vertex's storedReductions are consumed and cleared. For each (N, predecessorSet) in storedReductions and each (lrStatePre, gotoTarget) in predecessorSet: add a GSS edge from (gotoTarget, v) to (lrStatePre, vPre) labeled Symbol.N(N) and enqueue (gotoTarget, v) for reduction at the current layer. This ensures reductions discovered earlier are propagated through newly added GSS paths without re-running the intersection.
     5.   Build path index during RNGLR execution.
          - For each shift: add PTerminal(t) to I[(lrState, v)][(shiftTarget, vNext)].
          - For each reduction by N finding predecessor (lrStatePre, vPre): add PNonterminal(N) to I[(nsStart, vPre)][(nsFinal, v)] where nsStart/nsFinal are block N's start and final RSM states. Add PIntermediate to I[(lrStatePre, vPre)][(gotoTarget, v)].
          - For empty input / epsilon acceptance: the initial automaton state at vertex 0 may reduce via epsilon-nullable nonterminals without consuming input; these reductions produce PNonterminal entries and cascade via the reduce phase at layer 0.
     6.   SPPF construction from path index: reuse GLL.buildSppfFromIndex. If RNGLR's entry layout differs, adapt the converter (module shared between GLL and RNGLR).
     7.   Derivation tree extraction: reuse GLL.extractDerivationTree (path-index-based).
     8.   Implement tests in tests/FLPQ.Languages.Tests/RnglrTests.fs:
          1. Equivalence with CYK ([<Property>]). For random grammars and random strings, RNGLR accepts/rejects same as CYK (via Grammar.toCnf). Include right-nullable, ambiguous, and left-recursive grammars.
          2. Equivalence with GLL ([<Property>]). RNGLR produces identical acceptance results as GLL (task 137) for the same grammar + input. Tests that both path indices have equivalent entries (possibly different intermediates/projections, but same acceptance outcome).
          3. Extracted tree yield. For accepted strings: DerivationTree.leaves = input string chars.
          4. Acceptance fact tests. Specific grammar + input pairs: basic shift-only, epsilon, right-nullable chain (S -> A B, A -> a A | eps, B -> b B | eps), left-recursive, ambiguous.
          5. Reduction cascade test. Verify that reductions at a layer can trigger further reductions at the same layer (e.g., A → eps reduce triggers B → A reduce, both at layer 0 without consuming input).
     
139. [done] Help on 138. 
      1.    Use exended grammar to simplify and RSM (reuse code form GLL, move this code to RSM)
      2.    Layer handled with fixpoint logik: while new nodes appears (as results of reductions), continue reductions.
      3.    Add simple property tests
            1.    S -> a* must accepts and rejects same strings as DFA constructed for a*
            2.    S -> a* a*  must accepts and rejects same strings as DFA constructed for a* a*
            3.    S -> (a | b)* must accepts and rejects same strings as DFA constructed for (a | b)*
             4.    S -> (a | b)* (a | c)* must accepts and rejects same strings as DFA constructed for (a | b)* (a | c)*

140. [done] Refactor RNGLR findPredecessors to classical automata intersection and fix passing mechanism.
      Rework the ad-hoc BFS in findPredecessors into a standard automaton intersection algorithm (simultaneous traversal over (gssIdx, invState) pairs). Both the GSS (over Symbol<'t,'nt>) and inverted RSM blocks (over RsmSymbol<'t,'nt>) are automata — implement the product construction as a standalone function and replace the current BFS with it. Fix the passing reduction mechanism to align with this model and resolve the 2 remaining right-nullable test failures.
      1.   Precompute inverted RSM blocks once — build invBlocks: Map<Nonterminal<'nt>, DFA<RsmSymbol<'t,'nt>, int>> at the start of buildPathIndex, not per findPredecessors call.
      2.   Reuse intersectrion from Automaton.intersection to find predcessors. To do it represent GSS as an automaton.
      3.   Replace storedReductions with storedStates: Set<(Nonterminal<'nt>, int)> array:
           - Per GSS vertex: flat set of (nonterminal, invState) pairs. The pair is a unique key — nonterminal identifies which inverted block, invState is the state within it.
           - During intersection, store ALL reachable intermediate (nonterminal, invState) pairs at the corresponding GSS vertex.
      4.   Rewrite findPredecessors:
           - let reachable = intersectAutomata
           - Store intermediate configs for passing: for each (g, inv) in inersection automata, add (nt, inv) to storedStates.[g].
           - Return predecessors: reachable |> filter (_, invState = blockStartState) |> map to (lrState, gssIdx, v).
      5.   Rewrite addEdge passing:
           - Consume storedStates.[toIdx], clear it.
           - For each (nt, invState) consumed: continue intersection from (fromIdx, invState) using invBlocks.[nt].
           - Store new intermediate configs in storedStates. Find new predecessors at block start states.
           - For each new predecessor: look up goto, create GSS edge, enqueue.
      6.   Add Task 139 simple regex-DFA equivalence tests first (before refactoring) — use as debug safety net:
           - S -> a* ≡ DFA for a* ([<Property>])
           - S -> a* a* ≡ DFA for a* a* ([<Property>])
           - S -> (a | b)* ≡ DFA for (a | b)* ([<Property>])
           - S -> (a | b)* (a | c)* ≡ DFA for (a | b)* (a | c)* ([<Property>])
           - For each regex, build RSM from single EBNF rule S -> <regex>. Build DFA for the same regex independently. Property-test: for random strings, rnglrAccepts rsm str = dfaAccepts dfa str.
      7.   Equivalence requirement: after refactoring, ALL existing RNGLR tests must pass (21/21), and the 2 currently failing right-nullable tests must be resolved. Equivalence with the pre-refactoring implementation must be verified for all existing passing tests.

141. [done] Refactor Automaton.intersection. Output tyme mast be NFA<'t, 's * 'v>: States in resulting automaton is a product of states of intut automata.
142.  [done] Add GLL proerty tests:
            - S -> a* ≡ DFA for a* ([<Property>])
            - S -> a* a* ≡ DFA for a* a* ([<Property>])
            - S -> (a | b)* ≡ DFA for (a | b)* ([<Property>])
            - S -> (a | b)* (a | c)* ≡ DFA for (a | b)* (a | c)* ([<Property>])
            - For random strings, gllAccepts rsm str = dfaAccepts dfa str.
143.  [done] Add tests for GLL and GLR. Check acceptance and crone of derivation tree.
      1.    Grammar: S -> N a*; N -> (a a) | a  Accept: a, aa, aaa, aaaa . Reject: <empty string>, b, ab, aab, aaab, abaa
      2.    Grammar: S -> a* N; N -> a | (a a)  Accept: a, aa, aaa, aaaa . Reject: <empty string>, b, ab, aab, aaab, abaa
      3.    Grammar: S -> N*; N -> a | (a a)  Accept: <empty string>, a, aa, aaa, aaaa . Reject: b, ab, aab, aaab, abaa
      4.    Grammar: S -> a | S S | S S S  Accept: a, aa, aaa, aaaa . Reject: <empty string>, b, ab, aab, aaab, abaa
144.  [done] For all grammars from task 143 add property tests: for random string acceptGLL str == acceptGLR str == acceptCYK  str
145.  [done] Fix test that fails on CI. Fix locally. Do not try to pysh and run CI. Log:
          ```
               Test run for /Users/runner/work/FLPQ_sharp_edu/FLPQ_sharp_edu/tests/FLPQ.Printers.Tests/bin/Release/net10.0/FLPQ.Printers.Tests.dll (.NETCoreApp,Version=v10.0)
               A total of 1 test files matched the specified pattern.
               [xUnit.net 00:00:00.55]     AutomatonVisualizationTests+AutomatonGoldenTests.NFA a+ tikz golden [FAIL]
               Failed AutomatonVisualizationTests+AutomatonGoldenTests.NFA a+ tikz golden [46 ms]
               Error Message:
               Golden file 'nfa_aplus.tikz' was created in output/GoldenData/.
               Copy it to tests/FLPQ.Printers.Tests/GoldenData/ and re-run tests.
               Stack Trace:
                    at GoldenHelpers.verifyGolden(String goldenFileName, String actualContent) in /Users/runner/work/FLPQ_sharp_edu/FLPQ_sharp_edu/tests/FLPQ.Printers.Tests/GoldenHelpers.fs:line 20
               at AutomatonVisualizationTests.AutomatonGoldenTests.NFA a+ tikz golden() in /Users/runner/work/FLPQ_sharp_edu/FLPQ_sharp_edu/tests/FLPQ.Printers.Tests/AutomatonVisualizationTests.fs:line 319
               at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
               at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

               Failed!  - Failed:     1, Passed:    47, Skipped:     0, Total:    48, Duration: 410 ms - FLPQ.Printers.Tests.dll (net10.0)

          ```
146.  [done] Set FSharp lint up (https://fsprojects.github.io/FSharpLint/).
      1.    Install dotnet tool: https://fsprojects.github.io/FSharpLint/how-tos/install-dotnet-tool.html
      2.    Run with default config and generate detailed structured report to `linter_report.md`. Group problems by types. Add brief summary.
      3.    Do not try for fix problems detected by linter. Just generate report.
147.  [done] Linter tuning.
       1.    Move linter report linter_report.md to tasks/ 
       2.    Create project-specific linetr config: https://fsprojects.github.io/FSharpLint/how-tos/rule-configuration.html   It must be based on default config: https://github.com/fsprojects/FSharpLint/blob/master/src/FSharpLint.Core/fsharplint.json 
       3.    Tune config. 
             1.    FL0085 — Local Function Naming: in our project we use camelCase
             2.    FL0069 — Type Parameter Naming: in our project we use camelCase
             3.    Regenerate report. Place it in tasks/linter_report.md. Align it with previous version of report.
148. [done]  Fix preblems detected by linter.
      1.    Fix all problems detected by fsharplint. Ensure that all problems fixed.
            1.    Fix it one-by one. Linter reports position of error. Use it to fix. Do the same with compilation errors. Do not try to invent general solution (do not try to write renaming script, do not try to rename with sed, etc)
      2.    Update AGENTS.md with instructions to use linter to check whether all ready for commit. You must not commit code with problems detected by linter. Run linter only if F#-related files were chenged. Add instructions how to run linter. Linet must be run on slnx file, not on projects or sources individually.
      3.    Update CI. Linter must be executed on CI.
149. [done]  `RnglrTests.fs` lines 175–188 contain three `[<Fact>]` tests with empty bodies `()`. It is critical problem. Make these tests meaningful. Fix code such taht all tests pass. 
      1.    Line 176: ``S -> a S b | eps accepts a a b b`` — comment: "grammar2 with S -> S S creates unbounded DFA states"
      2.    Line 180: ``S -> a S b | eps rejects a a b`` — same comment
      3.    Line 185: ``S -> a S b | eps | S S accepts a b a b`` — similar comment
      Comments are worng. Two first gramars do not contain S -> S S rule at all. Third grammar contains. But it must not be a problem. Right part of production is a valid regular expression, DFA must be constructed correctly by derivatives.
150. Refactoring
     1.   Fix ALL fsharplint problems
     2.   `FLPQ.GraphAnalysis.fsproj` includes `<PackageReference Include="FSharpPlus" Version="1.9.1" />` but neither `Graph.fs` nor `MsBfs.fs` uses any FSharpPlus types (no `NonEmptyList`, `NonEmptySet`, etc.). Remove the unused package reference.
     3.   Identical function `collectGraphEdges` (`Graph<int, Option<'t>> -> ResizeArray<'t * int>[]`) is defined in both:
          1.   `Gll.fs:49–58` (inside `module GLL`)
          2.   `Rnglr.fs:35–44` (inside `module Rnglr`)
          Create common function and use in both places.
     4.   Two substantial helper functions are copy-pasted:
          1.    `grammarToEbnfText` (`RnglrTests.fs:11–29`, `GllTests.fs:13–31`) — converts a BNF grammar to EBNF text for RSM builder
          2.    `grammarToRsm` (`RnglrTests.fs:31–33`, `GllTests.fs:35–37`) — wraps `grammarToEbnfText` + `RsmBuilder.buildRSMFromText`
          Create common function and use in both places.
     5. Helper functions `stringToTerminals`/`stringToChars`, `inputToGraph`/`terminalsToGraph`, `rnglrAccepts`/`gllAccepts`, `cykAccepts`, and `nonEpsilon` are duplicated or near-duplicated.
        Create common function and use in both places.
     6. `GraphTests.fs` lines 63–72 (`filterOutgoing empty set`) and lines 74–83 (`filterIncoming empty set`) are structurally identical, differing only in the function called. The verification loop is copy-pasted. Create one generic parametrizable function.
     7. `BooleanDecomposition.fs`: `decompose` (lines 12–27) and `decomposeNonEmptySet` (lines 32–52) share the same structure. Create one generic parametrizable function.
151. [done] Fix GLL anf GLR
      1.   Fix tree tests. all tests must check that crone of tree is exactly the input tree. 
      2.   Fix code if some tests mail
152. [done] Add GLL/RNGLR visualization: SPPF DOT, PathIndex TeX, RSM DOT, CLI runners.
      1.   Add SPPF DOT visualization. Render SPPF graph (GllTypes.fs) to DOT format.
      2.   Add PathIndex TeX visualization. Render GLL/RNGLR path index matrix to TeX.
      3.   Add RSM DOT visualization. Render RSM automaton structure to DOT format.
      4.   Add GLL CLI runner. Support -a gll, step-by-step output.
      5.   Add RNGLR CLI runner and RnglrTableTeX. Support -a rnglr + LR table tee.
      6.   Add GLL/RNGLR to CLI Algorithm enum, Summary module, and Program dispatch.
      7.   Fix pre-existing FSharpLint warnings in modified projects.
153. [done] SPPF refactoring
      1.   Add tests on path index to tex rendering and tex compilation.
      2.   GLL and GLR tree tests contains workaround to collect terminals. It is illigable. Tusts must check the algorithm. If SPPF construction incorrect, fix it. If necessary, ask me to assist with sppf structure.
      3.   Improve path index printing
           1.   Print pairs (rsm_state, input_positions) as indexes for rows and colunms. Make matrix printer parametrizable by index printer
           2.   In summary Wrap path index matrix with resizebox  0.9/textxwidth
154. Refactoring. RnglaTypes.
      1.   storedStates
155. Microsoft.NET.Test.Sdk.Program.fs  .NETCoreApp,Version=v10.0.AssemblyAttributes.fs  AssemblyInfo.fs
156. [done] Fix skills and documentation violating no-duplicates principle. Multiple skills describe documentation process. Some skills describe documentation structure ("what") instead of procedures ("how"). Create single canonical source for documentation requirements in docs/, trim all skills to reference it.
157. [done] Strengthen subtask-loop and git-workflow skills to prevent subtask batching. Add hard gate before commit (verify single SN, no ranges), rename "Commit Gate" → "Pre-Commit Check" to disambiguate from final merge gates, add multi-subtask sequential discipline, add documentation-only subtask section, strengthen git commit message format rules. Add doc-only task note to AGENTS.md.
158. [done] Rework RNGLR path index.
      1.   Turn skipped tests on RNGLR trees on.
      2.   Rework RNGLR bfs traversal: each moving-formard step add intermediate node. For grammar `S -> a a` and input `0 a 1 a 2`. Having both `a` on gss you match first (inversed oreder) `a` in BFS cretae terminal node in cell with input positions 1, 2 and respective RSM states. Next BFS step you add terminal to cell with input positions 0,1. And intermediate node I(1) to cell with input position 0,2 (and respective RSM states). For more complex cases your behaviour is similar. So, during BFS propagate collected range coorinates required to know were to add intermediate node.
      3.   Fix SPPF root definition and acceptance check: root of SPPF for linear case is a range from start input position to end input position, from start state of extended RSM to next state (reachable by start nonterminal of original grammar. Must be single one transition from start state in extended rsm).
      4.   Check taht all tests pass.
      5.   Recheck GLL to be aligned with previous changes. Path index and SPPF MUST be identical gor GLL and RNGLR
      6.   Check taht all tests pass.
159. [done] Add more tree checking tests for both GLL and RNGLR. All tests extract tree from sppf and check that leafs are exactly input string.
      1.   Grammar: `S -> a S b S | eps `     String: a a b a b b
      2.   Grammar: `S -> a S b S | eps `     String: a a b a b b a b
      3.   Grammar: `S -> a S b S | eps `     String: a a a b a b b a b b
      4.   Grammar: `S -> S a S b | eps `     String: a a a b a b b a b b
      5.   Grammar: `S -> S a S b | eps `     String: a a b a b b a b
      6.   Grammar: `S -> S S | a S b |eps `     String: a a a b a b b a b b
      7.   Grammar: `S -> S S | a S b |eps `     String: a a b a b b a b
      8.   Grammar: `S -> (a S b)* '     String: a a a b a b b a b b
      9.   Grammar: `S -> S1 S2; S1 -> (a S1 b)*; S2 -> (c S2 d)* `     String: a a a b a b b a b b 
      10.   Grammar: `S -> S1 S2; S1 -> (a S1 b)*; S2 -> (c S2 d)* `     String: a a a b a b b a b b c c d c d d
      11.   Grammar: `S -> S1 S2; S1 -> (a S1 b)*; S2 -> (c S2 d)* `     String: a a a b a b b a b b c d
      12.   Add proprty based tests: collect all grammars used in GLL and RNGLR tests. For each collected grammar property test: for randomly generated string (use strings up to 30 symbols), if sreing accepted, leafs of tree is exactly it string. 
160. [done] Refactoring (GLL and RNGLR).
      1.    Collect all SPPF-related types and functions in separated module.
      2.    Collect all path index related types and functions in separated module
      3.    RnglrAction is similar to LRAction. Use one common type
161. [done] SPPF refactoring
      1.    For all tests in GLL and GLR add check that all cells of path index contains mot more than 1 nonterminal. It must be true even if input does not accepted.
      2.    Do not use strings as a key for sppf nodes in sppf extraction. Reuse existing types or crete new one. Create specific dictionary for each node type.
      3.    Nonterminal is not an alternative for range node. If range (cell) contains nonterminal, it must contains only one nonterminal and possibly several nodes of other types. So, nonterminal node is a predcessor of respective range node. For example, if intermediate node i1 refers to range node r1, and cell for r1 contains nonterminal N1, edges in sppf: i1 -> N1, N1 -> r1
162. [done] Выделение вспомогательных инструментов
      1.    Создай в корне проекта директорию tools. В ней будут храниться вспомогательные инструменты для работы с проектом, контроля качества кода и т.д. Задокументируй это.
      2.    Проанализируй текущие скилы и создай инструменты на Python. Каждый инструмент возвращает свой файл с подробными лоагми и результатми. Файл создаётся в дирекртрии tmp в корне (она уже есть). Файлы перезаписываются. Ничего не пишется в консоль. Продумай структуру файла так, чтобы тебе было уддобно его анализировать. Например: сперва короткое резюме о наличии проблем. Потом детальный лог соответствующих команд. Задокументируй структуру файлов. Инструменты запускаются БЕЗ каких-либо таймаутов. 
            1.     Для определения проектов, в которых были изменения. 
            2.     Для проверки качества кода между подзадачами. Мперва запускается форматирование. Потом запускается сборка всего решения.
            3.     Для hard gate проверки качества. Последовательность: форматирование, сборка всего решения, все тесты с контролем покрытия (установить среднее в 80% по строкам, для проектов не ниже 75% по строкам), fsharplint только тех проектов, которые были изменениы.
      3.    Замени в скилах соответствущие места на инструкцию по запуску созданных инструментов. Явно укажи, что запуск без таймаута. Явно укажи, что результат в файле и необходим детальный глубокий анализ его содержимого. Сперва исправляем все проблемы, указанные в файле, потом перезапускаем инструменты.
      4.    Обрати внимание: на текущий момент покрытие может быть меньше установленного. Не исправляй это. Сейчас главное --- подготовить инструменты контроля.
163. [done] Доработай тестовое покрытие проектов, котрые не проходят пороговое значение.
164. [done] GLL fail wtih infinite loop when handle grammar `S -> a S b | S S | eps`  and string a b . Fix this problem. Add respective test.
165. [done] Check tmp/hard-gate.txt. Logs from fsharplint looks like tool run linter incorrectly: `ERROR running fsharplint: [Errno 2] No such file or directory: '/usr/lib/dotnet/tools/dotnet-fsharplint'` Fix this problem
166. [done] When I run `dotnet src/FLPQ.Cli/bin/Release/net10.0/FLPQ.Cli.dll -a rnglr -i data/example_input.txt -g data/example_grammar_amb.bnf -s -o viz_output/glr` resulting SPPF in DOT conteins range nodes that have no childs. Moreover, leafs of SPPF missed (not all terminals presented). Fix this probelm. Create respective test.
167. [done] For all GLL anf RNGLR tests (property and fact) add check that on accepted string in SPPF for this string each range node contains at least one child.
168. Tests GLL 
169. [done] Improve storage for RSM. Current design stores boxes in separated Matrices. Some algorithms introduce states remapping to guarantee uniquity. Avoid remapping.
       1.   In RSM stores all transitions for all boxes in one common Matrix.This way number of state is globally unique.
       2.   Flow: individual automata for each nonterminal using existing fuctions -> merging of automata.
       3.   When extends RSM, add new states at the end of range, not to the start. Thus you avoid existing states renumbering, remapping.
       4.   Update algorithms that use RSMs.
170. [done] Fix path index rendeting
171. [done] Improve SPPF rendering and constructing.
172. [done] Improve SPPF building and rendering.
173. [done] Reorganize RNGLR tests.
174. Remove GLL.extractDerivationTree. Use extractDerivationTreeFromSppf evrywhere
175. [done] Improve python scripts tooling.
176. [done] Add tests for wollowing grammaers for RNGLR.
177. [done] Add tests for GLR.
178. [done] Unify RSM test flow for GLL and RNGLR.
179. [done] RNGLR tests refactoring.
180. [done] Fix RNGLR. Some acceptance tests on RNGLR use rnglrAcceptsWithoutSppfValidation instead rnglrAccepts.
       1.   Use rnglrAccepts everywhere
       2.   If some tests fail, fix RNGLR algorithm.
181. [done] Remove all gllAccepts. Rename rnglrCheckReject and rnglrAccepts because they are not specific for RNGLR. Use renamed versions of rnglrCheckReject and rnglrAccepts for both GLL and RNGLR tests. If some tests fail, fix GLL algorithm. RNGLR is correct.
182. [done] Rework RNGLR.
       1.    Use extractDerivationTreeFromSppf in RNGLR tests instead of Gll.extractDerivationTree. Preserve original extractDerivationTreeFromSppf signature.
       2.    Current version of RNGLR handles nonterminals incorrectly.
             1.    Remove stub epsilon node. Preserve epsilon nonterminal to explicitly mark epsilon derivation.
             2.    Nonterminal in Path index cell is just a reference to another range node. Nonterminal node marks call site.
             3.    Example: Suppose grammar has rules N -> A, A -> a. Input a. RSM for grammar: N: 0 -[A]-> 1; A: 2 -[a]-> 3. cell (0,0)(1,1) contains Nonterm(A). Child of this node is a range node --- cell (0,2)(1,3). Because to recognize A we must go from A_start=2 to A_final=3. In general, nonterminal may have several childs, because block may have several final states. So, to build SPPF we must check all respective cells.
             4.    Current version add stub epsilon nodes. E.g. in provided example, after fix, must not be epsilon nodes at all. Intermediate node is a real concatenation of two ranges. There are no ranges to concatenate in this grammar. Current version of RNGLR adds redundant intermediate nodes. Epsilon nodes a childs of range nodes. Epsilon is an explicit marker of epsilon string reduction. It may contains only in cells with coordinates of form (i,q)(i,q).
             5.    Nonterminal node is a child of respective range node. For our example: nonterm a is a child of range node (0,0)(1,1)
       3.    After this fix two invariants in tests must be corrected.
             1.    PathIndex invariant. After fix cell may contains more than one Nonterminal. Replace it with: if cell (i,p)(j,q) contains nontermonal A,  start state of block for A is s and final states are f1,f2...fi, then at least one cell (i,s)(j,fi) is not empty.
             2.    SPPF nonterminal children invariant. After fix Nonterminal node may have more than one children. Replace it with "all childs on nonterminal node are range nodes".
  Detailed analysis of detected issues and required code changes:

  **A) Self-referencing `PNonterminal` in callee cell (Rnglr.fs — `productBfs` lines 194-212, `processReduction` lines 279-315)**:
  Both `productBfs` (BFS traversal) and `processReduction` add `PNonterminal(reduceNt)` to the callee's own range cell `(globalStart, vPre)→(finalRsmState, vEnd)`. This creates a self-referencing cycle in the SPPF: Range → Nonterm(N) → Range[same cell]. Tests pass only by accident because F# Set ordering places `PTerminal` before `PNonterminal`, so the terminal child is picked first in extraction.

  **B) `productBfs` `isInitialStartState` (Rnglr.fs line 159)**:
  For epsilon blocks (start=final), unconditionally adds `PNonterminal` even though the range is epsilon (fromVertex=toVertex). Should be `PEpsilonNonterminal` and only for genuine epsilon derivations.

  **C) `processReduction` adds PEpsilonNonterminal for ALL epsilon cases (Rnglr.fs lines 283-284)**:
  When `vPre=vEnd`, unconditionally adds `PEpsilonNonterminal` to the callee cell, even for chain epsilons like A→B, B→eps. For chain epsilons, the callee cell already has `PNonterminal(B)` and `PEpsilonNonterminal(A)` is spurious. Should only add when `vPre=vEnd && finalRsmState=globalStart` (true direct epsilon production).

  **D) Five spurious entries in `processReduction` caller propagation (Rnglr.fs lines 343-376)**:
  Each reduction adds 5 entries to the caller cell where only 1 is needed:
  - `(callGlobalState, vPre)→(returnGlobalState, vEnd)`: `PNonterminal(reduceNt)` — CORRECT call site
  - `(callGlobalState, vPre)→(globalStart, vPre)`: `PEpsilonNonterminal` — stub epsilon at call boundary
  - `(callGlobalState, vPre)→(returnGlobalState, vEnd)`: `PIntermediate(globalStart, vPre)` — redundant intermediate
  - `(globalStart, vPre)→(returnGlobalState, vEnd)`: `PIntermediate(finalRsmState, vEnd)` — intermediate straddling blocks
  - `(finalRsmState, vEnd)→(returnGlobalState, vEnd)`: `PEpsilonNonterminal` — stub epsilon at return boundary

  **E) Redundant intermediate in callee cell (Rnglr.fs lines 306-313)**:
  When `finalRsmState ≠ globalStart`, finds the first terminal target from block's start state and adds `PIntermediate`. For rules like N→A (single nonterminal) or A→a (single terminal), there is no concatenation → intermediate is redundant. Intermediate nodes should only be added by `productBfs` during actual BFS traversal.

  **F) `processReduction` skip condition (Rnglr.fs lines 334-341)**:
  Complex guard prevents adding `PNonterminal` at the caller cell when vPre=vEnd, call is from caller's start, returns to a final state, and caller≠freshStart. This prevents legitimate entries for chains like S→A, A→eps (the caller cell `(S_start,0)→(S_final,0)` needs `PNonterminal(A)` for SPPF linkage).

  **G) Direct epsilon child of nonterminal in SPPF (Sppf.fs lines 166-168)**:
  When `ntStart=ntFinal && fromPos=toPos`, creates an Epsilon node as direct `SingleChild` of Nonterminal node. Per the task: epsilon nodes are children of **range** nodes, not nonterminal nodes. The epsilon should be a `PackedAlternative` child of the range node, and the nonterminal links to that range.

  **H) `validateNonterminalChildren` allows `SppfEpsilon` (Sppf.fs line 304)**:
  Accepts both `SppfRange` and `SppfEpsilon` as valid children. Per task 182.3.2: only `SppfRange` should be allowed.

  **I) GLL also has same self-referencing PNonterminal (Gll.fs lines 206-207, 286-288, 304-307, 326-332)**:
  Not part of task 182 but the SPPF changes (G, H) affect the shared construction. GLL's self-referencing PNonterminal will create cycles after SPPF fix — may manifest as wrong tree yields.

  **Summary of required code changes:**

  | File | Location | Change |
  |------|----------|--------|
  | Rnglr.fs:159 | `productBfs` isInitialStartState | Remove `addToIndex … PNonterminal`. Not needed — callee cell already populated by BFS entries + processReduction for epsilon. |
  | Rnglr.fs:194-212 | `productBfs` isStart | Remove PNonterminal/PEpsilonNonterminal addition. Keep only predecessor tracking. |
  | Rnglr.fs:279-315 | `processReduction` callee cell | Only add `PEpsilonNonterminal` when `vPre=vEnd && finalRsmState=globalStart`. Remove PNonterminal and PIntermediate here. |
  | Rnglr.fs:317-379 | `processReduction` caller items | Replace with: for each caller item, add `PNonterminal(reduceNt)` at caller cell `(callGlobalState, vPre)→(returnGlobalState, vEnd)`. Remove the skip condition (lines 334-341) and all 5 extra entries. |
  | Sppf.fs:163-172 | `buildSppfFromIndex` PNonterminal | Remove direct epsilon child (lines 166-168). Always process child range if non-empty. |
  | Sppf.fs:303-306 | `validateNonterminalChildren` | Remove `SppfEpsilon` from allowed children — only `SppfRange`. |

  Golden test `path_index_rnglr_aa.tex` will need regeneration after RNGLR path index changes.
 183. [done] Deep GLL refactoring: common isAccepted, parameterized accepts/checkReject, remove GLL.extractDerivationTree.
        1.    Move `isAccepted` from Rnglr.fs to PathIndex.fs as a shared function.
              - Signature: `PathIndex<'t,'nt> -> ExtendedRSM<'t,'nt> -> int -> bool`
              - Checks range (startGlobalState, 0) → (startGlobalState+1, vertexCount-1) where startGlobalState = flatExt.BlockStart[flatExt.StartBlock]
              - This is algorithm-independent: both GLL and RNGLR build path indices over ExtendedRSM states with S' at the end
              - Update all callers: RnglrRunner.fs, TestHelpers.fs, RnglrTests.fs — replace `Rnglr.isAccepted` → `PathIndex.isAccepted`
        2.    Refactor GLL to use ExtendedRSM internally (same as RNGLR).
              - In `Gll.buildPathIndex`: replace `let rsm = ersm.OriginalRsm` → `let rsm = ersm.ExtendedRsm`
              - All derived values (stateCount, StateInfo, BlockStart, termTrans, nontermTrans) now come from ExtendedRSM
              - GLL starts from S' block (fresh start), which has one nonterminal transition to original start — processed as a regular call, no algorithmic change needed
              - Remove `Gll.isAccepted` entirely (~18 lines)
        3.    Update GllRunner.fs for ExtendedRSM-based path index.
              - Replace `GLL.isAccepted` → `PathIndex.isAccepted`              
              - Root ranges: from fresh start block (S' start → S' final), same as RNGLR
        4.    Parameterize `accepts` and `checkReject` in TestHelpers.fs.
              - New signatures:
                ```
                accepts : (Nonterminal<string> -> ExtendedRSM<_,_> -> Graph<_,_> -> PathIndex<_,_>)
                        -> (PathIndex<_,_> -> ExtendedRSM<_,_> -> int -> bool)
                        -> RSM<string,string> -> string list -> bool
                checkReject : (Nonterminal<string> -> ExtendedRSM<_,_> -> Graph<_,_> -> PathIndex<_,_>)
                            -> (PathIndex<_,_> -> ExtendedRSM<_,_> -> int -> bool)
                            -> Grammar<string,string> -> string list -> bool
                ```
              - Shared pipeline: create ExtendedRSM → buildPI freshStart ersm graph → isAcc pi ersm vc → SPPF from fresh start block → enumerateTrees → validate leaves = input
              - Remove obsolete helpers: `gllAcceptsRsm`, `gllAcceptsWithSppfCheck`, `gllAcceptsAndCheckTree`, `buildPathIndexForRsm`
        5.    Add local bindings in GllTests.fs, replace all call sites.
              ```
              let private accepts = TestHelpers.accepts Gll.buildPathIndex PathIndex.isAccepted
              let private checkReject = TestHelpers.checkReject Gll.buildPathIndex PathIndex.isAccepted
              ```
              - Replace `TestHelpers.accepts` → `accepts` (~20 calls)
              - Replace 36 `match TestHelpers.gllAcceptsAndCheckTree rsm input with | Some tree -> Assert.Equal(input, DerivationTree.leaves tree) | None -> Assert.True(false, ...)` → `Assert.True(accepts rsm input)` (since `accepts` already validates tree leaves internally)
        6.    Add local bindings in RnglrTests.fs, replace all call sites.
              ```
              let private accepts = TestHelpers.accepts Rnglr.buildPathIndex PathIndex.isAccepted
              let private checkReject = TestHelpers.checkReject Rnglr.buildPathIndex PathIndex.isAccepted
              ```
              - Replace `TestHelpers.accepts` → `accepts`, `TestHelpers.checkReject` → `checkReject` (~35 calls)
              - Replace `Rnglr.isAccepted` → `PathIndex.isAccepted` in checkRsmAccepts/checkRsmRejects
              - Inline ExtendedRSM creation in buildSppf (since buildPathIndexForRsm is removed)
        7.    Remove `Gll.extractDerivationTree` from Gll.fs (~85 lines).
              - Tree extraction goes through SPPF (`Sppf.enumerateTrees`) for both algorithms
              - No other code references `Gll.extractDerivationTree` after the above changes
 184. [done] GLL is incorrect. Fix it.
      1.   To see problem add one more PathIndex invariant: of RSM does not contains states that simultaneously start and final, then path index does not contains epsilon nodes in cells.
      2.   Run GLL tests. All must pass.
      3.   Be careful. While path index incorrect, some tests may fall in infinite loop in enumarate trees. Add trace to see it.
      4.   Run RNGLR tests. All must pass. If someone ail --- report to the user.
 185. [done] Add more invarinats.
      1.   For accepted strings, path index invariant: path index cell (q,0)(p,l-1) contains start nonterminal of the original grammar. l is a length of input, q is a start state of box for S`, p is a target state for start nonterminal transition (suppose S is a start nonterminal of the original grammar): q -[S]-> p.
      2.   Run all tests. All must pass.
      3.   If something fail, fix respective parsing algorithm
 186. [done] Check that all tests (grammars, inputs) form RNGLR ests in GLL tests and back. 
      1.   Collect all grammars and inputs in TestGrammars.
      2.   Extend tests of GLL and RNGLR: final set for each of them must be the union of current sets.
      3.   Run all tests. If something fails, fix respective parsing algorithm.
 187. [done] Improve GLL visualization
      1.   Add collecting information about steps. The algorithm must collect F# data sructures that describes each step (similar to LL)
           1.   Data: descriptors queue, path index, GSS, input
           2.   Collect: 
                1.   Initial state (prior main loop iteration) 
                2.   At the end of main loop body: when single descriptor processing fully finished.
      2.   Add steps visualization
           1.   Reuse exidting functions
           2.   Each step is separsted subfolder (look at LL visualization)
           3.   Descriptors queue: tex, descriptors separsted by comma
           4.   GSS --- graph in DOT. Parametrize graph visualizarion by vertices and edges vizualization
                1.   Highlight newly added edges and vertices with color.
      3.   Impriove summary content
           1.   Add original grammar
           2.   Add RSM for extended grammar
                1.   in visualization of RSM-s use global states numbering (all states stored in common Matrix)
 188. blockFinalStates
 189. [done] Redesign documentation structure with metadata, abstracts, TOCs, and consistent sections. Add tag taxonomy for grep-based search. Update documentation conventions guide and reusing skill. Apply template to all developer docs (44 docs).
 190. [done] Inprove visualization
      1.   Remove \footnotesize from path index wrapper 
      2.   Use R^{from_state, from_pos}_{to_state, to pos} to render matched range in descriptor
      3.   Highliight current GSS node
 191. [done] Improve hard gate python script.
      1.   Run tests project-by project. Update status project-by-peoject: update total status, add status fro each project. Note: all tests must be run. Colect tests project, do not hardocde them.
      2.   Run linting for touched projects project-by-project. Update status project-by-peoject: update total status, add status fro each project.
      3.   IN overall status add `<steps done>/<total steps>`. For testing and linting --- one project is one step
 192. [done] Improve GLL visualization
      1.   Add handled descriptors set to each step
      2.   Add all newly created descriptors for each step.
      3.   Visualize descriptors to precess and handled descriptors as e on table with following structure
           1.   Header --- components of descriptor: q or rsm state, i for input, g for gss vertex, MR fro matched range,
           2.   Header splitted by two hlines.
           3.   first block of rows --- descriptors to handle.
           4.   Second block of rows --- handled descriptors.
           5.   Blocks separated with two hlines
      4.   Highlight current descriptior in the table (fill row with yellow!20 color). Note: do not collect currentGSS vertex. It is a part of curretn descriptor.
      5.   Render newly created descriptors as a set of tuples. Highlight (with filled box) already handled with red!20, really new with green!20 
193. [done] Improve GLL summary visualization
      1.   In descriptors table highlight cirrent descriptor (yellow!20)
      2.   Do not show descriptors queue. Descriptors  Table only.
      3.   Add original grammar
      4.   Replace RSM with extended RSM. Use global states numbering.
      5.   At the start add colours legent for all highlighting. What color of what used for what.
194. [done] Fix GLL summary visualization. Summary generated with `dotnet src/FLPQ.Cli/bin/Release/net10.0/FLPQ.Cli.dll -s -i data/example_input_a_a_a.txt -g data/example_grammar_a_a_a.bnf -a gll -o viz_output/gll` has some some strange places. Investigate and fix if necessary.
      1.   Sppf contains 5 intermediate nodes. But final PathIndex contains only one IntermediateNode. In steps, path index does not contain all intermediate nodes. Why?
      2.   In some steps (eg step 5, step 12, step 19) no highlighted cirrent gss node.
      3.   No highlighting of modified path index cells each step.
195. [done] Add invarinat to GLL and RNGLR tests (accepts function). Invariant for accepted strings:
     1.   total numaber of nonterminals in path index not less than total numaber of SPPF nonterminal nodes
     2.   total numaber of terminals in path index not less than total numaber of SPPF terminal nodes
     3.   total numaber of epsilon-nonterminals in path index not less than total numaber of SPPF epsilon-nonterminal nodes
     4.   total numaber of intermediate nodes in path index not less than total numaber of SPPF intermediate nodes
     5.   If SPPF constriction adds some nodes not represented in PathIndex (excluding range nodes: range nodes are cells in Pathindex, so they are not expliitly represented ass cells content) --- remove respective code. All nodes in SPPF must be represented in PathIndex.
     6.   Run all GLL and RNGLR tests. All must pass. If something fail, fix apth index creation for respective algorithm. 
196. [done] Improve Gll visualization
     1.   In descriptors table highlight not cells in row, but full row.
     2.   In some GSS figures, initial vertex rendered correctly as (<S`_start>,0), but in some figures it renders as v<number>. It is wrong. Right id first variant: (<S`_start>,0)
     3.   In Descriptors we use numbers of GSS nodes. Render this numbers in GSS nodes explicitly. Eg: <gss_v_number>: (<q>,<i>)
     4.   Add to each step RSM with highlighted current state. Use the same color as for current gss node.
197. [done] Add tests on GSS to dot visualization (for GLL). Reuse existing dot reader. Extend it if necessary.
     1.   Use all files produced by full trace visualization, not one isolated dot file for arbitrary step.
     2.   Verify that in generated DOT file all vertices has labael of form `idx: (rsm_state, input_position)`. E.g `1: (2,0)` Current to dot serialization does not use this template, so fix it first.
     3.   Verify that in generated DOT file all edges has labael of form `rsm_state, input_position -> rsm_state, input_position`. Eg `1,0 -> 2,1`  Use unicode symbol for arrow. Current to dot serialization does not use this template, so fix it first.
     4.   Verify that in generated DOT file there is exactly one blue colored vertex.
     5.   Run all tests. They must pass. If no, fix GSS to dot generartion. 
198. [done] Gll input visualization improvement. For visualizaiotn
     1.   Convert linera string to graph: `abc` to  `0-[a]->1-[b]->2-[c]->3` Use existing Graph type
     2.   Render graph to dot. 
     3.   Use this graph in summary instead of current input representation. Highlight current position: vertex (get position form current descriptor)
199. [done] GLL step content layout for summary.
     1.   For summary, for each step content use template `data/GLL_step_template.tex` to layout each step.
     2.   Reuse all currently generated parts "as-is", just use prvided teplate to layout parts.
200. Add tests on GLL steps visualization
201. [done] Refactor RNGLR algorithm to classical descriptor-based working set structure. Replace per-vertex pending queues and recursive cascade with a single global descriptor queue (descriptors to handle) and handled descriptors set. Main loop takes next descriptor from queue, consults LR table for actions (shift/reduce), performs all defined actions, and enqueues new descriptors for future processing. Remove recursive processNode cascade by enqueuing descriptors for reduction targets instead. Remove the depth guard (1000). All existing tests must pass without changes.
202. [done] Add RNGLR steps visualization. Analogous to GLL steps visualization. Each step is one `processNode` call iteration in the per-vertex loop. Visualize descriptors table, GSS, path index, LR automaton (instead of RSM), input graph, new descriptors. Use the same three-panel side-by-side template for step layout.
    1.   Add step data collection to RNGLR algorithm.
         - Add `RnglrParsingStep<'t,'nt>` record type to `RnglrTypes.fs`. Fields: `PendingQueues: RnglrDescriptor list[]` (per-vertex pending queue snapshots), `ActiveGssVertices: Set<int>`, `ActiveGssEdges: Set<int*int>`, `NewGssVertices: Set<int>`, `NewGssEdges: Set<int*int>`, `PathIndexMatrix: Matrix<Set<PathIndexEntry<'t,'nt>>>`, `ChangedCells: Set<int*int>`, `InputVertex: int`, `CurrentLrState: int option`, `CurrentDescriptor: RnglrDescriptor option`, `HandledDescriptors: Set<RnglrDescriptor>`, `NewDescriptors: Set<RnglrDescriptor>`, `AttemptedDescriptors: Set<RnglrDescriptor>`.
         - Add `Rnglr.buildPathIndexWithSteps` — variant of `buildPathIndex` accepting an `onStep` callback. The callback fires at initialization and after each `processNode` call inside the per-vertex loop (`for v in 0..vertexCount-1 do while pending.[v].Count > 0 do ...`). Collects: per-vertex pending queue state, active GSS elements (differenced from previous step via `collectActiveGss` analog), path index matrix copy, changed cells, current descriptor, and descriptor accounting (new/attempted/handled).
         - Share `collectActiveGss` from GLL (or extract common version) — both GLL and RNGLR scan GSS edge matrices to find active vertices/edges.
    2.   Add RNGLR step visualizer.
         - New file `src/FLPQ.Printers/RnglrStepVisualizer.fs`. Module `RnglrStepVisualizer` with type `RnglrVisualizationStep` containing rendered strings: `DescriptorsTable`, `NewDescriptors`, `GssDot`, `PathIndex`, `Input`, `LrAutomatonDot`.
         - Descriptors table TeX: columns `lrState \ input` (2 columns vs GLL's 4). Same structure: header with `\hline\hline`, to-handle block, `\hline\hline`, handled block. Current descriptor row gets `\rowcolor{yellow!20}`. Reuse GLL's `newDescriptorsToTeX` pattern — render descriptor as `(lrState, input)`, green background for new, red for reattempts.
         - GSS DOT: reuse `GssDot.toDotFromSets` with RNGLR-specific label printers. Vertex label: `gssIdx: (lrState, inputVertex)` (computed via `lrState * vertexCount + inputVertex`). Edge label: grammar symbol (from `RnglrGSS.outgoingEdges`). Highlight new vertices yellow, new edges red, current node lightblue.
         - LR automaton DOT: reuse `AutomatonDot.dfaToDot` with a wrapper that adds `fillcolor=lightblue` for the current LR state. State visualizer: render LR items via reusable logic from `LRAutomatonTikz`, with state number prefix. Start state has green fill, final (accept) states double border.
         - Path index TeX: reuse `PathIndexTeX.toTeXWithHighlights` (generic over `PathIndex<'t,'nt>`).
         - Input graph DOT: reuse `InputGraphDot.toDot` (generic). Current vertex highlighted lightgreen.
    3.   Add RNGLR step template.
         - New file `data/RNGLR_step_template.tex`. Same three-panel `minipage` layout as `data/GLL_step_template.tex`. Placeholders: `__DESCRIPTORS_TABLE__` (left 7%), `__STEP_GSS_PDF__` (middle-top), `__STEP_LR_AUTOMATON_PDF__` (middle-bottom, 47% total), `__STEP_INPUT_PDF__` (right-top), `__PATH_INDEX__` (right-middle), `__NEW_DESCRIPTORS__` (right-bottom, 42% total).
    4.   Update CLI runner.
         - `RnglrRunner.fs`: call `Rnglr.buildPathIndexWithSteps` instead of `Rnglr.buildPathIndex`. Render steps via `RnglrStepVisualizer.renderSteps`. Write per-step files.
         - `Helpers.fs`: add `writeRnglrStepsVisualization` — writes per-step files: `descriptors_table.tex`, `new_descriptors.tex`, `gss.dot`, `path_index.tex`, `input.dot`, `lr_automaton.dot`. Add `findRnglrStepTemplate` (same pattern as `findGllStepTemplate`).
    5.   Update summary TeX generation.
         - `SummaryTeX.fs`: add `rnglrStepSection` (analogous to `gllStepSection`) — reads per-step files, fills `RNGLR_step_template.tex` placeholders. Add `rnglrColorLegend`. Update `buildContent` to route `SummaryKind.RNGLR` through per-step rendering (replace current static-only RNGLR header). Update `headerSection` to include LR automaton PDF/Tikz + color legend for RNGLR.
         - `Summary.fs`: for RNGLR, load `RNGLR_step_template.tex`, compile DOT files to PDF via `compileDotArtifacts`, pass template to `buildContent`.
    6.   Add tests.
         1. RNGLR step output existence test: verify `writeRnglrStepsVisualization` produces all 6 expected files per step directory.
         2. RNGLR step golden test: generate full step visualization for grammar `S -> a a` with input `a a`, save all step artifacts as golden reference, compare.
         3. RNGLR summary compilation test: generate merged TeX with `-s` flag for the same grammar+input, compile with lualatex, verify success.
         4. RNGLR GSS DOT vertex/edge label format test: verify DOT output contains vertex labels of form `idx: (lrState, inputVertex)` and edge labels with grammar symbols. Reuse DOT parsing from existing GLL GSS tests.
         5. Add compilation tests for all parts.
    7.   Reuse analysis.
         - Fully reusable as-is: `PathIndexTeX.toTeX`, `PathIndexTeX.toTeXWithHighlights`, `InputGraphDot.toDot`, `AutomatonDot.dfaToDot`, `GssDot.toDotFromSets`, `SppfDot.toDot`, `GrammarTeX.grammarToTeX`, `RnglrTableTeX.tableToTeX`, `ExternalTools.compileDotFileToPdf`, `ExternalTools.compileTexFile`, `Helpers.readFile`, `Helpers.writeOutputFile`, `Helpers.cleanOutputDir`, `Helpers.naturalSortKey`, `SummaryTeX.collectSteps`, all `SummaryTeX` TeX helpers (`wrapMath`, `wrapCenter`, `section`, etc.), `DerivationTreeDot.escapeLabel`, `MatrixTeX.toTeXStyled`, `Matrix.copy`.
         - Adaptable (same structure, new function): GLL's `descriptorsTableToTeX` → RNGLR version (2 columns vs 4), GLL's `newDescriptorsToTeX` → RNGLR version (simpler descriptor format), GLL's `descriptorToTeX` → RNGLR version, `GssDot.toDotFromSets` → RNGLR label printers, `SummaryTeX.gllStepSection` → `rnglrStepSection` (different placeholder names).
         - New required: `RnglrParsingStep` type, `Rnglr.buildPathIndexWithSteps`, `RnglrStepVisualizer` module + `RnglrVisualizationStep` type, `Helpers.writeRnglrStepsVisualization`, `Helpers.findRnglrStepTemplate`, `RNGLR_step_template.tex`, LR automaton DOT with state highlighting wrapper, RNGLR GSS label printers.
    8.   All existing RNGLR tests must pass without changes. Step visualization must not alter algorithm behavior — `buildPathIndex` remains unchanged, `buildPathIndexWithSteps` must produce identical `PathIndex` output.
 203. [done] Add solution level `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` to turn all warnings to errors. Compile all projects, fix all problems.
 204. [done] Deduplicate GLL/RNGLR shared algorithm helpers (code review R1, R2, R3).
       1. Extract `collectActiveGss` into shared location — identical logic in `Gll.fs:50` and `Rnglr.fs:314`. Both iterate GSS edge matrix to collect `(vertex, edge)` sets. Place in `GraphHelpers` module (`GllTypes.fs`) or a new shared module.
       2. Extract `addToIndex` into shared location — structurally identical local function in `Gll.fs:110` and `Rnglr.fs:101`. Both compute linear indices, check Set.contains, mutate matrix. Place in `PathIndex` module since it operates on path index.
       3. Extract `linearIndex` formula `state * vertexCount + vertex` — appears in 3 modules: `GSS.linearIndex` (GllTypes.fs:83), `RnglrGSS.linearIndex` (RnglrTypes.fs:60), `PathIndex.linearIndex` (PathIndex.fs:42). Create single shared implementation.
       4. All existing tests must pass without changes.
 205. [done] Deduplicate test helpers for GSS DOT visualization (code review R4, R5, R6).
       1. Move `stripQuotes`, `vertexLabelRegex`, `edgeLabelRegex` from `GssDotVisualizationTests.fs` into shared `GoldenHelpers.fs` or a new `DotTestHelpers.fs` in `FLPQ.Printers.Tests`.
       2. Update both `GssDotVisualizationTests.fs` and `RnglrStepVisualizationTests.fs` to use shared helpers.
       3. All existing tests must pass without changes.
 206. [done] Deduplicate GLL/RNGLR twin grammar test modules (code review N2).
       1. Extract shared grammar definitions (grammar1-4) and common accept/reject test structures into a shared test module or parameterized test framework.
       2. Both `GllTests.fs` and `RnglrTests.fs` should reference the same grammar definitions rather than duplicating ~400 lines of identical test structure.
       3. Consider using `[<Theory>]`/`[<InlineData>]` or a shared test data module to parameterize tests over algorithm (GLL vs RNGLR).
       4. All existing tests must pass without changes.
 207. [done] Deduplicate `regexToDfa` / `buildBlockDfa` (code review N3).
       1. `regexToDfa` in `RPQTests.fs:237` duplicates `RsmBuilder.buildBlockDfa` (`EbnfParser.fs:268`). Both use Brzozowski derivatives.
       2. Either move shared implementation to a common location or have the test reuse `buildBlockDfa`.
       3. All existing tests must pass without changes.
208. [done] Deduplicate SPPF-extraction logic in CLI runners (code review N7).
       1. `gllTree`/`rnglrTree` share ~30 lines of nearly identical SPPF-extraction logic in `GllRunner.fs:26-52` and `RnglrRunner.fs:30-59`.
       2. Extract shared rootRanges construction and `Sppf.buildSppfFromIndex` calls into a common helper for SPPF.
       3. All existing tests must pass without changes.
209. [done] Remove empty XML doc comments (code review R7).
       1. Remove `///` on a line by itself in 5 locations: `MsBfs.fs:7,34`, `KroneckerRPQ.fs:10`, `ArroyueloRPQ.fs:9`, `BelyaninRPQ.fs:10`.
210. [done] Add test coverage for uncovered areas (code review 4.6, 4.7, 4.8, 4.9).
       1. Add direct unit test for `Nfa.epsilonClosure` — cover epsilon cycles, multi-step epsilon chains, self-loops.
       2. Extend RPQ generators beyond `["a"; "b"]` alphabet — add tests with larger alphabets and special characters.
       3. Add property-based tests (`[<Property>]`) for `Graph` operations: `filterOutgoing`, `filterIncoming`, `keepVertices`, `mapVertices`, `mapEdges`, `fromEdges`.
       4. Add dimension-consistency property test for `BooleanDecomposition.recompose` — verify all matrices in decomposition have same dimensions as original.
       5. All existing tests must pass without changes.
211. [done] Add LL(k>1) property-based equivalence tests (code review 4.4/N10).
       1. `LLParserTests.fs` has only `[<Fact>]` tests for k=2/k=3 with hardcoded strings.
       2. Add `[<Property>]` test comparing LL(k>1) acceptance against CYK/Valiant using FsCheck-generated grammars and inputs.
       3. All existing tests must pass without changes.
212. [done] Fix FsCheck property tests without registered Arbitrary (code review N9).
       1. `GllTests.GllCykEquivalence` (line 88) and `GllPropertyTreeYield` (line 520) produce mostly irrelevant inputs without `[<Properties(Arbitrary=...)>]`.
       2. Register custom Arbitrary generators so property tests receive meaningful inputs.
       3. All existing tests must pass without changes.
213. [done] Move `TokenStringGenerators` to shared `Generators.fs` (code review N6).
       1. `TokenStringGenerators` defined inline in `TokenizerTests.fs:144-151` instead of shared `Generators.fs`.
       2. Move to `FLPQ.TestUtilities/Generators.fs` for reuse by other test projects.
       3. All existing tests must pass without changes.
214. [done] Address GoldenHelpers.verifyGolden risk (code review 5.2).
       1. `GoldenHelpers.verifyGolden` creates golden files on first run when they don't exist — buggy output can be captured as golden.
       2. Add a safeguard: require explicit opt-in flag to create golden files.
       3. All existing tests must pass without changes.
215. Use [indexed properties](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/members/indexed-properties) to access Matrix elements instead of `get` and `set`.
     1.   Replace all usages of `get` and `set`
     2.   All tests must pass.

216. [done] RNGLR GSS visualization — show edge symbols to distinguish from GLL GSS.
     RNGLR GSS is a labeled automaton: edges carry grammar symbols (`RnglrGssEdge { EdgeSymbol: Symbol<'t,'nt> }`), and these symbols drive the BFS-based product intersection with inverted RSM blocks. Current GSS DOT visualization renders edges as bare `"lrState,v → lrState,v"` coordinate pairs — identical to GLL's GSS DOT output — hiding the fundamental structural difference. The visualization must show edge symbols so the RNGLR GSS reads as a labeled directed automaton.
     1.   `RnglrTypes.fs` — `RnglrParsingStep`: add field `ActiveGssEdgeSymbols: Map<int*int, NonEmptySet<Symbol<'t,'nt>>>` — maps each `(fromIdx, toIdx)` GSS edge pair to its grammar symbol(s). Multiple symbols on the same vertex pair are possible (different reduce/shift paths produce edges with distinct symbols between the same vertices).
     2.   `Rnglr.fs` — in step collection callback (`onStep` in `buildPathIndexWithSteps`), populate `ActiveGssEdgeSymbols` from the GSS: iterate active vertices, call `RnglrGSS.outgoingEdges`, build the `Map<int*int, NonEmptySet<Symbol<'t,'nt>>>` grouping symbols by edge pair.
     3.   `RnglrStepVisualizer.fs` — GSS DOT edge label printer: use `ActiveGssEdgeSymbols` to render comma-separated grammar symbols (e.g., `"a"` for terminal, `"S"` for nonterminal, `"a, S"` if multiple symbols share the same edge pair). Replace the current coordinate-pair label (not needed — vertex labels already show coordinates). Vertex labels unchanged for this task.
     4.   `GoldenHelpers.fs` — add RNGLR-specific `rnglrEdgeLabelRegex` matching symbol-only labels (e.g., `^"?[A-Za-z0-9ε, ]+"?$`), distinct from the current GLL `edgeLabelRegex` (`^\d+,\d+ → \d+,\d+$`).
     5.   `RnglrStepVisualizationTests.fs` — update GSS DOT vertex/edge label format test to use `rnglrEdgeLabelRegex` for RNGLR. Regenerate all 6 RNGLR golden files (`rnglr_gss_step0.dot`, `rnglr_descriptors_table_step0.tex`, `rnglr_new_descriptors_step0.tex`, `rnglr_path_index_step0.tex`, `rnglr_input_step0.dot`, `rnglr_lr_automaton_step0.dot`). All existing RNGLR tests must pass.
     6.   `GssDotVisualizationTests.fs` — unchanged (tests GLL GSS only). RNGLR GSS DOT tests remain in `RnglrStepVisualizationTests.fs`.

217. [done] RNGLR Descriptor refactoring — explicit GssIdx + continuous GSS vertex numbering from 0.
     Current `RnglrDescriptor = { LrState; Vertex }` derives the GSS vertex at every use site via formula `lrState * vertexCount + v`. GLL's `Descriptor` carries explicit `GssIdx` — RNGLR must too. Additionally, GSS pre-allocates all `|Q_lr| × |V|` vertices as a dense matrix; refactoring creates vertices lazily on-demand with sequential IDs (0, 1, 2, ...), decoupling GSS indexing from the PathIndex grid formula. PathIndex stays unchanged.
     **Prerequisite**: Task 216 (RNGLR GSS visualization) must be done first — this task inherits the edge-symbol-aware visualization and adapts vertex labeling for the new numbering scheme.
     1.   `RnglrTypes.fs` — `RnglrDescriptor`: add `GssIdx: int` field → `{ LrState: int; Vertex: int; GssIdx: int }`. Update XML doc comment to reflect 3-field structure.
     2.   `RnglrTypes.fs` — `RnglrGSS` type redesign from pre-allocated matrix to lazy on-demand structure:
          - Replace `GssGraph: Graph<RnglrGssVertex, Option<NonEmptySet<RnglrGssEdge<'t,'nt>>>>` with:
            - `VertexLookup: Dictionary<int*int, int>` — `(lrState, v) → gssIdx`, O(1) lookup
            - `VertexInfo: ResizeArray<int*int>` — reverse mapping `gssIdx → (lrState, v)`, O(1) random access
            - `Edges: Dictionary<int, Dictionary<int, NonEmptySet<RnglrGssEdge<'t,'nt>>>>` — adjacency map, sparse (only allocated for existing vertices)
          - Replace `StoredStates: Set<...>[]` with `StoredStates: Dictionary<int, Set<Nonterminal<'nt> * int * int * int>>` — resizeable, matches dynamic vertex creation
          - Remove `RnglrGSS.linearIndex` (formula no longer needed)
          - Remove `RnglrGSS.init` (no pre-allocation)
          - Add `RnglrGSS.create() : RnglrGSS<'t,'nt>` — returns empty GSS (all dictionaries empty, no vertices)
          - Add `RnglrGSS.getOrCreateVertex(gss, lrState, v) → int` — if `(lrState, v)` already has a GSS vertex, return its ID; otherwise allocate next sequential ID, add to `VertexLookup` and `VertexInfo`, return new ID
          - Add `RnglrGSS.getVertexInfo(gss, gssIdx) → int*int` — returns `(lrState, v)` for a GSS vertex; replaces `Graph.getVertex gssIdx gss.GssGraph`
          - Update signatures (no changes needed): `addEdge`, `outgoingEdges`, `getStoredStates`, `setStoredStates` — they already take `gssIdx: int` and work with indices; adapt internals to Dictionary-based storage
     3.   `GllTypes.fs` — `GraphHelpers` module: add `collectActiveGssForDict(edges: Dictionary<int, Dictionary<int, 'a>>) → Set<int> * Set<int*int>` for Dictionary-based edge storage. Same return type as the Matrix-based `collectActiveGss`. Iterates dictionary keys (source vertices) and nested dictionary keys (target vertices) to collect active vertices and edge pairs.
     4.   `Rnglr.fs` — core algorithm adaptation:
          - Replace `let gss = RnglrGSS.init lrStateCount vertexCount` → `let gss = RnglrGSS.create()`
          - Remove local `linearIdx` function
          - At each descriptor creation site:
            - Initial descriptor: `let gssIdx = RnglrGSS.getOrCreateVertex gss 0 0` → `{ LrState = 0; Vertex = 0; GssIdx = gssIdx }`
            - Shift target: `let targetGssIdx = RnglrGSS.getOrCreateVertex gss targetLrState vNext` → `{ LrState = targetLrState; Vertex = vNext; GssIdx = targetGssIdx }`
          - Replace all `Graph.getVertex idx gss.GssGraph` → `RnglrGSS.getVertexInfo gss idx` (in `productBfs`, `findPredecessors`)
          - Replace `GraphHelpers.collectActiveGss gss.GssGraph.Edges` → `GraphHelpers.collectActiveGssForDict gss.Edges`
          - `processedGotos`: replace `Set<Nonterminal<'nt> * int> array` of fixed size `lrK` with `Dictionary<int, Set<Nonterminal<'nt> * int>>` — keyed by `gotoGssIdx`, lazy allocation
          - In `productBfs`: use `RnglrGSS.getVertexInfo gss currGss` and `RnglrGSS.getVertexInfo gss nextGss` instead of `Graph.getVertex`
          - In `processReduction`: `gotoGssIdx` via `RnglrGSS.getOrCreateVertex gss gotoTarget vEnd`
          - Edge symbol collection (Task 216 code in step callback): adapt to use `RnglrGSS.outgoingEdges` (unchanged signature, just internal Dictionary-based iteration)
          - All existing algorithm logic preserves behavior — the refactoring changes only how GSS vertices are created and accessed, not what the algorithm computes
     5.   `RnglrStepVisualizer.fs` — visualization adaptation:
          - `rnglrDescriptorToTeX`: render 3-field descriptor `(lrState, vertex, gssIdx)` instead of `(lrState, vertex)`
          - `descriptorsTableToTeX`: header becomes `lrState & input & gssIdx` (3 columns instead of 2), `renderRow` renders 3 values
          - `newDescriptorsToTeX`: uses `rnglrDescriptorToTeX` — automatically picks up 3-field format
          - `renderStep` — `currentGssIdx` directly from `step.CurrentDescriptor.Value.GssIdx` instead of computing via formula
          - GSS DOT vertex label printer: use `RnglrGSS.getVertexInfo gss gssIdx` to get `(lrState, v)` for label `"idx: (lrState, v)"`. The visualizer needs access to vertex info — pass a lookup function `(int → int*int)` or carry `VertexInfo` in step data. Vertex indices are now sequential (0, 1, 2, ...) instead of formula-derived (0, vc, 2*vc, ...).
          - GSS DOT edge label printer: symbols from `ActiveGssEdgeSymbols` (from Task 216) — unchanged
     6.   `RnglrRunner.fs` — pass vertex info to visualizer if needed (e.g., `RnglrGSS.getVertexInfo` as a lookup lambda, or carry reverse mapping in step data).
     7.   Golden data — all 6 RNGLR golden files regenerated (descriptor now 3 fields, GSS vertex numbering is sequential, edge symbols already present from Task 216). Update `GoldenHelpers.fs` regex patterns if vertex label format changes.
     8.   Tests — `RnglrTests.fs` (all algorithm tests), `RnglrRunnerTests.fs` (CLI output), `RnglrStepVisualizationTests.fs` (golden + invariants) — all must pass. No skipped tests.
     9.   `docs/developer/rnglr.md` — update type definitions: `RnglrDescriptor` (now 3 fields), `RnglrGSS` (lazy vertex creation, Dictionary-based storage). Update GSS module functions table: remove `linearIndex`, `init`; add `create`, `getOrCreateVertex`, `getVertexInfo`. Update design decisions table: new entry "GSS vertices created on-demand with sequential IDs" replacing "Vertices pre-allocated as |Q_lr| * |V|"; update "RnglrDescriptor struct type" entry to reflect 3-field structure.

218. [done] RNGLR steps visualization — LR table with highlighted actions instead of LR automaton DOT.
     In each RNGLR step, replace the LR automaton DOT figure with the LR parsing table (ACTION/GOTO) and highlight the active state row and the specific cells consumed during this step. Highlighting scheme: current LR state row gets `\rowcolor{yellow!20}`; cells for actions taken this step (shift terminals + reduce nonterminals) get `\cellcolor{green!20}`; accumulated reductions across all descriptors at the current input vertex ("level reductions") get `\cellcolor{red!20}`. The table replaces the automaton completely — no more LR automaton DOT in steps.
     1.   `RnglrTypes.fs` — `RnglrParsingStep`: add three fields:
          - `ActiveShiftTerminals: Set<Terminal<'t>>` — terminals shifted this step (those with matching input edges from current vertex and SHIFT action in table)
          - `ActiveReduceNonterminals: Set<Nonterminal<'nt>>` — nonterminals reduced at this LR state (from `getReduceNtWithStates`)
          - `LevelReductions: Set<Nonterminal<'nt>>` — accumulated set of all nonterminals reduced across all descriptors processed at the current input vertex (resets when vertex changes)
     2.   `Rnglr.fs` — capture action data during step collection in `buildPathIndexWithSteps`:
          - Add mutable ref cells: `stepShiftTerminals: Set<Terminal<'t>> ref`, `stepReduceNt: Set<Nonterminal<'nt>> ref`, `levelReductions: Set<Nonterminal<'nt>> ref`, `prevInputVertex: int ref`
          - In `processNode`: before the reduce loop, add each `reduceNt` from `getReduceNtWithStates lrState` to both `stepReduceNt` and `levelReductions`. Before the shift loop, at the point where `lrTable.Action` confirms a SHIFT, add `Terminal tVal` to `stepShiftTerminals`. At start of `processNode`, if `v ≠ prevInputVertex.Value`, reset `levelReductions.Value` to empty and set `prevInputVertex.Value ← v`.
          - In step callback (`onStep`): populate the three `RnglrParsingStep` fields from the ref cells, then clear `stepShiftTerminals` and `stepReduceNt` (reset to empty). Do NOT clear `levelReductions` — it persists across steps at same vertex and resets only on vertex change in `processNode`.
     3.   `RnglrTableTeX.fs` — add `tableToTeXWithHighlights` function:
          - Signature: `tableToTeXWithHighlights (terminalPrinter: 't -> string) (nonterminalPrinter: 'nt -> string) (table: RnglrTable<'t,'nt>) (currentLrState: int option) (activeActions: Set<Symbol<'t,'nt>>) (levelReductions: Set<Nonterminal<'nt>>) : string`
          - Same tabular structure as `tableToTeX`. Per-row: if state equals `currentLrState`, wrap row with `\rowcolor{yellow!20}`. Per-cell: if the cell's symbol is in `activeActions`, wrap cell content with `\cellcolor{green!20}`. If the cell corresponds to a nonterminal in `levelReductions`, wrap with `\cellcolor{red!20}`. Color priority: red (level reductions) overrides green (active actions) which overrides yellow (row). Requires `\usepackage[table]{xcolor}` or `\usepackage{colortbl}` in preamble — note this in rendering (callers must include the package).
          - If `currentLrState = None` (initial step before any descriptor), render plain table with no highlights.
          - Keep existing `tableToTeX` unchanged (used for output-independent rendering of the full table, e.g., `rnglr_table.tex` in runner).
     4.   `RnglrStepVisualizer.fs` — visualizer changes:
          - `RnglrVisualizationStep`: replace `LrAutomatonDot: string` field with `LrTable: string` (TeX content, not DOT).
          - Remove `lrAutomatonToDot` private function entirely.
          - Remove `symbolToDotLabel` helper (only used by `lrAutomatonToDot`).
          - In `renderStep`: compute `activeActions = (Set.map (fun (Terminal t) -> Symbol.T(Terminal t)) step.ActiveShiftTerminals) + (Set.map (fun nt -> Symbol.N nt) step.ActiveReduceNonterminals)`. Call `RnglrTableTeX.tableToTeXWithHighlights` with `activeActions`, `step.CurrentLrState`, `step.LevelReductions`. Store in `LrTable`.
     5.   `Helpers.fs` — `writeRnglrStepsVisualization`:
          - Replace `writeOutputFile (Path.Combine(stepDir, "lr_automaton.dot")) steps.[idx].LrAutomatonDot` → `writeOutputFile (Path.Combine(stepDir, "lr_table.tex")) steps.[idx].LrTable`.
          - Remove any LR automaton DOT → PDF compilation from this function (LR table is inline TeX, no PDF needed).
     6.   `data/RNGLR_step_template.tex` — replace LR automaton PDF placeholder:
          - Remove: `\begin{center}\includegraphics[width=\textwidth,keepaspectratio]{__STEP_LR_AUTOMATON_PDF__}\end{center}`
          - Add: `\begin{center}\resizebox{\textwidth}{!}{$__LR_TABLE__$}\end{center}`
     7.   `SummaryTeX.fs` / `Summary.fs` — summary generation:
          - `SummaryTeX.rnglrStepSection`: replace `__STEP_LR_AUTOMATON_PDF__` placeholder substitution with `__LR_TABLE__` inline TeX substitution (read `lr_table.tex` file content directly, like `__DESCRIPTORS_TABLE__`).
          - `Summary.fs`: remove LR automaton DOT → PDF compilation for RNGLR (`compileDotArtifacts` for `lr_automaton.dot`). Remove `dot_pdfs/..._lr_automaton.pdf` entries from dot artifact lists.
     8.   Tests:
          - `RnglrStepVisualizationTests.fs`: replace golden test "lr_automaton step 0" → "lr_table step 0" (`rnglr_lr_table_step0.tex`). Remove `RNGLR LR automaton DOT compiles` test (no more DOT). Add `RNGLR LR table TeX compiles` test.
          - `RnglrRunnerTests.fs`: check `lr_table.tex` exists instead of `lr_automaton.dot` in per-step output.
          - Golden data: delete `rnglr_lr_automaton_step0.dot`, create `rnglr_lr_table_step0.tex`.
           - All existing RNGLR tests must pass. No skipped tests.

219. [done] Language Registry and test infrastructure refactoring. Create a specification document and typed F# registry module that catalogs all test languages, their grammars (with manually-verified properties: left-recursive, ambiguous, has-epsilon, in-CNF, etc.), accept/reject strings, and string generators. Group grammars strictly by formal language equivalence (same language = same group). Restructure TestGrammars.fs to re-export from the registry. Refactor all algorithm test files (CYK, Valiant, GLL, RNGLR, LL, LR, SharedParsingTests) to use registry types and helpers. Update tests-writer skill with a workflow section describing how to use the registry. No algorithm compatibility bindings in the registry — the developer uses grammar properties + algorithm knowledge to decide compatibility. No undecidable property detection — all properties are manual annotations verified by the developer.

220. [done] Refactor AnnotatedGrammar type. Current design has an IsEbnf boolean flag and uses a dummy parseG "S -> a" placeholder for EBNF-only grammars — ugly and error-prone. Replace with a clean design where the canonical text is the single source of truth, and both CFG (Grammar<string,string>) and RSM (RSM<string,string>) representations are derived from it. The AnnotatedGrammar record stores: Text (canonical CFG/EBNF text), Grammar, AugmentedGrammar, Rsm. No IsEbnf flag, no dummy grammars, no EbnfText field. For EBNF-only texts, Grammar is obtained via RsmToGrammar.rsmToGrammar as a fallback when Grammar.parseGrammar fails. In tests, stop calling TestHelpers.grammarToRsm and RsmBuilder.buildRSMFromText — use g.Rsm directly from the registry entry. The TestHelpers.checkReject function should accept RSM instead of Grammar to eliminate the internal grammarToRsm conversion. Drop the IsEbnf filter from collectAcceptFailures/collectRejectFailures — all entries have valid Grammar. No new types — only existing types. All 500 tests must pass with zero regressions.



221. [done] Comprehensive test refactoring: unify GLL/RNGLR tests, consolidate cross-parser equivalence, eliminate TestGrammars indirection, fix broken equivalence tests, merge duplicated generators.
    **Context**: After tasks 219-220 the LanguageRegistry is established as the single source of grammars, accept/reject strings, and generators. However, the test suite still has significant structural problems.

    **Problem 1: Copy-paste duplication between GllTests.fs and RnglrTests.fs**.
    The two files are near-identical mirrors (~360 and ~484 lines). Same submodules, same test structure, same grammars, same inputs — only the `accepts` binding differs. SharedParsingTests.fs parameterizes only 3 categories (epsilon, regex, tree-yield), leaving ~70% of tests duplicated:
    - `GllAcceptance` / `RnglrAcceptance` (~75 / ~80 lines) — basic accept/reject. Inconsistent grammar sources: some use `TestGrammars.grammarS2a`, others use `singleA.Grammars[0].Rsm`.
    - `GllGrammarAcceptanceAndTree` / `RnglrGrammarAcceptanceAndTree` (~50 / ~60 lines) — grammars 11-14. Structurally identical.
    - `GllGrammar159A-D` / `RnglrGrammar159A-D` (~30 / ~50 lines) — four single-test submodules each.
    - `GllRightNullable` / `RnglrRightNullable`, `GllReductionCascade` / `RnglrReductionCascade`, `GllPropertyTreeYield` / `RnglrPropertyTreeYield` — all duplicated.

    **Problem 2: Broken RNGLR vs GLL equivalence tests (always true)**.
    `RnglrTests.fs:110-113` and `:124-127` compare `accepts = accepts` (same binding on both sides). Always returns `true`. Provides zero coverage. Same bug in APlus variant.

    **Problem 3: Cross-parser equivalence scattered across 6 files**.
    12+ pairwise comparisons defined wherever convenient: GLL vs CYK in `GllTests.fs`, RNGLR vs CYK in `RnglrTests.fs`, Valiant vs CYK in both `ValiantTests.fs` and `LLParserTests.fs`, LL vs SLR/CLR/CYK in `LLParserTests.fs`, SLR vs CLR/CYK in `LRParserTests.fs`. No single place to see "do all parsers agree?"

    **Problem 4: TestGrammars.fs is an unnecessary indirection layer**.
    125 lines of aliases that look up from LanguageRegistry by name. After tasks 219-220, the registry has `AnnotatedGrammar` with `Grammar`, `AugmentedGrammar`, `Rsm`, `Text`. Tests should reference the registry directly.

    **Problem 5: Duplicated string generators**.
    `Generators.fs` and `LanguageRegistry.fs` both define `abStringGen`/`AbStringGenerators`, `aStringGen`/`AStringGenerators`, etc. with identical distributions and bounds.

    **Problem 6: Submodule proliferation**.
    10+ submodules per parser, many with a single test (`GllGrammar159A` = 1 test, `GllReductionCascade` = 1 test). Navigation noise.

    **Proposed structure**:
    ```
    tests/FLPQ.Languages.Tests/
      GllTests.fs                    — GLL-specific + shared acceptance via RSM adapter
      RnglrTests.fs                  — RNGLR-specific + shared acceptance via RSM adapter
      CykTests.fs                    — CYK-specific + shared acceptance via Grammar adapter
      LLParserTests.fs               — LL-specific + shared acceptance via Grammar adapter
      LRParserTests.fs               — LR-specific + shared acceptance via Grammar adapter
      ValiantTests.fs                — Valiant-specific + shared acceptance via Grammar adapter
      CrossParserEquivalenceTests.fs — NEW: all pairwise equivalence, consolidated from 6 files
      SharedParsingTests.fs          — DELETE (moved to TestUtilities)
      TestGrammars.fs                — DELETE

    tests/FLPQ.TestUtilities/
      ParsingTestCases.fs            — NEW: moved from SharedParsingTests.fs, extended with AcceptanceCase type, cases for ALL languages
      LanguageRegistry.fs            — Add Gen→Arbitrary bridge, consolidate string generators
      Generators.fs                  — Remove duplicated string generators (keep non-string)
      TestHelpers.fs                 — Unchanged (already parameterized from task 183)
    ```

    **Subtasks**:

    1. Move `SharedParsingTests.fs` to `FLPQ.TestUtilities/ParsingTestCases.fs`. Extend with shared acceptance cases for ALL languages.
       - Define `AcceptanceCase { CaseName: string; LanguageName: string; GrammarName: string; Rsm: RSM<string,string>; Grammar: Grammar<string,string>; Input: string list; ExpectedAccepted: bool }`.
       - Module `AcceptanceCases`: iterate all languages in `LanguageRegistry.allLanguages`, for each language iterate `Grammars`, for each grammar iterate `AcceptStrings` (expected=true) and `RejectStrings` (expected=false). Use prebuilt `AnnotatedGrammar.Rsm` and `AnnotatedGrammar.Grammar` from the registry — no conversion needed.
       - Module `TreeYieldCases`: shared tree-yield test cases (grammar + inputs that should produce trees with matching leaves). Source from `LanguageRegistry.Dyck1.AcceptStrings`, `APlus.AcceptStrings`, etc.
       - Module `RegexEquivalenceCases`: regex patterns + filter functions for DFA comparison.
       - Module `EpsilonCases`: all epsilon grammars from `LanguageRegistry.EpsilonOnly`.
       - Pure data, no `[<Fact>]` attributes, no parser-specific logic.

    2. Refactor `GllTests.fs` to use shared cases.
       - Replace `GllAcceptance` submodule with one `[<Fact>]` per grammar: iterate `ParsingTestCases.AcceptanceCases.forLanguage "Dyck1"` etc., for each case assert `accepts case.Rsm case.Input = case.ExpectedAccepted`. The fact name includes language and grammar names so failures are pinpointed.
       - Replace `GllGrammarAcceptanceAndTree` Grammar1-4 submodules with shared cases loop.
       - Merge `GllGrammar159A-D` into a single `GllTreeYield` submodule using `ParsingTestCases.TreeYieldCases`.
       - Keep GLL-specific tests: descriptor/GSS visualization, step traces.
       - Replace all `TestGrammars.xxx` references with direct `LanguageRegistry.xxx.Grammars[n]` access.

    3. Refactor `RnglrTests.fs` symmetrically.
       - Same pattern as GLL: shared cases for acceptance, tree yield, epsilon, right-nullable, reduction cascade.
       - Fold `RnglrGrammarTests` (lines 406-484) into shared cases — the `accepts` helper already validates SPPF which is stricter than raw RSM→path-index without SPPF validation.
       - Keep RNGLR-specific tests: SPPF construction (`SppfDotTests`), dual Dyck language tests.
       - Replace all `TestGrammars.xxx` references with direct registry access.

    4. Create `CrossParserEquivalenceTests.fs`.
       - Module `VsCyk`: GLL vs CYK, RNGLR vs CYK, Valiant vs CYK, LL vs CYK, SLR vs CYK, CLR vs CYK. Use prebuilt `AnnotatedGrammar.Grammar` for CYK/LL/LR/Valiant and `AnnotatedGrammar.Rsm` for GLL/RNGLR. Property-based with FsCheck generators from the registry.
       - Module `VsDfa`: GLL vs DFA, RNGLR vs DFA for regex-derived grammars. Move from `GllTests.fs` and `RnglrTests.fs`.
       - Module `GllVsRnglr`: direct comparison. Fix the broken test: define separate `gllAccepts = TestHelpers.accepts Gll.buildPathIndex PathIndex.isAccepted` and `rnglrAccepts = TestHelpers.accepts Rnglr.buildPathIndex PathIndex.isAccepted`, compare across them using shared cases from registry.
       - Module `LlVsLr`: LL vs SLR, LL vs CLR, SLR vs CLR. Move from `LLParserTests.fs` and `LRParserTests.fs`.
       - Module `ValiantVsCyk`: move from `ValiantTests.fs` and `LLParserTests.fs`.

    5. Refactor `CykTests.fs`, `LLParserTests.fs`, `LRParserTests.fs`, `ValiantTests.fs`.
       - Each uses shared acceptance cases via a Grammar adapter: `Grammar → Terminal list → bool`. One `[<Fact>]` per grammar, iterating `ParsingTestCases.AcceptanceCases.forLanguage "..."`.
       - Remove cross-parser equivalence tests (moved to step 4).
       - Keep algorithm-specific tests: CYK trace/table, LL conflict detection and k>1 resolution, LR automaton structure and conflict types, Valiant matrix comparison and modified Valiant.
       - Replace all `TestGrammars.xxx` references with direct registry access.

    6. Consolidate string generators.
       - In `LanguageRegistry.fs`: add a `GenToArbitrary` helper that wraps `Gen<string>` into `Arbitrary<string>`.
       - In `Generators.fs`: remove `AbStringGenerators`, `AStringGenerators`, `ExprStringGenerators`, `AbcdxyStringGenerators`. Replace with references to the corresponding LanguageRegistry generators via the wrapper.
       - Keep non-string generators in `Generators.fs`: `MatrixGenerators`, `LinearAlgebraGenerators`, `SetMatrixGenerators`, `RandomGraphGenerators`, `RPQGenerators`, `RPQExtendedAlphabetGenerators`, `IntersectionGenerators`, `RegexGenerators`, `RegexAndGraphGenerators`, `StressStringGenerators`, `StressNfaGenerators`, `StressRpqGenerators`, `StressMatrixGenerators`.
       - Update all `[<Properties(Arbitrary = [...])>]` attributes across test files to use the consolidated generators.

    7. Delete `TestGrammars.fs`.
       - Replace all references across test files with direct registry access.
       - Add a `findGrammar` helper to LanguageRegistry: `findGrammar (language: Language) (name: string) : AnnotatedGrammar` for cases where named lookup is clearer than index-based access.

    8. Run all tests. Zero regressions. All existing test assertions must pass. The refactoring changes how tests are organized and how grammars are accessed, but not what is tested or the expected outcomes.
 222. [done] Improve LR table inclusion to RNGLR smmary. 
      1.   In summary do not use additional inner centering.
      2.   In summary do not use wrapping to math $ ... $
      3.   In summary scale to 0.3\textwidth.
      So, final correct example:
      ```
      \begin{center}
      \resizebox{0.3\textwidth}{!}{%%
      \begin{tabular}{ c || c | c || c }
      & a & \$ & S \\ \hline
      \hline 0 & $s_1$ &  & 2 \\
      \hline 1 & $r_{S}$ & $r_{S}$ &  \\
      \hline 2 & $s_1$ & acc & 3 \\
      \hline 3 & $s_1$ & $r_{S}$ & 4 \\
      \hline 4 & $s_1$ & $r_{S}$ & 4 \\ [1ex]
      \end{tabular}
      }
      \end{center}
      ```
     Use function for table content generation, and reuse additional wrappers cretaion that reqiored for summary or for step generation.
223. [done] Fix RNGLR summary compilation. 
     1.   run `dotnet src/FLPQ.Cli/bin/Release/net10.0/FLPQ.Cli.dll -a rnglr -i data/example_input_a_a_a.txt -g data/example_grammar_a_a_a.bnf -s -o viz_output/glr`
     2.   compile result: `lualatex rnglr_merged.tex`
     3.   now i got error. Fix it. Error: 
     ```
     ! Package xcolor Error: Undefined color `lightblue'.

     See the xcolor package documentation for explanation.
     Type  H <return>  for immediate help.
     ...                                              
                                                       
     l.29 ...htblue!20}{\rule{0pt}{2ex}\rule{1.2em}{0pt}}
                                                       & Current GSS node \\
     ? 
     ```
      4.   Add summary compilation test.

 224. [done] Implement basic SPPF (Rekers-style) type. Current Sppf.fs implements the RSM-based SPPF with 5 node types. The basic SPPF is simpler — built directly from BNF productions with numbered rules, only 2 structural node types (symbol + production), matching the classical derivation tree structure.
       1.   Types in new file `src/FLPQ.Languages/BasicSppf.fs`:
            - `BasicSppfNodeInfo<'t,'nt>`: DU = `Terminal of Terminal<'t> * leftPos: int * rightPos: int` | `Nonterminal of Nonterminal<'nt> * leftPos: int * rightPos: int` | `Epsilon of pos: int` | `Production of ruleIndex: int * leftPos: int * rightPos: int`.
            - `BasicSppfEdgeLabel`: DU = `Derives` (nonterminal → production) | `ChildOf of positionInRhs: int` (production → child node).
            - `BasicSPPF<'t,'nt>`: record = `{ Graph: Graph<BasicSppfNodeInfo<'t,'nt>, Option<BasicSppfEdgeLabel>>; RootIndex: int }`.
       2.   Node semantics (per def:basicSPPF):
            - Terminal node ($a_{i,i+1}$): leaf, stores matched terminal at position $i$.
            - Nonterminal node ($X_{i,j}$): internal, one node per unique $(X, i, j)$. May have multiple production children (packing alternatives).
            - Epsilon node ($\varepsilon_i$): leaf, empty derivation at position $i$.
            - Production node (rule $k$): internal, stores rule index and span. Parent is single nonterminal node $(X_k)_{i,j}$. Children are terminal/nonterminal/epsilon nodes for each element of RHS $\alpha_k$, covering $[i, j)$ with split points.
       3.   Tree extraction function: similar to existing lazy trees enumeration for existing SPPF.
       4.   DOT visualization in `src/FLPQ.Printers/BasicSppfDot.fs`:
            - Nonterminal nodes: rounded rectangles with label `$X_{i,j}$`.
            - Terminal/Epsilon nodes: circles with label `$a_{i,i+1}$` / `$\varepsilon_i$`.
            - Production nodes: ovals with rule number.
225. [done] Reorganize RNGLR to canonical shift-then-reduce ordering with per-input-position visualization steps.
       1.   Reorder `processNode` in `Rnglr.fs`: shift first, then reduce. Shift creates terminal GSS edges and descriptors at V+1, then reduce phase cascades through the GSS (which now includes freshly-shifted terminal edges). storedStates from reductions at V are consumed during shifts at V+1 (canonical per-position semantics).
       2.   Change step capture from per-descriptor to per-input-position: move the `onStep` call from inside the per-descriptor `while` loop to after the while loop completes for each vertex. Step 0 stays initial empty state. Step 1 = after all descriptors at v=0, step 2 = after v=1, etc.
       3.   Reset `stepShiftTerminals`/`stepReduceNt` per-vertex instead of per-descriptor. `levelReductions` logic stays vertex-aware.
       4.   Update `CurrentDescriptor`/`CurrentLrState` to `None` for position-level steps (no single descriptor). `AttemptedDescriptors`/`NewDescriptors` reflect all descriptors from the entire vertex.
       5.   Update visualization: descriptors table shows ALL descriptors at the vertex in "to handle" block; "current" descriptor row removed.
       6.   Update golden data for step 0 (unchanged) and regenerate any other golden files. Update tests that check step-specific content.
       7.   Update `docs/developer/rnglr.md`: fix algorithm description (currently says "shift then reduce" but implementation did opposite), update design decisions.
       8.   All existing RNGLR tests must pass.
226.  [done] Extend CYK with data to construct BasicSPPF: each cell for each nonterminal stores all k that allows to easely reconstruct two childrenb cells, and respective production number.
227.  [done] Add indexed operations for Matrix: elementwize operations has access to element indices.
      1.    map2i --- as F# map2i or mapi.
      2.    mxmi --- op_mult and op_add has access to element indices
      3.    Add tests on indexed operations. Both facts and property.
228.  [done] extend Valiant and its modifiacton to compute information fo BasicSPPF. 
      1.    each cell for each nonterminal stores all k that allows to easely reconstruct two childrenb cells, and respective production number.
      2.    Use indexed operations for it.
      3.    Grammar may be captured using closure.
229.  [done] Add table to BasicSPPF function.
      1.    CYK and Valiand (+modified) build similar tables
      2.    Input is a table, output is a BasicSPPF
230.  [done] Add Tests on new Valiant and CYK. Extend existing tests to check complex invariants. Create common check fucntion. Be sure that all respective tests are property tetst.
      1.    For same grammar and input CYK, Valiant, Modified Valiant built exactly the same tabales
      2.    SPPF extracted from Tables for same grammar and input CYK, Valiant, Modified Valiant, exactly the same
      3.    Leaves of tree extracted from SPPF is a input string.
      4.    Implement function that treats BasicSPPF as a directed graph and computes number strongly conneted components (SCC). 
      5.    For same grammar and input CYK, Valiant, Modified Valiant built SPPF with identical numer of SCC.
231.  [done] Improve CYK and Valiant visualization. 
      1.    Improve cell content rendering: sell is a set of tuples of form <nonterminal>,<k>,<prod_id>
      2.    Add final SPPF visualization. Include SPPF to summary.
232. [done]  Add one more property check for SPPF checking for GLL and RNGLR
      1.    Treat SPPF as directed graph, implement algorithm to compute number of nontrivial strongly connected components (SCC). Nontrivial --- number of vertices more than one. Can algo for BasicSPPF be reused or generalized?
      2.    Add invariant: for same grammar and input SPPF from GLL and from CYK has same number of nontrivial SCC.
      3.    Add invariant: for same grammar and input SPPF from GLL and from RNGLR has same number of nontrivial SCC.
      4.    Add invariant: for same grammar and input SPPF from GYK and from RNGLR has same number of nontrivial SCC.
233.  [done] Store AcceptStrings and RejectStrings in LanguageRegistry as `Terminal<string> list` (pre-tokenized). Adapt helpers and callers to consume tokenized strings directly, removing ~175 `tokenizeTerminals`/`List.map Terminal` conversions throughout the test suite. Update `terminalsToGraph`, `accepts`, `checkReject`, `collectAcceptFailures`, `collectRejectFailures` signatures to accept `Terminal<string> list`. Remove `tokenized` and `acceptStrToSpace` helpers from test files.
234. [done] Extend GllVsRnglr property tests on all existing grammars and languages from language registry. Design it to avoid massive code duplication.
      1.    Note that some new tests may fail. Fix respective algortihm.
      2.    Note that SCC-s count for RNGLR looks more correct. So, be careful with algorithms analysis and fixes.
235. [done] Fix grammar2 filter workaround in CrossParserEquivalenceTests GllVsRnglr. The grammar2 (S -> a S b | eps | S S) is filtered out (`g.Name <> "grammar2"`) because GLL and RNGLR produce different SCC counts. The root cause is that GLL uses PEpsilonNonterminal in places where RNGLR uses PNonterminal, creating structurally different SPPFs. Analyze GLL carefully and fix the PEpsilonNonterminal/PNonterminal discrepancy so that grammar2 produces matching SCC counts without the filter.
236. Reorganize GllVsCyk and RnglrVsCyk tests in CrossParserEquivalenceTests using the GllVsRnglr iteration pattern.
     1.    Replace individual `[<Property>]` tests (one per grammar: Dyck1 grammar1/grammar2, APlus grammar3/grammar4) with `[<Fact>]` + `Check.One` tests that iterate over all languages and all their grammars, grouped by string generator type.
     2.    Compare acceptance only — CYK does not produce an SCC-compatible SPPF, so no SCC comparison.
     3.    GLL/RNGLR: use `TestHelpers.accepts` with `g.Rsm` (already precomputed in AnnotatedGrammar). CYK: use `TestHelpers.cykAccepts` with `g.Grammar`.
      4.    Minimal changes — no new types, no new helpers, no CNF precomputation.

237. [done] Improve GLL visualization. Split initialization from steps. Initialization draws initial state with the same template, but no new descriptors, no highlighted vertices in RSM, GSS, input, no highlighted descriptor in descriptors table. Just initial descriptor in it. For steps flow we visualize one descriptor handling. Table contains descriptors at start of step. Current descriptor highlighted. Current position, RSM state, GSS vertex highlighted. Changes highlighted: new vertices and edges in GSS, updated cells in path index.
      1.   Add `renderInit` function to `GllStepVisualizer.fs` — renders init step with zero highlights: GSS without `currentVertex`/`highlightedVertices`/`highlightedEdges`, RSM without `highlightedState`, input without position highlight, path index via `PathIndexTeX.toTeX` (no cell highlights), new descriptors as `\{ \emptyset \}` (no green/red boxes).
      2.   Dispatch init vs regular step in `renderSteps` — detect `CurrentDescriptor = None` (uniquely identifies init) and call `renderInit` instead of `renderStep`.
      3.   Label step 0 as "Initialization" in summary builder — `SummaryTeX.gllStepSection`: when `stepNum = 0`, use section title `"Initialization"` instead of `"Step 0"`.
      4.   Fix `verifyGssDots` in `GssDotVisualizationTests.fs` — currently asserts `blueCount == 1` for all dots including init. After fix, first dot (init) must have `blueCount == 0`; remaining dots `blueCount == 1`. Each guarded by `NodeCount > 0`.
      5.   Fix step-highlight tests in `GllRunnerTests.fs` — current tests check only hardcoded non-init steps (5, 12, 19). Add tests asserting step 0 has NO highlights. Update existing tests to iterate all step dirs: step ≥ 1 must have highlights; step 0 must NOT.
 238. [done] Add tikz visualization for GSS, SPPF, RSM, input graph.
      1.   Reuse existing functions for automata to tikz visualization.
      2.   Use tikz visualization as default for GLL (steps and summary). Reuse existing CLI flag to switch to dot. 
      3.   Use tikz visualization as default for RNGLR (steps and summary). Reuse existing CLI flag to switch to dot.
 239. [done] Improve GLL Tikz GSS visualization (preserve DOT viaualization "as is")
      1.   Use R-based notation for ranges on edges. Like ranges in descriptor table
      2.   Use rounded rectangle shape for vertices.
 240. [done] Fix toCNF function. 
      1.   To CNF conversion must include gramar cleanup (as last step of transformations): non-generating and unreachable nonterminals must be removed. Note: order of removing is important. Wrong order leads to incorrect result. Add these two cleanup subcteps. Simple test is grammar S -> a; S -> S S; S -> S S S. Currently it contains N_2 -> a rule in CNF where N_2 is unreachable.
      2.   Add tests. Add two functions: one checks that all nonterminals in grammar reachable from start nonterminal, one checks that each nonterminal in grammar can produce terminal or empty string. both applicable for grammar in BNF. Tests: for all grammars in language registry convert grammar to CNF and use cretaed functions to check that there are no non-generating and unreachable nonterminalsin result. 
 241. [done] Improve CYK and Valiant (+ modified) table rendering. Render each nonempty cell as a set of tuples of form (nonterm, split_point, prod_id).
 242. [done] Improve BasicSPPF creation and visualization (rendering). 
      1.   SPPF must be built only for start nonterminal in respective cell (if string accepted). Not for all nonterminals from all cells.
      2.   Do not mark edges with `derives` and numbers.
      3.   Production node store not left and right positins, but split point. Render it with respective lable of form `split_point, prod_id`
      4.   In rendering: for nontermonal node label use same form as in nonterminal node lable in rsm sppf rendering: `nonterm [from,to]`
      
 243. Add tikz rendering for BasicSPPF.
      1.   Use ` \graph` with `layered layout`. (look at automata tikz layout for example)
      2.   Use tikz as default for CYK and Valiant BasicSPPF visualization. Use existing CLI arg to switch to dot.
 246. Refactor LanguageRegistry: semantic naming, distribute MiscTestGrammars, improve isEbnfText.
     1.   Rename all grammars from numbered (`grammar1`, `grammar3`, ...) to semantic names (`dyckAmbiguousEps`, `aPlusRightRecursive`, ...).
     2.   Distribute MiscTestGrammars entries to existing languages (SingleA, SingleAB, AStar, Dyck1, ArithExpr) or promote to new full-featured languages (DoubleA, AOrEps, ABPlus, FourTerm, MixedPairs, AX, SingleB).
     3.   Keep 5 pure infrastructure grammars (parseGrammar/tocnf tests) in a dedicated `TestInfraGrammars` language.
     4.   Improve `isEbnfText`: use the existing EBNF parser's Regexp AST — parse text, walk each rule's AST; if ANY rule contains `RAlt` or `RStar` → true EBNF; if ALL rules are pure concatenation (RTerm, RNonterm, RSeq, REps only) → plain CFG. Ban `+`, `*`, `?`, `|`, `(`, `)` as terminal names.
     5.   Update all test call sites for renamed grammar references.
     6.   Run all tests — zero regressions.

245. [done] Fix all Language Registry Violations from @tasks/code_review.md Section 7.
     1.   Add missing grammars to LanguageRegistry: ClassicArithExpr (with +/*/(/) operators as terminals), ANB-like grammars, chain/cascade grammars, and any other edge-case grammars used in test files but not yet in the registry.
     2.   Fix 7.1 (RV1-RV4): Replace hardcoded Grammar.parseGrammar calls in non-printer test files (GrammarTests.fs, FirstFollowTests.fs, StressTests.fs, PathIndexTeXTests.fs) with LanguageRegistry references.
     3.   Fix 7.2 (RV5-RV16): Replace hardcoded Grammar.parseGrammar calls in printer/golden test files (TexCompilationTests.fs, AutomatonVisualizationTests.fs, LLVisualizerTests.fs, LRVisualizerTests.fs, LRStepsGoldenTests.fs, LRTableTeXGoldenTests.fs, GrammarTeXGoldenTests.fs, LLStepsGoldenTests.fs, LLTableTeXGoldenTests.fs, ValiantTraceGoldenTests.fs, CykSummaryGoldenTests.fs, DerivationTreeVisualizationTests.fs) with LanguageRegistry references.
     4.   Fix 7.3-7.4 (RV17-RV23): Replace hardcoded RsmBuilder.buildRSMFromText calls in test files with LanguageRegistry references. For files testing the builder itself (EbnfParserTests.fs, RSMTests.fs, RsmToGrammarTests.fs), add representative grammars to the registry and reference them.
     5.   Fix 7.5 (RV24-RV26): Remove TestHelpers.grammarToRsm and TestHelpers.grammarToEbnfText. Replace all ~29 call sites of grammarToRsm with direct g.Rsm access. Keep LanguageRegistry.grammarToEbnfText (private) and buildRegexRsm.
     6.   Fix 7.6 (RV27): Replace data/*.bnf file references in CLI tests with temp files created from LanguageRegistry Text entries. Remove the bnf files or ensure they're generated on-demand.
     7.   Run all tests — all must pass with zero regressions.

244. Inprove GLL rendering
      1.   Use adjustbox from package adjustbox to scale tikz figures. ```\begin{adjustbox}{max width=\textwidth}\begin{tikzpicture} ... \end{tikzpicture}\end{adjustbox}```
      2.   `v0 [label={[font=\tiny, anchor=north west, xshift=-1.01mm, yshift=1.01mm]north west:1} ,as={(0,0)}];`
