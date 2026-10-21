module CfpqTestHelpers

open FsCheck
open FSharpPlus.Data
open FLPQ.CFPQ
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

/// Book Chapter 12 worked example graph (matrix-consistent reading): a-edges 0->1, 1->2, 2->0;
/// b-edges 0->3, 3->0. The TikZ figure's 2<->3 b-edges are an inconsistency recorded in S8.
let bookGraph: CfpqInput.CfpqGraph<string> =
    { VertexCount = 4
      Labels =
        Map.ofList
            [ "a", Matrix.create 4 4 (fun i j -> (i, j) = (0, 1) || (i, j) = (1, 2) || (i, j) = (2, 0))
              "b", Matrix.create 4 4 (fun i j -> (i, j) = (0, 3) || (i, j) = (3, 0)) ] }

/// Book Chapter 12 worked example grammar: S -> AB | AS1, A -> a, B -> b, S1 -> SB.
let bookGrammar: Grammar<string, string> =
    { Rules =
        [ { Lhs = Nonterminal "S"
            Rhs = Rhs.Symbols(NonEmptyList.ofList [ Symbol.N(Nonterminal "A"); Symbol.N(Nonterminal "B") ]) }
          { Lhs = Nonterminal "S"
            Rhs = Rhs.Symbols(NonEmptyList.ofList [ Symbol.N(Nonterminal "A"); Symbol.N(Nonterminal "S1") ]) }
          { Lhs = Nonterminal "A"
            Rhs = Rhs.Symbols(NonEmptyList.ofList [ Symbol.T(Terminal "a") ]) }
          { Lhs = Nonterminal "B"
            Rhs = Rhs.Symbols(NonEmptyList.ofList [ Symbol.T(Terminal "b") ]) }
          { Lhs = Nonterminal "S1"
            Rhs = Rhs.Symbols(NonEmptyList.ofList [ Symbol.N(Nonterminal "S"); Symbol.N(Nonterminal "B") ]) } ]
      Start = Nonterminal "S" }

/// The TensorProduct.tex example graph: a-edges 0->1, 1->2, 2->0; b-edges 2->3, 3->2.
let tensorBookGraph: CfpqInput.CfpqGraph<string> =
    { VertexCount = 4
      Labels =
        Map.ofList
            [ "a", Matrix.create 4 4 (fun i j -> (i, j) = (0, 1) || (i, j) = (1, 2) || (i, j) = (2, 0))
              "b", Matrix.create 4 4 (fun i j -> (i, j) = (2, 3) || (i, j) = (3, 2)) ] }

/// The book's exact 4-state recursive automaton for S -> aSb | ab (TensorProduct.tex M1):
/// states 0..3 all in block S; transitions 0-a->1, 1-S->2, 1-b->3, 2-b->3; start state 0,
/// final state 3. Hand-built so the iteration trace matches the book's figures exactly (a
/// derivative-built RSM would have more states and a different iteration count).
let tensorBookRsm: RSM<string, string> =
    let s = Nonterminal "S"

    RsmFixtures.mkRsm
        [| for i in 0..3 ->
               { BlockNonterminal = s
                 LocalState = i
                 IsFinal = (i = 3) } |]
        [ { From = 0
            Label = AutomatonLabel.ATerm(RsmSymbol.RTerm(Terminal "a"))
            To = 1 }
          { From = 1
            Label = AutomatonLabel.ATerm(RsmSymbol.RNonterm s)
            To = 2 }
          { From = 1
            Label = AutomatonLabel.ATerm(RsmSymbol.RTerm(Terminal "b"))
            To = 3 }
          { From = 2
            Label = AutomatonLabel.ATerm(RsmSymbol.RTerm(Terminal "b"))
            To = 3 } ]
        [ (s, 0) ]
        [ 3 ]
        s

/// The WCNF rule set that AzimovCfpq.evaluate derives internally (toCnf with the deterministic
/// fresh names N_%d), for use by extraction and enumeration against evaluate's matrices.
let wcnfRules (g: Grammar<string, string>) : AzimovCfpq.WcnfRule<string, string> list =
    AzimovCfpq.classifyRules (Grammar.toCnf Grammar.freshStringNonterminal g)

/// The set of cells (i, j) whose value is true.
let trueCells (m: Matrix<bool>) : Set<int * int> =
    [ for i in 0 .. Matrix.rows m - 1 do
          for j in 0 .. Matrix.cols m - 1 do
              if m.[i, j] then
                  (i, j) ]
    |> Set.ofList

/// The number of cells whose value is true.
let trueCellCount (m: Matrix<bool>) : int =
    [ for i in 0 .. Matrix.rows m - 1 do
          for j in 0 .. Matrix.cols m - 1 do
              if m.[i, j] then 1 else 0 ]
    |> List.sum

/// All-paths enumeration oracle: brute-force DFS over simple paths from i to j.
let bruteForceSimplePaths (graph: CfpqInput.CfpqGraph<string>) (i: int) (j: int) : Trans<string> list list =
    let rec dfs (v: int) (visited: Set<int>) (acc: Trans<string> list) : Trans<string> list list =
        if v = j then
            [ List.rev acc ]
        else
            graph.Labels
            |> Map.toList
            |> List.collect (fun (label, m) ->
                seq {
                    for w in 0 .. graph.VertexCount - 1 do
                        if m.[v, w] && not (Set.contains w visited) then
                            yield! dfs w (Set.add w visited) ({ From = v; Label = label; To = w } :: acc)
                }
                |> Seq.toList)

    dfs i (Set.singleton i) []

/// Build a CfpqGraph from an NFA: one Boolean adjacency matrix per terminal label.
let nfaToCfpqGraph (nfa: NFA<string, int>) : CfpqInput.CfpqGraph<string> =
    let v = List.length nfa.States

    let cells =
        [ for i in 0 .. v - 1 do
              for j in 0 .. v - 1 do
                  match nfa.Transitions.[i, j] with
                  | Some nes ->
                      for label in NonEmptySet.toSeq nes do
                          match label with
                          | ATerm t -> (t, (i, j))
                          | AEpsilon -> ()
                  | None -> () ]

    { VertexCount = v
      Labels =
        cells
        |> List.groupBy (fun (t, _) -> t)
        |> List.map (fun (t, ts) ->
            t, Matrix.create v v (fun i j -> List.exists (fun (_, (a, b)) -> (a, b) = (i, j)) ts))
        |> Map.ofList }

/// A registry grammar with toCnf precomputed once (outside the property body). The comparison
/// runs over the CNF grammar's nonterminals: toCnf may drop original nonterminals (useless-symbol
/// elimination, e.g. all-epsilon grammars collapse to a single fresh start), and both algorithms
/// run on the same CNF conversion, so every CNF nonterminal is present on both sides.
type GrammarCase =
    { Name: string
      Grammar: Grammar<string, string>
      Rsm: RSM<string, string>
      Cnf: Grammar<string, string>
      CnfNts: Nonterminal<string> list
      CnfNullable: Set<Nonterminal<string>>
      Alphabet: string list }

/// Terminal alphabet of a grammar (sorted, deduplicated).
let private terminalAlphabet (g: Grammar<string, string>) : string list =
    g.Rules
    |> List.collect (fun r ->
        match r.Rhs with
        | Rhs.EpsilonRhs -> []
        | Rhs.Symbols nel ->
            NonEmptyList.toList nel
            |> List.choose (function
                | Symbol.T(Terminal t) -> Some t
                | _ -> None))
    |> List.distinct
    |> List.sort

/// Nonterminals deriving epsilon in the RSM. A block A derives epsilon iff its start state can
/// reach one of its final states along a path whose every label is a nonterminal that itself
/// derives epsilon: terminal labels never contribute to an epsilon derivation and RsmBuilder
/// output has no AEpsilon transitions, so only RNonterm(b) edges with b nullable may be used.
/// This is a least fixed point over the blocks (a block becomes nullable once its start reaches a
/// final through already-nullable blocks). It is the correct reference for the empty-path
/// (diagonal) cells and for deciding which per-nonterminal cells toCnf may have altered: toCnf
/// allows epsilon only at its own (possibly fresh) start, so for a nullable non-start A
/// L_WCNF(A) = L_RSM(A) \ {epsilon} and only that diagonal can differ from the RSM's matrix.
/// CNF-based nullability (Grammar.nullableNonterminals cnf) does not match the RSM's language
/// for exactly those symbols.
let rsmNullable (rsm: RSM<string, string>) : Set<Nonterminal<string>> =
    let nts = RSM.nonterminals rsm
    let finals = RSM.blockFinalsMap rsm
    let q = rsm.StateCount

    /// Whether the edge s -> t is labelled by a nonterminal in the given nullable set.
    let activeEdge (nullable: Set<Nonterminal<string>>) (s: int) (t: int) : bool =
        match rsm.Transitions.[s, t] with
        | Some nes ->
            NonEmptySet.toSet nes
            |> Set.exists (function
                | AutomatonLabel.ATerm(RsmSymbol.RNonterm b) -> Set.contains b nullable
                | _ -> false)
        | None -> false

    /// Blocks whose start reaches a block final using only edges active for the given set.
    let epsReachable (nullable: Set<Nonterminal<string>>) : Set<Nonterminal<string>> =
        nts
        |> List.filter (fun a ->
            let fs = Map.find a finals

            // grown always contains seen, so an empty difference means the closure has converged.
            let rec closure (frontier: Set<int>) (seen: Set<int>) : Set<int> =
                let next =
                    [ for s in Set.toList frontier do
                          for t in 0 .. q - 1 do
                              if activeEdge nullable s t && not (Set.contains t seen) then
                                  t ]
                    |> Set.ofList

                let grown = Set.union seen next

                if Set.isEmpty (Set.difference grown seen) then
                    seen
                else
                    closure next grown

            let reached =
                closure (Set.singleton rsm.BlockStart.[a]) (Set.singleton rsm.BlockStart.[a])

            fs |> Set.exists (fun f -> Set.contains f reached))
        |> Set.ofList

    // epsReachable is monotone, so grown always contains acc; an empty difference means a fixpoint.
    let rec fixpoint (acc: Set<Nonterminal<string>>) : Set<Nonterminal<string>> =
        let grown = epsReachable acc

        if Set.isEmpty (Set.difference grown acc) then
            acc
        else
            fixpoint grown

    fixpoint Set.empty

/// All registry grammars with toCnf precomputed once per grammar.
let grammarCases: GrammarCase list =
    LanguageRegistry.allGrammars
    |> List.map (fun ag ->
        let g = ag.Grammar
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        { Name = ag.Name
          Grammar = g
          Rsm = ag.Rsm
          Cnf = cnf
          // The same nonterminal set AzimovCfpq.evaluate initializes matrices for.
          CnfNts = Set.toList (Set.union (Grammar.nonterminalsOf cnf) (Set.singleton cnf.Start))
          CnfNullable = Grammar.nullableNonterminals cnf
          Alphabet = terminalAlphabet g })

/// Linear path graph for a word: |s|+1 vertices, edge i -> i+1 labeled s[i].
let linearPathGraph (word: string list) : CfpqInput.CfpqGraph<string> =
    let n = List.length word

    { VertexCount = n + 1
      Labels =
        word
        |> List.mapi (fun i t -> t, (i, i + 1))
        |> List.groupBy fst
        |> List.map (fun (t, cells) ->
            t, Matrix.create (n + 1) (n + 1) (fun i j -> cells |> List.exists (fun (_, (a, b)) -> (a, b) = (i, j))))
        |> Map.ofList }

/// A registry grammar case paired with a random word over its terminal alphabet.
let genCase: Gen<GrammarCase * Terminal<string> list> =
    MyGen.elements grammarCases
    |> MyGen.bind (fun c ->
        if List.isEmpty c.Alphabet then
            MyGen.constant (c, [])
        else
            MyGen.choose (0, 5)
            |> MyGen.bind (fun len ->
                MyGen.listOfLength len (MyGen.elements c.Alphabet)
                |> MyGen.map (fun ts -> c, List.map Terminal ts)))

type CfpqGrammarWordGenerators =
    static member Case() : Arbitrary<GrammarCase * Terminal<string> list> = MyArb.fromGen genCase

    static member Grammar() : Arbitrary<GrammarCase> =
        MyArb.fromGen (MyGen.elements grammarCases)

/// Compare a Kronecker result with an Azimov result computed on the same graph and language:
/// the start answer matrices must be equal on every cell, and for every nonterminal present in
/// both RSM.nonterminals rsm and the CNF's nonterminal set the n x n matrices must be equal.
/// Nonterminals dropped by toCnf have no counterpart and are skipped; the start-answer check
/// keeps the property non-vacuous when the start itself is replaced by a fresh one.
///
/// The start answer is compared on all cells: toCnf introduces a fresh start S' -> S | eps
/// whenever the original start is nullable, so L_CNF(S') = L_RSM(start) including epsilon. For a
/// nullable non-start A, however, the CNF matrix is L_RSM(A) \ {epsilon} (CNF allows epsilon only
/// at its own start), so only that diagonal (the empty path) can differ from the RSM's; those
/// per-nonterminal diagonal cells are excluded and every other cell is checked on both sides.
let compareKronVsAzimov
    (rsm: RSM<string, string>)
    (cnf: Grammar<string, string>)
    (kronResult: KroneckerCfpq.CfpqKronResult<string>)
    (azimovResult: AzimovCfpq.CfpqResult<string, bool>)
    : bool =
    let kronAnswer = KroneckerCfpq.answer kronResult rsm
    let n = Matrix.rows kronAnswer

    // Start answer matrix equality (all cells, including the empty-path diagonal).
    let azimovAnswer = AzimovCfpq.answer azimovResult cnf.Start

    let nullable = rsmNullable rsm
    let mutable ok = true

    for i in 0 .. n - 1 do
        for j in 0 .. n - 1 do
            if kronAnswer.[i, j] <> azimovAnswer.[i, j] then
                ok <- false

    // Per-nonterminal matrix equality over the shared nonterminals.
    let cnfNts =
        Set.toList (Set.union (Grammar.nonterminalsOf cnf) (Set.singleton cnf.Start))

    let shared =
        RSM.nonterminals rsm |> List.filter (fun a -> cnfNts |> List.contains a)

    for a in shared do
        let kronM = Map.find a kronResult.Matrices
        let azimovM = Map.find a azimovResult.Matrices

        for i in 0 .. n - 1 do
            for j in 0 .. n - 1 do
                // Skip the empty-path diagonal of RSM-nullable A: CNF allows epsilon only at its
                // own (possibly fresh) start, so L_CNF(A) = L_RSM(A) \ {epsilon} for such A.
                if
                    not (Set.contains a nullable && i = j)
                    && kronM.[i, j] <> Option.isSome azimovM.[i, j]
                then
                    ok <- false

    ok
