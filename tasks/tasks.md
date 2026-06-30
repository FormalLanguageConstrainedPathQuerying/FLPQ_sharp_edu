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

