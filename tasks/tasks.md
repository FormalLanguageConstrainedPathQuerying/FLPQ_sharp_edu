* This file contains user-defined tasks. Do not modify them. Only track status of tasks in this file.
* **Strict rule: When marking a task as done, ONLY prepend `[done] ` to the existing task line (between task number and task formulation). NEVER rewrite, reformulate, shorten, or replace the task description itself. The task text is immutable — only the status tag may change.**
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