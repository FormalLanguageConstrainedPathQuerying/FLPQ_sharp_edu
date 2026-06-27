* This file contains user-defined tasks. Do not modify them. Only track status of tasks in this file.
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
6. Implement CYK algorithm. Use `Matrix<Option<HashSet<Symbol<'t, 'nt>>>>` to represent working table. Initial matrix and matrix on each step may be printed. Empty cell (`None`) printed as `\cdot`. Example of tests:
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