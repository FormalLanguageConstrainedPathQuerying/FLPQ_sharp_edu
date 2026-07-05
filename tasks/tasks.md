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
