* This file contains user-defined tasks. Do not modify them. Only track status of tasks in this file.
* **Strict rule: When marking a task as done, ONLY prepend `[done] ` to the existing task line. NEVER rewrite, reformulate, shorten, or replace the task description itself. The task text is immutable — only the status tag may change.**
* The book is in TeX. Root of the book in `../../`.
* In some tasks Russian may be used to simplify references to the book that is in Russian.

1. [done] Init infrastructure. Create solution and initial projects. Set CI config up. CI must build the solution on Ubuntu, Windows, MacOS. In Release and Debug. Check formatting, run tests.
2. [done] Add generic matrix type (as wrapper around standard 2d array) and module with genetic operations:
    * map2: ('a -> 'b -'>c) -> Matrix<'a> -> Matrix<'b> -> Matrix<'c> 
    * transpose
    * map
    * Helper functions for matrices creation and initialization.
   
    For property tets use the following facts:
    * map2 with commutative operation is commutative
    * repeated transpose is identity
    * sequence of map-s is a single map with composition of functions
    
    Add printing of matrix to TeX using [nicematrix](https://ctan.org/pkg/nicematrix) package. Matrix printer is parametrized by function to print cell. Numbers of columns and rows may be optionally printed. 

3. [done] Add module for linear algebra over generic matrices.
    * mxm: Matrix<'a> -> Matrix<'b> -> ('a -> 'b -> 'c) (*opMult*) -> ('c -> 'c -> 'c) (*opAdd*) -> 'c (*zero*) -> Matrix<'c>  //general matrix-matrix multiplications. Classical inner loops. 
    * kron: Matrix<'a> -> Matrix<'b> -> ('a -> 'b -> 'c) (*opMult*) -> 'c (*zero*) -> Matrix<'c>  //Kronecker product
  
    For property tests use the following facts:
    * Kronecker product where one of matrices has form 1x1 can be expressed using map over second matrix.
    * For square matrices there exist identity matrix
    * transpose(mxm a b) = mxm (transpose b) (transpose a) 

4. [done] Add support for BNF grammar reading. File with extension `.bnf`. Format: one rule per line, file may contains empty lines, each rule has form `<nonterm> -> <sequence of terminals and nonterminals separated by space. Or eps --- special mark for epsilon>`. Start nonterminal is a lef part of the first rule. Nonterminal is any word start from capitalized letter, in PascalCase. Terminal is any word in camelCase. Respected types must be generic: `type Terminal<'t> = Terminal of 'T`. Similarly for nonterminals. In the future we will use not only strings to identify noterminals and terminals.
5. [done] Implement transformation of the grammar in BNF to the Chomsky normal form.
6. [done] Implement CYK algorithm. Use `Matrix<Option<HashSet<Symbol<'t, 'nt>>>>` to represent working table. Initial matrix and matrix on each step may be printed. Empty cell (`None`) printed as `\cdot`. Example of tests:
   1. Grammar: 
   ```
   S -> a S b S 
   S -> eps
   ```
   Acceptable strings: abab, ab , <empty string>, aabb, aababb
   Not-acceptable strings: aa, bb, abb, abba, b, a, ababa
   2. Grammar: 
   ```
   S -> a S b  
   S -> eps
   S -> S S
   ```
   can be used for property-based tests. This grammar specifies exactly the same language as previous one.
   3. Grammar: 
   ```
   S -> a S
   S -> a
   ```
   Acceptable strings: a, aa, aaaa, aaaaa
   Not-acceptable strings: <empty string>
   4. Grammar: 
   ```
   S -> S a   
   S -> a
   ```
   can be used for property-based tests. This grammar specifies exactly the same language as previous one.
   5. Grammar: 
   ```
   S -> S S
   S -> S S S
   S -> a
   ```
   can be used for property-based tests. This grammar specifies exactly the same language as previous one.
7. [done] Implement Boolean decomposition of matrices over sets (read the book to investigate it). Boolean matrix is a case of generic Matrix. Add functions to convert matrix over set to boolean decomposition and back.
8. [done] Implement standard Valiant algorithm. Use boolean decomposition to represent set of boolean matrix. Use implemented generic functions to operate with matrices. Add more functions and tests if necessary (eg slices may be needed) to Use property-based testing: CYK and Valiant must return identical results (both acceptance status and final tables).
9. [done] Refactoring:
  1. Create common base of strings and grammars for all algorithms.
  2. Move boolean decomposition tests out of Valiant tests.
  3. Final table for both CYK and Valiant is a matrix over set, not its boolean decomposition.
  4. Add more property-based tests: for fixed grammar and random string tables for Valiant and CYK must be the same.
  5. Update all documentation.
10. [done] Extract ground truth (accept/reject string lists) into shared TestGrammars.fs.
11. [done] Add more grammars and strings for tests for all parsing algorithms. All three new  grammars specify the same language, so they must be used for both typical unit tests and for property-based tests.
    1.  Grammar:
    ```
    S -> x
    S -> S + S
    S -> S * S
    S -> ( S )
    ```
    Accept: x, (x), (x)*x, x+x, x+x*x, x*(x+x), (x*(x+x))
    Reject: <empty string>, (), +x, x+, x+()
    2. Grammar:
    ```
    E -> E + T
    E -> T
    T -> T * F
    T -> F
    F -> ( E )
    F -> x
    ```
    Accepts and rejects the same as for previous grammar
    3. Grammar:
    ```
    E -> T + E
    E -> T
    T -> F * T
    T -> F
    F -> ( E )
    F -> x
    ```
    Accepts and rejects the same as for previous grammar
12. [done] Refactor to CNF transformation. Binarization must be before epsilon rules removing.
13. [done] Implemnt `first_k` and `follow_k` computations.
14. [done] Implement deterministic and nondeterministic finite automaton.
15. [done] Implement construction of LL(k) parsing table
16. [done] Implement LL(k) parsing table interpreter with derivation tree building (aka LL(k) parser).
17. [done] Implement LR(0) and LR(1) automata as a cases of deterministic finite automata where states parametrizes with respective types (sets of respective items). 
18. [done] Implement CLR(1), SLR(1), and LR(0) parsing tables creation. More information in the book.
19. [done] Implement interpreter of LR tables with tree creation (aka LR parser). Use grammars grammar1, grammar2, grammar3, and lsat two from tak 11 for tests. Be careful: not all of them LR(0). To check tree use the fact that concatenation of leaves is an input string (modulo epsilon leaves)
20. [done] Refactoring
    1.  Documentation on LL missed. Fix it
    2.  Move derivation tree an operations over it (eg leaves collecting) to separated module
    3.  Remove `states.Length > 500` check. It looks like hack. It the algorithm correct, it is not necessary to check number of states.
    4.  Add more property based tests
        1. Check that LL and LR accepts and rejects simultaneously (for appropriate grammars).
        2. Check that LL and Valiant accepts and rejects simultaneously (for appropriate grammars). 
        3. Check that LR and CYK accepts and rejects simultaneously (for appropriate grammars).
        4. When CYK and Valiant both reject, tables still mst be identical.
        5. Make firstK and followK more generic: they can be applied for grammars not only over strings
        6. Add property-based tests for boolean decomposition: composition of compose and recompose is identity.
        7. Use grammar6, grammar7 and grammar8 for property-based tests for all parsing algorithms. They all define the same language.
    5. Create common tokenizer for all parsing algorithms. Grammars may contains multy-symbol terminals, so, let suppose that terminals separated by spaces in the input string.
    6. Remove stubs like Tests.fs and Library.fs.
    7. Split both sources a tests into two projects. One for linear algebra, one for languages. 
  21. [done] Refactoring. In LL and LR you suppose that grammar is over string. Make code generic. We can handle grammars over arbitrary symbols.
  22. [done] Implement automata visualization. Use dot for visualization. Visualizer must be parametrized by states visualization function. Use double circle for final states. Fill start states with green. Add tests that check that generated dot string can be saved to file and compiled with graphviz dot without errors. You can use -TPlain output format to briefly check compilation result. Graphvis is installed.
  23. [done] Implement derivation tree visualization using dot. As far as nonterms and terms are generic type, visualizer must be parametized with symbols visulaization. Add tests that check result compilation. 
  24. [done] Add LL parser steps visualization. Each step visualize three parts: derivation tree, current stack (tex, one-row nicematrix, bottom is left), input (tex, full input with marked current position).  Add tests. Introduce struct type to represent visualization result of step. 
  25. [done] Add LR parser steps visualization. Each step visualize three parts: derivation tree, current stack (tex, one-row nicematrix, bottom is left), input (tex, full input with marked current position). Add tests.
26. [done] Some tests require graphviz installed. Mark such tests (use `[<Trait("Category", <category_name>)>]`) to be able to run them only in appropriate environment. Improve CI. For ubuntu add installation of graphvis (from apt). All tests, excluding tests that require graphviz, must be executed on all operation systems. Tets that require graphviz must be executed only on ubuntu.
27. [done] Improve graphs and trees visualization tests. Generated with -Tplain files contain text with information about graph layout that can be easily parsed. Check that the file contains expected data (eg expected number of nodes and edges).
28. [done] Refactor grammar representation types. Now it is still possible crete production with empty right part that is incorrect. Use NonEmptyList (https://fsprojects.github.io/FSharpPlus/reference/fsharpplus-data-nonemptylist.html) from FSharpPlus (https://www.nuget.org/packages/FSharpPlus) to prevent it. Right part of rule is non empty list of symbols or epsilon. Do not forget to update technologies.md.
29. [done] Introduce epsilon transition in finite automata explcitely. Similarly to grammars. Use NonEmptySet (https://fsprojects.github.io/FSharpPlus/reference/fsharpplus-data-nonemptyset.html) from FSharpPlus (https://www.nuget.org/packages/FSharpPlus) to avoid incorrect empty sets of symbols in transition matrix.
30. [done] Analyze code and use non-empty-list and non-empty-set where it is required.
31. [done] Split deterministic and nondeterministic automaton on type level. Deterministic has exactly one start state and does not have epsilon transitions.
32. [done] Remove code duplication for algorithms steps visualization. Just collect information for visualization during regular execution. Moreover, in some cases collected information contains part of regular result. E.g. for CYK table from last step is exactly resulting table. The same for tree in LL and LR.
33. [done] Improve Matrix visualization. 
      1.  It must has a parameter that specify cells to highlight (and color for highlighting). 
      2.  It must provide ability to border and fill specified submatrix.
   34. [done] Improve CYK visualization. Highlight each step modified cells.
   35. [done] Add steps visualization for Valiant. Highlight each step modified cells. Visualize both boolean decomposition and recomposed matrix. Use \cdot for false and empty cells. Use 1 for true. Border and fill submatrices processed at he step. Use different colors to highlight resulting submatrix and "input" submatrices.
   36. [done] Create console application to run algorithms. Use Argu to create CLI. interface must allow to user specify algorithm, input files, output directory. For parsing algorithm (CYK, Valiant, LR, LL): file with input string, file with input grammar, root directory to write steps visualization. Each step is a separated subdirectory. One artefact per file (eg for LR three files: stack, input, tree). Create common helpers to read grammars and strings from files. Create common helpers to write TeX and Dot files. For TeX files: in output file must be only code for visualization. No any standard headers or similar stuff.
   37. [done] Add tests that check that TeX files compiles. Introduce respective tests category. This tests require TeX installed. These tests must be executed local only. No CI environment with TeX installed. For tests it is necessary to generate full-featured TeX document. Create respective template and reuse it. You can create TeX file and the include generated parts of code into it.
   38. [done]  Add more TeX compilation tests. For Valiant. For CYK for all steps (similar to LR tests). Fix LL test: it dose not check that generated steps compiles successfully.
   39. [done]  Perform code review. Analyze code and tests. What architectural problems you can detect? What code quality problems you can detect (unnecessary duplicates, poor coding stile, unclear structure or naming)? DO NOT FIX ANYTHING. Just generate report to code_review.md. 
   40. [done] Make CYK and Valiant generic to be able to handle terminals of arbitrary type (similarly LL and LR)
   41. [done] Refactor `buildLR0Table`, `buildSLR1Table`, `buildCLR1Table` to avoid code duplication.
   42. [done] Valiant must use boolean decomposition instead explicit set of matrix.
   43. [done] Refactor `checkDotCompiles` and `checkDotCompilesWithInfo` to avoid code duplication.
   44. [done] Refactor `LR0Item`/`LR1Item` to use camelCase for fields.
   45. [done] Refactor LRParserTests.fs to avoid code duplication across submodules. Create parametrizable tests.
   46. [done] Check that documentation is up to date.
   47. [done] Some previous tasks fix problems described in code_review.md. Update code review: remove problems solved.
   48. [done] Refactoring of all parsing algorithms visualization. Paring algorithms must collect and return data for visualization represented as F# data structures (add necessary types). After that, collected data may be converted to tex using appropriate shared standalone function. E.g. to TeX conversion for LL and LR are te same. Some parts can be reused also for CYK and Valiant.
   49. [done] In LR parser use unified stack for states and symbols instead of two separated stacks: stack frame is state or symbol. Create respective type for frame. Improve visualizer respectively. 
    50. [done] Add LL(k) parsing table visualization to TeX using the exact same tabular format as in the book.
        The book (Chapter 7, `04_TopDown.tex`) displays the LL parsing table for grammar $S \to aSbS \mid \varepsilon$ as:
        \begin{center}
        \begin{tabular}{ r || c | c || c | c | c }
        N & $\first$ & $\follow$ & a & b & $\$ $ \\ \hline
        $S$ & $\{ a, \varepsilon \}$ & $\{ b, \$ \}$ & $S \rightarrow aSbS$ & $S \rightarrow \varepsilon$ & $S \rightarrow \varepsilon$
        \end{tabular}
        \end{center}
        Specification of the generated TeX — exact column layout and content.
        Wrapper: \begin{center}...\end{center}. Environment: \begin{tabular}{ r || c | c || c | c | ... | c }.
        Column 1 `r` (right-aligned) — header `N`, one cell per nonterminal with the nonterminal name (e.g. `$S$`).
        Column 2 `c` — header `$\first$`, FIRST set (e.g. `$\{ a, \varepsilon \}$`).
        Column 3 `c` — header `$\follow$`, FOLLOW set (e.g. `$\{ b, \$ \}$`).
        Double bar `||` separates metadata columns (N, FIRST, FOLLOW) from action columns.
        Columns 4+ `c` — one per terminal + `$\$ $` for end marker. Header is the terminal name (e.g. `a`, `b`) and `$\$ $` for end marker. Single bars `|` between individual terminal columns.
        \hline after the header row. One row per nonterminal.
        N column: nonterminal in math mode, e.g. `$S$`.
        \first column: set notation in math mode, e.g. `$\{ a, \varepsilon \}$`. Use `$\varnothing$` for empty set.
        \follow column: set notation, same style.
        Terminal/lookahead columns: production rule in math mode using `\rightarrow`, e.g. `$S \rightarrow aSbS$`, `$S \rightarrow \varepsilon$`. If no rule exists for a (nonterminal, lookahead) pair — cell is empty.
        Use `\rightarrow` in math mode (not `\to`); for epsilon-productions display `$\varepsilon$`. Epsilon in other contexts: `\varepsilon`.
        Tests: generate TeX for LL(1) table of grammar S -> aSbS | eps and verify it compiles with TeX (use existing TeX compilation test infrastructure, name category accordingly). Verify generated TeX contains expected structural elements: correct number of \hline, correct column count, nonterminal names, production rules. For a grammar with multiple nonterminals (e.g. grammar2 from task 11), verify the table has correct number of rows and production entries.
    51. [done] Add LR(0), SLR(1), CLR(1) parsing table visualization to TeX using the exact same tabular format as in the book.
        The book (Chapter 7, `05_BottomUp.tex`) displays the abstract LR table structure as:
        \begin{center}
          \begin{tabular}{c||c|c|c|c|c||c|c|c|c}
             States & $t_0$   &$\dots$ & $t_a$   & $\dots$ & \$      & $N_0$   &$\dots$ & $N_b$   & $\dots$  \\ \hline \hline
            $\dots$ & $\dots$ &$\dots$ & $\dots$ & $\dots$ & $\dots$ & $\dots$ &$\dots$ & $\dots$ & $\dots$  \\ \hline
            $10$    & $\dots$ &$\dots$ & $s_i$   & $\dots$ & $r_k$   & $\dots$ &$\dots$ & $j$     & $\dots$ \\ \hline
            $\dots$ & $\dots$ &$\dots$ & $\dots$ & $\dots$ & $acc$ & $\dots$ &$\dots$ & $\dots$ & $\dots$
          \end{tabular}
        \end{center}
        And the concrete SLR(1) table for grammar $S \to aSbS \mid \varepsilon$ (states 0–6):
        \begin{center}
        \begin{tabular}{c||c|c|c||c}
                     & a        & b     & \$    & S \\ \hline
            \hline 0 & $s_3$    & $r_1$ & $r_1$ & 1 \\
            \hline 1 &          &       & acc   &   \\
            \hline 2 &          &       &       &   \\
            \hline 3 & $s_3$    & $r_1$ & $r_1$ & 4 \\
            \hline 4 &          & $s_5$ &       &   \\
            \hline 5 & $s_3$    & $r_1$ & $r_1$ & 6 \\
            \hline 6 &          & $r_0$ & $r_0$ &   \\ [1ex]
        \end{tabular}
        \end{center}
        Specification of the generated TeX — exact column layout and content.
        Wrapper: \begin{center}...\end{center}. Environment: \begin{tabular}{c||c|c|c|...|c||c|c|...|c}.
        Column 1 `c` — state number column. Header is empty (unnamed) or `States`.
        Double bar `||` separates state column from ACTION columns.
        ACTION columns `c` — one per terminal + `\$` for end marker. Header is the terminal name (e.g. `a`, `b`) and `\$`. Single bars `|` between individual terminal columns.
        Double bar `||` separates ACTION columns from GOTO columns.
        GOTO columns `c` — one per nonterminal (excluding the augmented start nonterminal, e.g. `S'`). Header is the nonterminal name (e.g. `S`, `B`, `C`). Single bars `|` between GOTO columns.
        Header: first row with column labels. The state column header is either empty (concrete examples) or `States` (abstract). Ends with `\\ \hline`.
        Data rows: one row per automaton state. Each row starts with `\hline N` where `N` is the state number (bare number, not math mode).
        ACTION cells: `$s_n$` — shift and go to state n (e.g. `$s_3$`); `$r_n$` — reduce by rule n (e.g. `$r_1$`); `acc` — accept (bare, not in math mode); multiple entries in conflict: comma-separated (e.g. `$s_3$, $r_1$` for shift-reduce conflict); empty cell — error (no action).
        GOTO cells: bare integer n (not in math mode) — goto state n (e.g. `1`, `4`); empty cell — no goto for this nonterminal.
        Final row: may include `\\ [1ex]` for extra vertical spacing (as in book's SLR(1) example).
        Rule numbering: rules are numbered 0, 1, 2, ... corresponding to the order in the grammar (as in the book). The augmented rule is included in the numbering.
        Must support three LR table variants: LR(0) — built from LR(0) automaton, may contain conflicts (multiple entries per cell, shown comma-separated as in the book); SLR(1) — same automaton as LR(0), but reduce entries restricted by FOLLOW sets, column layout identical to LR(0); CLR(1) — built from CLR(1) automaton, visually identical layout.
        Tests: generate TeX for SLR(1) table of grammar S -> aSbS | eps and verify it compiles with TeX (use existing TeX compilation test infrastructure, name category TexCompilation). Generate TeX for LR(0) table of the same grammar and verify it shows shift-reduce conflicts (comma-separated entries in cells). Verify generated TeX contains expected structural elements: correct number of \hline, correct column count, state numbers, shift/reduce/accept entries, goto entries. For a grammar with multiple nonterminals (e.g. grammar2 from task 11), verify the GOTO section has correct number of columns and entries. Test that all three table variants (LR(0), SLR(1), CLR(1)) produce compilable TeX for the same grammar.
     [done] 52. Implement modified Valiant algorithm. The book (Chapter 7, `02_Valiant.tex`, subsection "Модифицированный алгоритм") describes a modification that structures the parsing table into V-shaped layers of disjoint submatrices of equal size, enabling parallel execution of matrix multiplications.
        The algorithm operates as follows. The main procedure $main()$ first initializes the diagonal cells of $T$ ($T[\ell-1, \ell]$ for all $\ell \in [1,n]$) with trivial productions. Then, for each layer $i = 1, 2, \dots$ up to $\lceil \log n \rceil$, it constructs a layer via $constructLayer(i)$ and calls $completeVLayer$ on it.
        Procedure $constructLayer(i)$ builds a set of disjoint submatrices of size $2^i$: first it constructs submatrix $\mathcal{A} = submatrixByBottomCellAndSize((2^i-1, 2^i), 2^i)$, then returns all submatrices obtained by shifting $\mathcal{A}$ by $(k\cdot2^i, k\cdot2^i)$ for $k \geq 0$, keeping only those that fit within the $T$ matrix bounds.
        Procedure $completeLayer(M)$ processes a set $M$ of submatrices of equal size. If all $m \in M$ have $size(m) = 1$, it fills $T[i,j]$ for all bottom cells $(i,j)$ of submatrices in $M$ where $i+1 \neq j$ (the case $i+1 = j$ is handled in $main()$): $T[i,j] = \{A \mid \exists (B,C) \in P[i,j]: A \to BC \in R\}$. Otherwise, it recursively calls $completeLayer$ on the bottom submatrices, then calls $completeVLayer(M)$.
        Procedure $completeVLayer(M)$ is the core. For each $m \in M$, it process the three upper quarters: $leftSubmatrix(m)$, $rightSubmatrix(m)$, and $topSubmatrix(m)$. It builds three multiplication tasks:
        - $firstMultiplicationTask$: for each $m$ in $leftSubLayer$, multiply $leftGrounded(m)$ and $rightNeighbor(m)$; for each $m$ in $rightSubLayer$, multiply $leftNeighbor(m)$ and $rightGrounded(m)$
        - $secondMultiplicationTask$: for each $m$ in $topSubLayer$, multiply $leftGrounded(m)$ and $rightNeighbor(m)$
        - $thirdMultiplicationTask$: for each $m$ in $topSubLayer$, multiply $leftNeighbor(m)$ and $rightGrounded(m)$
        Then $performMultiplications$ is called on the first task, followed by recursively calling $completeLayer$ on the union of $leftSubLayer$ and $rightSubLayer$, then $performMultiplications$ for the second and third tasks, and finally $completeLayer$ on $topSubLayer$.
        The key difference from the standard Valiant: $performMultiplications$ in the modified algorithm receives sets of multiple triples (not just one), enabling parallel execution of independent matrix multiplications. Procedure $performMultiplications(tasks)$ iterates over each triple $(m, m_1, m_2)$ and each pair $(B,C)$ of nonterminals, computing $P_{BC}[m] \mathrel{+}= T_B[m_1] \times T_C[m_2]$ (Boolean matrix multiplication).
        Preconditions for $completeVLayer$: all submatrices in $M$ are disjoint and of equal size; for each $m \in M$, cells in $bottomSubmatrix(m)$ and cells $(i,j)$ where both indices are within $m$ but $(i,j)$ is in a strictly smaller submatrix of $m$ must already be computed. The $P$ matrix must satisfy: for each $m \in M$, $P[i,j] = \{(B,C) \mid \exists k: a < k < b; a_{i+1}\dots a_k \in L(B) \land a_{k+1}\dots a_j \in L(C)\}$ where $(a,b)$ are the coordinates of submatrix $m$.
        Use the same boolean decomposition representation as in the standard Valiant implementation (task 8, task 42). The modified algorithm must integrate with the existing Valiant infrastructure (boolean matrices, submatrix operations, grounded submatrix helpers).
        Implement step-by-step visualization of the modified Valiant algorithm. Since layers consist of disjoint non-overlapping submatrices, visualize each layer independently: for each layer, show the $T$ matrix with all submatrices of that layer highlighted. Use different colors for different submatrices within a layer. After each multiplication step, highlight the cells that were modified. Use the same visual conventions as the standard Valiant visualization (bool decomposition + recomposed, \cdot for false, 1 for true, border and fill submatrices).
        Tests: use property-based tests — for any grammar (from existing test grammars) and any input string, the standard Valiant and modified Valiant must return identical results: same acceptance status and same final table (matrix over sets). For step-by-step execution with visualization, verify that each layer covers disjoint submatrices and that the union of all layers covers the upper triangle of $T$. Verify that TeX output for modified Valiant steps compiles successfully (use existing TeX compilation tests).
     [done] 53. Implement type for storing RSM (Recursive State Machine). The book (Chapter 6, `03_RecursiveAutomata.tex`) defines an RSM as a tuple $\mathcal{R} = \langle \mathcal{N},\Sigma,B,B_S,Q,Q_S\rangle$ where:
        - $\mathcal{N}$ — set of nonterminal symbols
        - $\Sigma$ — set of terminal symbols
        - $Q$ — set of all automaton states
        - $Q_S$ — set of start states of all blocks
        - $B = \{B_{N_i} \mid N_i \in \mathcal{N}\}$ — set of blocks, where each $B_{N_i} = \langle Q_{N_i}, q_S, Q_F^{N_i}, \delta \rangle$ is a deterministic finite automaton (block) for nonterminal $N_i$. $Q_{N_i} \subseteq Q$, $q_S \in Q_{N_i} \cap Q_S$, $Q_F^{N_i} \subseteq Q_{N_i}$, $\delta \subseteq Q_{N_i} \times (\Sigma \cup Q_S) \times Q_{N_i}$ — transition function over alphabet of terminals and start states of other blocks
        - $B_S \in B$ — start block
        RSM is a collection of deterministic finite automata, one per nonterminal. Transitions are labeled by either terminals (read input) or start states of other blocks (recursive call). Represent using the existing deterministic finite automaton type (task 14, task 31). Each block is a DFA over the alphabet $\Sigma \cup Q_S$. Map each block's start state to its corresponding nonterminal. The RSM type must be generic over terminal and nonterminal types. Provide accessors: list of all blocks, get block by nonterminal, get start block, list all terminals, list all nonterminals.
     [done] 54. Implement reading of EBNF grammar and construction of RSM from it. The book (Chapter 6, `02_EBNF.tex`) defines EBNF grammar as $\langle \Sigma, N, P, S \rangle$ where each rule $N_i \to R$ has a regular expression $R$ over $\Sigma \cup N$ as the right-hand side.
        EBNF input format: file with extension `.ebnf`. One rule per line. Empty lines allowed. Each rule has form `<nonterm> -> <regex>`. Nonterminal naming follows the same convention as BNF (PascalCase: starts with capital letter). Terminal naming: camelCase (starts with lowercase). The `eps` symbol is used explicitly to denote epsilon.
        Extended regular expression syntax in right-hand sides supports:
        - Concatenation (juxtaposition): `a b c`
        - Alternative: `|`
        - Kleene star: `*` (zero or more)
        - Plus: `+` (one or more, shorthand for $R \cdot R^*$)
        - Optional: `?` (zero or one, shorthand for $R \mid \varepsilon$)
        - Parentheses for grouping: `( ... )`
        - `eps` as an explicit epsilon symbol (matches empty string)
        Examples of valid EBNF rules: `S -> a S b S | eps` (same as BNF), `S -> ( a S b )*`, `A -> a+ b? c*`, `Expr -> Term ( (+ | -) Term )*`.
        If multiple rules have the same left-hand side nonterminal (e.g., `S -> a S b S` and `S -> eps` on separate lines), join their right-hand sides with alternative: `S -> a S b S | eps` at the AST level before building automata. This means after parsing, the grammar AST groups rules by nonterminal: each nonterminal has exactly one regular expression that is the disjunction (|) of all right-hand sides from rules with this nonterminal.
        Use FParsec (https://www.nuget.org/packages/fparsec/) for parsing. Implement parsing in two stages: (1) parse into an AST representing the EBNF grammar with explicit regular expression nodes (Epsilon, Terminal, Nonterminal, Concatenate, Alternative, Star, Plus, Optional); (2) group rules by nonterminal and build a single combined regex per nonterminal.
        To build DFA for each regular expression, use Brzozowski derivatives. Reuse the implementation from `https://github.com/gsvgit/CFPQ_GLL/blob/Parsing/CFPQ_GLL/RsmBuilder.fs`. The algorithm: starting from the initial state (the regex itself), compute derivatives with respect to each symbol in $\Sigma \cup N$, creating new states for distinct derivatives. Continue until closure (no new derivatives). Each derivative that matches the empty string (nullable) corresponds to a final state.
        Steps to build RSM:
        1. Parse EBNF file into AST (FParsec)
        2. Group rules by nonterminal: for each nonterminal, build a combined regex by joining all right-hand sides with `|`
        3. For each nonterminal $N_i$ with combined regex $R_i$, build a DFA using Brzozowski derivatives over alphabet $\Sigma \cup \mathcal{N}$. Each DFA state is a regular expression (derivative). The start state is $R_i$. Final states are those derivatives that are nullable. Transitions: from derivative $d$ on symbol $x$, go to derivative $d_x$ (the derivative of $d$ w.r.t. $x$)
        4. After building all DFA blocks, identify block start states: each block's start state $q_S$ is its DFA start state (index 0). The set $Q_S$ is the collection of all block start states
        5. Relabel transitions: replace terminal/nonterminal symbols with proper types. Transitions labeled with nonterminals become transitions on the start state of the corresponding block (using the block's $q_S$). This is because RSM transitions are over $\Sigma \cup Q_S$
        6. Assemble the RSM: collect all blocks, set the start block to the block for the grammar's start nonterminal
        Tests:
        - Grammar `S -> eps`: RSM has one block (S) with one state (start state), no transitions, the start state is also final
        - Grammar `S -> a*`: RSM has one block (S) with one state, one transition on `a` looping back, start state is final
        - Grammar `S -> a b`: RSM has one block (S) with 3 states, 2 edges, last state final
        - Grammar `S -> a* a*`: RSM has one block (S) with one state, one transition on `a` looping back, start state is final
        - Grammar `S -> (a S b)*`: EBNF for Dyck language — 3 states, 3 transitions, the start state is also final
        - Grammar `S -> a+ b?`: tests + and ? operators
        - Grammar with multiple rules for same nonterminal: `S -> a S b S` and `S -> eps` — verify rules are merged correctly
        - Grammar2 from task 11 (expression grammar): verify RSM blocks correspond to E, T, F
        - Verify that the constructed DFAs are deterministic and have the expected number of states (for simple grammars)
        - Generate dot visualizations of RSM blocks and verify they compile with graphviz (use existing infrastructure)
     [done] 55. Implement conversion of DFA to CFG and RSM to BNF grammar.
        The book (Chapter 5, `06_LinearGrammars.tex`) provides the conversion: given a DFA $M = \langle \Sigma, Q, q_s, Q_f, \delta \rangle$, build a right-linear grammar $G = \langle \Sigma, N, S, P \rangle$ where $N = Q$, $S = q_s$, $P = \{q_i \to t\,q_j \mid (q_i, t, q_j) \in \delta\} \cup \{q_i \to \varepsilon \mid q_i \in Q_f\}$.
        The book (Chapter 6, `02_EBNF.tex`, Theorem `\ref{thm:ebnf_cfg}`) describes converting an EBNF grammar back to BNF: for each rule $N \to R$, build a DFA for $R$, convert the DFA to a right-linear grammar $G_R$, then replace rule $N \to R$ with the rules of $G_R$, identifying nonterminal $N$ with the nonterminal corresponding to the DFA's start state.
        Based on this, implement the function `rsmToGrammar` that converts an RSM to a BNF grammar:
        1. For each block $B_{N_i}$ of the RSM, convert the DFA block to a right-linear grammar fragment:
           - For each transition $(q, x, q') \in \delta$ where $x \in \Sigma$ (terminal), add rule $Q_q \to x\,Q_{q'}$
           - For each transition $(q, s', q') \in \delta$ where $s' \in Q_S$ (call to another block), find the nonterminal $N_j$ whose start state is $s'$, and add rule $Q_q \to N_j\,Q_{q'}$
           - For each final state $q_f \in Q_F^{N_i}$, add rule $Q_{q_f} \to \varepsilon$
        2. For each block $B_{N_i}$:
           - Identify nonterminal $N_i$ with the grammar nonterminal $Q_{q_S}$ (where $q_S$ is the block's start state)
           - Therefore: $N_i \to RHS$ for each rule with left-hand side $Q_{q_S}$, and all rules produced for other states of the block are auxiliary nonterminals
        3. The resulting BNF grammar uses the book's Nonterminal and Terminal types. The start nonterminal of the BNF grammar is the nonterminal from the RSM's start block
        After implementing the conversion, write property-based tests: for any grammar that can be expressed both as BNF and EBNF, and any input string:
        - (grammar loaded as BNF → parsing algorithm) and (grammar loaded as EBNF → built RSM → converted to BNF → same parsing algorithm) must return identical accept/reject results
        - Test with all applicable parsing algorithms: CYK, Valiant, modified Valiant, LL (for LL-compatible grammars), LR (for LR-compatible grammars)
        - Use the existing test grammars infrastructure: grammars that are in BNF already (grammar1, grammar2, grammar3 from tasks 6 and 11) can be mechanically converted to EBNF (e.g., `S -> a S b S | eps` in EBNF becomes `S -> a S b S | eps` — same syntax since EBNF accepts `|`), and the round-trip through RSM and back to BNF should produce an equivalent grammar
        - Add specific test: the Dyck language grammar `S -> ( a S b )*` in EBNF, converted to RSM then to BNF, should recognize the same strings as the standard BNF grammar `S -> a S b S | eps` (from task 6)
56. [done] Refactoring.
      1. In all RsmBuilderTests tests in EbnfParserTests.fs check that all produced boxes are really deterministic automaton. Looks like there is isDeterministic function to do it.
      2. Add more ebnf parsing tests
         1. `S -> a (a | b)` must produse exactly the same result as  `S -> a (a|b)` and `S -> a(a |b)`
         2. `S -> a S | (eps)` must produce exactly the same result as `S -> a S | ((eps))` and `S -> a S |(eps)` and `S -> a S |eps`
         3. `S -> a (a ( a | b))` must produce exactly the same result as `S -> a(a (a | b))` and `S -> a (a ( a |     b))`
      3. Add more ebnf conversion property-based tests (using parsing)
         1. Parisn with `S -> a S | eps` must acceps and rejects te same strings as parsing for `S -> (a*) (a*)`
        4. Fix test ``Build RSM for a* a* grammar``  Waht is exact number of states? Explain in comments, why. I expect, it must be exactly one state and transition 0 -[a]-> 0, 0 is start and final. Are you sure you implement operatyions priority parsing correctly? This must parse like `(a*) (a*)` not `(a* a)*`
57. [done] Implement linear-algebra based multiple-source BFS as described in the book (Chapter 3, `05_BFS.tex`, algorithm `\ref{algo:MS-BFS_linal}`). This is a building block used by RPQ algorithms, implement it before them.
        MS-BFS (multiple-source BFS) performs independent BFS traversals from $k$ starting vertices simultaneously. The front is a $k \times |V|$ boolean matrix where row $i$ is the BFS front for source vertex $K[i]$. The algorithm uses two algebraic structures:
        - $\BbbB = \langle \{0,1\}, \vee, \wedge \rangle$ — standard Boolean semiring (element-wise OR as addition, AND as multiplication)
        - $\BbbM = \langle \{0,1\}, \oplus \rangle$ — mask structure with $0\oplus 0=0$, $1\oplus1=0$, $0\oplus1=0$, $1\oplus0=1$ (inverted mask: result keeps values from the first operand only where the second operand is 0)
        Pseudocode:
        ```
        current_front ← 0^{k × n}
        visited ← 0^{k × n}
        For i ∈ [0..|K|-1]: current_front[i, K[i]] ← 1
        While current_front ≠ 0:
          visited ← visited ⊕_B current_front         // accumulate visited vertices
          new_front ← current_front ⊗_B M             // propagate one step: matrix product in Boolean semiring
          current_front ← new_front ⊕_M visited       // filter: keep only vertices NOT yet visited
        return visited
        ```
        Where $M$ is the graph's boolean adjacency matrix ($n \times n$), $\oplus_B$ is `map2 (||)`, $\otimes_B$ is `mxm (&&) (||) false`, $\oplus_M$ is `map2` with the inverted mask operation.
        Each row of the result is a boolean vector indicating which vertices are reachable from the corresponding source.
        Tests:
        - Simple path graph $v_0 \to v_1 \to v_2$, sources $[v_0, v_1]$: row 0 = $\{v_0, v_1, v_2\}$, row 1 = $\{v_1, v_2\}$
        - Disconnected graph with two components, one source in each: each row covers exactly its own component
        - Complete graph of $n$ vertices, single source $v_0$: row 0 is all-ones
        - No-source edge case: $K$ is empty, front stays zero, result is zero matrix
        - Property-based: for any graph, the MS-BFS result for source set $K$ must equal running |K| independent single-source BFS traversals and stacking the results row-wise
58. [done] Add matrix operations needed for MS-BFS and RPQ algorithms (Belyanin, Arroyuelo, Kronecker-based). All operations are expressed through existing generic matrix operations (map2, mxm, map, kron from tasks 2 and 3). Do not introduce ad-hoc loops.
        The following algebraic structures and operations are needed, as defined in the book:
        1. Boolean semiring $\BbbB = \langle \{0,1\}, \vee, \wedge \rangle$ (element-wise OR as addition `\opAddFrom{\BbbB}`, AND as multiplication `\mmultFrom{\BbbB}`). Express `\opAddFrom{\BbbB}` via `map2 (||)` and `\mmultFrom{\BbbB}` via `mxm (&&) (||) false`.
        2. Mask semiring $\BbbM = \langle \{0,1\}, \oplus \rangle$ where $0\oplus 0=0$, $1\oplus1=0$, $0\oplus1=0$, $1\oplus0=1$ — inverted mask. Express `\opAddFrom{\BbbM}` via `map2` with the custom mask operation. This is used in BFS (Chapter 3, `05_BFS.tex`) to filter the new front: `current_front ← new_front \opAddFrom{\BbbM} visited` — only vertices not yet visited remain in the front.
        3. Boolean matrix decomposition (BoolDecomposition, Chapter 3, `01_BasicDefinitions.tex`, definition `\ref{def:BoolDecomposition}`) — already implemented in task 7. For RPQ, given a labeled graph $G$ and an automaton $N$, build per-label boolean matrices $G^a$ and $N^a$ where $G^a[i,j]=1$ iff there is an edge $v_i \xrightarrow{a} v_j$, and $N^a[q,q']=1$ iff the automaton has a transition $q \xrightarrow{a} q'$.
        4. Index-based unary operator $I$ (applied to each element of a matrix with knowledge of its indices, Chapter 11, `02_BFS.tex`). Expressed via `map` that receives the element and its row/column indices. Three instances:
           - $I^P_{reach}(x,q,v)$: returns $x$ if $P_{qv}=0$ (not yet visited), else $0$. Boolean mask preventing re-processing of already found (state, vertex) pairs. Requires accumulated matrix $P$ as context.
           - $I_{simple}(X,q,v)$ for path enumeration: keeps only path continuations where the new vertex $v$ has not appeared in the path $X$ yet.
           - $I_{trail}(X,q,v)$: keeps continuations where the last edge is not repeated.
        5. Kronecker product $\kron$ (Chapter 1, `07_MatricesAndVectors.tex`) — already implemented as `kron` in task 3. Needed for the Kronecker-based RPQ algorithm: $K_a = N^a \kron G^a$.
        6. MS-BFS (Chapter 3, `05_BFS.tex`, algorithm `\ref{algo:MS-BFS_linal}`). Front is a $k \times |V|$ matrix where row $i$ is the BFS front for source $K[i]$. Operation: `new_front ← current_front \mmultFrom{\BbbB} M` (matrix-matrix product, each row independently propagates). Filter: `current_front ← new_front \opAddFrom{\BbbM} visited`. Initialization: for each source $i$, set `current_front[i, K[i]] ← 1`. MS-BFS is used in the Kronecker-based RPQ to filter reachable (state, vertex) pairs.
        Tests: property-based tests for each operation. `\mmultFrom{\BbbB}` must equal the standard Boolean matrix product. `\opAddFrom{\BbbM}` must correctly implement inverted mask (verify: `[1,0] +_M [0,1] = [1,0]` and `[1,1] +_M [0,1] = [0,1]`). MS-BFS on a simple path graph must produce the correct reachability matrix for multiple sources. BoolDecomposition round-trip: recomposing from per-label matrices produces the original adjacency matrix.
59. [done] Implement Belyanin's LARPQ algorithm (BFS-based single-source RPQ, Chapter 11, `02_BFS.tex`, algorithm `\ref{algo:RPQ_BFS_semiring}`). The algorithm operates on two $|Q| \times |V|$ matrices: front $M$ and accumulated results $P$.
        Pseudocode:
        ```
        M ← 0_{|Q|×|V|}
        P ← 0_{|Q|×|V|}
        ForEach q ∈ Q_S: M_{q,v_s} ← 1
        While M ≠ 0:
          M ← I(M)           // mask: drop already found (q,v) pairs
          P ← P ⊕_B M        // accumulate in boolean semiring
          M ← ⊕_{a∈L^{↔}} (N^a)^T ⊗_B M ⊗_B G^a   // propagate: automaton + graph simultaneously
        F ← 0_{1×|Q|} with F_{1,q}=1 for q∈Q_F
        return F ⊗_B P       // only final automaton states
        ```
        Where:
        - $N^a$ are automaton transition matrices per label (rows = Q, cols = Q)
        - $G^a$ are graph edge matrices per label (rows = V, cols = V)
        - $(N^a)^T \otimes_B M \otimes_B G^a$ is the matrix product in boolean semiring: first multiply $(N^a)^T$ (reverse transitions) with $M$, then with $G^a$. This simultaneously extends paths by one automaton transition and one graph edge for label $a$.
        - $I = I^P_{reach}$ is the index-based unary operator that filters out $(q,v)$ pairs already in $P$
        - Result $F \otimes_B P$ is a $1 \times |V|$ vector: vertex $v$ is reachable iff any final state is associated with it in $P$
        Input: Input: DFA (query), NFA(labeled graph)
        Output: boolean vector of reachable vertices from $v_s$ respecting the regular constraint.
        Add step-by-step visualization: for each iteration, show the front matrix $M$ and accumulated results $P$. Use the same matrix TeX visualization conventions (nicematrix, highlight modified cells). Include visualization of the automaton (as graph) and the original graph.
        Tests:
        - Simple graph with one edge `v0 -[a]-> v1`, query `a`: vertex v1 must be reachable from v0
        - Graph `v0 -[a]-> v1 -[b]-> v2`, query `a* b`: v2 reachable
        - Graph `v0 -[a]-> v1 -[a]-> v2`, query `a+`: v1 and v2 reachable
        - Graph with cycle: `v0 -[a]-> v1 -[a]-> v0`, query `a*`: both v0 and v1 reachable
        - Query `a | b` on graph with both `a` and `b` edges from v0: verify both paths considered
        - Query with reverse label `a^-`: verify traversing edge backward
        - Use test grammars infrastructure for reusable test cases
60. [done] Implement Arroyuelo's RPQ algorithm (Chapter 11, `03_Arroyuelo.tex`). Translate a 2-way regular expression $E$ (over $L^{\leftrightarrow}$ with forward labels $a$ and backward labels $a^{-}$) into a Boolean matrix expression and evaluate it.
        The translation function $\mathcal{M}$ maps each sub-expression to a Boolean matrix:
        - $\mathcal{M}(\varepsilon) = I$ (identity matrix)
        - $\mathcal{M}(a) = M_a$ (graph adjacency matrix for label $a$)
        - $\mathcal{M}(a^{-}) = M_a^T$ (transpose for reverse traversal)
        - $\mathcal{M}(E_1 \mid E_2) = \mathcal{M}(E_1) \lor \mathcal{M}(E_2)$ (element-wise OR)
        - $\mathcal{M}(E_1 / E_2) = \mathcal{M}(E_1) \times \mathcal{M}(E_2)$ (Boolean matrix product)
        - $\mathcal{M}(E^+) = \mathcal{M}(E)^+$ (transitive closure)
        - $\mathcal{M}(E^*) = I \lor \mathcal{M}(E)^+$ (identity + transitive closure)
        The algorithm evaluates the expression tree in post-order (bottom-up). For joining operations with multiple operands ($E_1 \mid \dots \mid E_m$), heuristically choose pairs with smallest intermediate matrix sizes first. For concatenation chains ($E_1 / \dots / E_m$), push row constraints to the left operand and column constraints to the right operand.
        Uses dense Boolean matrices (not compressed k^2-trees). The key contribution is the translation from regular expression to matrix operations.
        The expression input is the regex AST from the EBNF parser (task 54) — reuse the same regular expression AST type (Epsilon, Terminal, Concatenate, Alternative, Star, Plus, Optional). Since the EBNF parser already builds a DFA via derivatives, also provide an alternative entry point that accepts a regex AST directly (not requiring a full grammar).
        Input: DFA (query), NFA(labeled graph)
        Handling of start vertices: Arroyuelo computes the full $|V| \times |V|$ matrix $\mathcal{M}(E)$ for the entire regex, then restricts it to source rows. If sources $S = \{s_1, s_2, \dots\}$ are specified, extract rows $S[i]$ into a $|S| \times |V|$ result matrix: $result[i,j] = \mathcal{M}(E)[S[i],j]$. This is equivalent to the book's row restriction operator $\langle S \rangle \mathcal{M}(E)$. If sources are not specified (all vertices are sources), return the full $\mathcal{M}(E)$ matrix unchanged. If both source and target vertices are specified, further restrict columns: $\langle S \rangle \mathcal{M}(E) \langle T \rangle$.
        Output: boolean reachability matrix $|startVertices| \times |V|$ (or $|V| \times |V|$ if all vertices are sources)
        Tests:
        - Same test cases as for Belyanin's algorithm (task 58) — both must produce identical results
        - Test with backward labels: graph `v0 -[a]-> v1`, query `a^-` starting from v1 must reach v0
        - Query with star: `a*` on graph `v0 -[a]-> v1 -[a]-> v2`, all pairs within the reachable set
        - Query with alternation + concatenation: `a (b|c)` on graph with both branches
        - Identity: query `\varepsilon` returns identity matrix (every vertex reaches itself)
61. [done] Implement RPQ algorithm based on Kronecker product of adjacency matrices with MS-BFS filtering. This algorithm is not described explicitly in the book but follows from the tensor product approach in Chapter 12 (`03_TensorProduct.tex`) adapted to RPQ:
        Given a regular expression query parsed into a DFA (via EBNF parser from task 54, reuse the automaton), build a single large intersection matrix via the Kronecker product and then filter reachable (state, vertex) pairs using MS-BFS.
        Algorithm steps:
        1. Compute kronecker product of transition matrices where elementwise operation is a set intersection.
        3. From the start pairs $S = \{(q_s, v_s) \mid q_s \in Q_S, v_s \in startVertices\}$, run MS-BFS on $K$: the front is a $|S| \times (|Q|\cdot|V|)$ matrix, each row corresponding to one start pair. Initialize front: for row $i$ with start pair $(q_s, v_s)$, set front at column corresponding to $(q_s, v_s)$ to 1. The same for reversed automata (start is final, final is start, edges go in reversed direction).
        4. After convergence, inetrsect visited matrix to determine vertices reachable from start states and reaches final states. Preserve only edges between these vertices. To do it use multiplication on diagonal matrix as described in the book.
        Input: DFA (query), NFA(labeled graph)
        Output: boolean reachability matrix $|startVertices| \times |V|$.
        Tests:
        - Same test cases as for Belyanin (task 58) and Arroyuelo (task 59) — all three must produce identical results
        - Multiple sources: graph with vertices v0, v1, v2 and edges v0-[a]->v2, v1-[b]->v2. Query `a` from sources [v0, v1]: only v2 reachable from v0
        - Single-state automaton (epsilon query): the Kronecker product is just the graph adjacency — MS-BFS reduces to standard multi-source BFS
62. [done] Implement graph reading function. Input format for graph files:
        - First line (optional): space-separated list of start vertex indices (0-based). If absent, all vertices are considered start vertices
        - Following lines: triples `fromVertex label toVertex`
        - Parse the file into graph adjacency representation, then build per-label boolean matrices. 
63. [done] Property-based tests: all three RPQ algorithms (Belyanin, Arroyuelo, Kronecker+MS-BFS) must return identical results on any input.
        Generate random test data:
        1. Random regular expressions over a small alphabet (2–4 symbols). Build random regex ASTs using the EBNF regex type (Epsilon, Terminal from alphabet, Concatenate, Alternative, Star). Avoid excessive nesting (limit depth to 3–4) to keep expressions manageable.
        2. Convert each regex to a DFA via the derivatives-based construction from the EBNF parser (task 54) to get the automaton for Belyanin and Kronecker algorithms.
        3. Random labeled graphs: generate as sparse boolean matrices of size $n \times n$ ($n \approx 10$–$20$). Each cell $(i,j)$ independently has a label from the alphabet with some probability (0.15–0.3). No epsilon transitions in the graph. Use `Matrix<Option<char>>` or similar. 
        4. Source vertices: select randomly, roughly half of all vertices as sources.
        For property-based tests:
        - For each generated (graph, regex, sources) triple, run all three algorithms
        - Belyanin: run once per source vertex, collect results into a $|sources| \times |V|$ matrix
        - Arroyuelo: compute full $|V| \times |V|$ matrix, then restrict to source rows
        - Kronecker: run MS-BFS with all source pairs
        - Assert all three result matrices are identical
        - Also test: for single-source case, Belyanin result equals the corresponding row of Arroyuelo result
        - Test with all semantics (reachability, simple paths, trails if implemented)
64. [done] Refactoring. 
    1. Move RPQ algorithms to separsted project (tests too).
    2. Move MS-BFS to GraphAnalysis project. Create respective tests project.
    3. Unify RPQ algorithms interafece. All of them must accept graph as NFA. Graph builder must returns NFA. Start vertices are start states of NFA. 
    4. Implement fold for Matrix<'t> Use it instead anyTrue in ms-bfs.
[done] 65. Refactoring.
    1. Paring algorithms (LL, LR, versions of Valiant, CYK) have inconsistent type for input sequence. Input for parsing algorithm is a list of Terminals.
    2. Lines 160–194 and 224–257 in `LRParser.fs` are structurally identical except for `closureLR0` vs `closureLR1` and the final-state check. ~60 lines of duplicated state-exploration logic. Remove code duplications.
    3. `MsBfsTests.fs` has a `[<Property>]` test but implements random generation manually via `System.Random.Shared` in a `for` loop rather than using FsCheck generators and the FsCheck framework to drive iterations. The test manually sets `succeeded <- false` on mismatch. Create common random graphs generator that can be used for MS-BFS and for RPQ algorithms. Generate graph as Matrix. May be you can use existin matrix generators. Reuse them.
    4. `ValiantTests.fs` covers grammars 1–5 but not 6–8 (the arithmetic expression grammars). Add necessary tests for both variants of Valiant.
    5. Lines 38–52 in `src/FLPQ.Cli/Program.fs` define `writeDotFile` and `writeTexFile` with byte-identical bodies. Should be a single `writeOutputFile` function.
    6. `completeTrace` (lines 412–494) and `computeTrace` (lines 483–498) copy ~90% of `complete` (lines 124–195) and `compute` (lines 175–195). Tracing logic (BooleanDecomposition recompose per submatrix) is entangled with computation. We can have only instance with traceing. The algorithm collects trace data as F# data structures. At he final it may be converted to TeX or other formats if necessary.
    7. Valiant init block duplicated 4x. The ~40-line setup block (building `tByNt` dictionary, `pByPair` dictionary, terminal rule initialization via `BooleanDecomposition.decompose`, and the epsilon-acceptance early-exit) is copy-pasted identically 4 times in `Valiant.fs Remove code duplication.