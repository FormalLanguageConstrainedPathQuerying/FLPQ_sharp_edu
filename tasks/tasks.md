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
   45. Refactor LRParserTests.fs to avoid code duplication across submodules. Create parametrizable tests.
   46. Check that documentation is up to date.
     47. Some previous tasks fix problems described in code_review.md. Update code review: remove problems solved.

(End of file - total 156 lines)