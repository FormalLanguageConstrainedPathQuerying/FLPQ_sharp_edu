namespace FLPQ.CFPQ

open FSharpPlus.Data
open FLPQ.CFPQ.CfpqInput
open FLPQ.CFPQ.CfpqSemiring
open FLPQ.Languages
open FLPQ.LinearAlgebra

module AzimovCfpq =

    /// A grammar rule in weak Chomsky normal form (WCNF): epsilon, one terminal, or two nonterminals.
    type WcnfRule<'t, 'nt> =
        | Epsilon of Nonterminal<'nt>
        | Terminal of Nonterminal<'nt> * 't
        | Binary of Nonterminal<'nt> * Nonterminal<'nt> * Nonterminal<'nt>

    /// Classify a grammar's rules into WCNF form, rejecting any other right-hand-side shape.
    let classifyRules (g: Grammar<'t, 'nt>) : WcnfRule<'t, 'nt> list =
        g.Rules
        |> List.map (fun r ->
            match r.Rhs with
            | Rhs.EpsilonRhs -> Epsilon r.Lhs
            | Rhs.Symbols nel ->
                match NonEmptyList.toList nel with
                | [ Symbol.T(FLPQ.Languages.Terminal t) ] -> WcnfRule.Terminal(r.Lhs, t)
                | [ Symbol.N b; Symbol.N c ] -> Binary(r.Lhs, b, c)
                | _ ->
                    invalidArg
                        "grammar"
                        $"Rule {r.Lhs} is not in WCNF: the right-hand side must be epsilon, one terminal, or two nonterminals")

    /// A matrix cell address: the nonterminal's matrix and the row/column of the cell.
    type CfpqCell<'nt> when 'nt: comparison =
        { Nt: Nonterminal<'nt>
          Row: int
          Col: int }

    /// A productive fixed-point iteration: the cells that changed in it and the matrix state
    /// after the iteration (for step-by-step visualization).
    type CfpqStep<'nt, 'elem> when 'nt: comparison =
        { Iteration: int
          ChangedCells: CfpqCell<'nt> list
          Matrices: Map<Nonterminal<'nt>, Matrix<'elem option>> }

    /// The result of Azimov's algorithm: the final matrix per nonterminal, the productive steps in
    /// iteration order, and the total number of loop applications (including the final no-change one).
    type CfpqResult<'nt, 'elem> when 'nt: comparison =
        { Matrices: Map<Nonterminal<'nt>, Matrix<'elem option>>
          Steps: CfpqStep<'nt, 'elem> list
          Iterations: int }

    /// Lexicographic cell order: nonterminal name, then row, then column.
    let private compareCells (a: CfpqCell<'nt>) (b: CfpqCell<'nt>) : int =
        let c = compare a.Nt b.Nt

        if c <> 0 then
            c
        else
            let c = compare a.Row b.Row

            if c <> 0 then c else compare a.Col b.Col

    /// Azimov's matrix-based algorithm (book Chapter 12, listing alg:graphParse): convert the grammar
    /// to CNF, initialize each nonterminal's matrix from its terminal and epsilon rules, then run the
    /// batch fixed point T <- T union (T x T) until an iteration changes nothing.
    let evaluate
        (semiring: Semiring<'elem>)
        (freshNt: int -> 'nt)
        (grammar: Grammar<'t, 'nt>)
        (graph: CfpqGraph<'t>)
        : CfpqResult<'nt, 'elem> =
        let cnf = Grammar.toCnf freshNt grammar
        let rules = classifyRules cnf
        let n = graph.VertexCount

        // T0: every nonterminal of the CNF grammar (left-hand sides, right-hand sides, and the start
        // symbol) starts with an empty matrix; terminal rules fill the cells of the label's adjacency
        // matrix and epsilon rules fill the diagonal (direct derivations).
        let emptyMat: Matrix<'elem option> = Matrix.init n n None

        let allNts = Set.union (Grammar.nonterminalsOf cnf) (Set.singleton cnf.Start)

        let mutable matrices: Map<Nonterminal<'nt>, Matrix<'elem option>> =
            allNts
            |> Set.toList
            |> List.fold (fun acc a -> Map.add a emptyMat acc) Map.empty

        for rule in rules do
            match rule with
            | Epsilon a ->
                let m = Map.find a matrices

                let updated =
                    Matrix.map2i
                        (fun i j cur _ ->
                            if i = j then
                                addCell semiring cur (Some(semiring.InitPath i i))
                            else
                                cur)
                        m
                        emptyMat

                matrices <- Map.add a updated matrices
            | Terminal(a, t) ->
                // A label absent from the graph contributes no cells.
                match Map.tryFind t graph.Labels with
                | None -> ()
                | Some adj ->
                    let m = Map.find a matrices

                    let values =
                        Matrix.map2i
                            (fun i j _ adjCell -> if adjCell then Some(semiring.InitPath i j) else None)
                            emptyMat
                            adj

                    matrices <- Map.add a (Matrix.map2 (addCell semiring) m values) matrices
            | Binary _ -> ()

        // Batch fixed point: each iteration computes every binary-rule product from a snapshot of the
        // current matrices and add-accumulates it into the left-hand side's matrix. The loop stops at
        // the first iteration that changes nothing; Iterations counts that final no-change application,
        // and Steps holds the productive iterations only.
        let mutable steps: CfpqStep<'nt, 'elem> list = []
        let mutable iteration = 0
        let mutable changedSomething = true

        while changedSomething do
            iteration <- iteration + 1
            let snapshot = matrices
            let mutable stepCells: CfpqCell<'nt> list = []

            for rule in rules do
                match rule with
                | Binary(a, b, c) ->
                    let prod =
                        Matrix.mxmi
                            (fun i j x y -> addCell semiring x y)
                            (fun _i k _j x y -> multCell semiring k x y)
                            None
                            (Map.find b snapshot)
                            (Map.find c snapshot)

                    let cur = Map.find a matrices
                    let updated = Matrix.map2 (addCell semiring) cur prod

                    let changes =
                        [ for i in 0 .. n - 1 do
                              for j in 0 .. n - 1 do
                                  if updated.[i, j] <> cur.[i, j] then
                                      { Nt = a; Row = i; Col = j } ]

                    if not (List.isEmpty changes) then
                        matrices <- Map.add a updated matrices
                        stepCells <- stepCells @ changes
                | _ -> ()

            if List.isEmpty stepCells then
                changedSomething <- false
            else
                let sorted = stepCells |> List.sortWith compareCells

                steps <-
                    { Iteration = iteration
                      ChangedCells = sorted
                      Matrices = matrices }
                    :: steps

        { Matrices = matrices
          Steps = List.rev steps
          Iterations = iteration }

    /// The reachability answer: the start nonterminal's matrix as a plain Boolean matrix.
    let answer (result: CfpqResult<'nt, bool>) (start: Nonterminal<'nt>) : Matrix<bool> =
        let m = Map.find start result.Matrices

        Matrix.map
            (function
            | Some _ -> true
            | None -> false)
            m

    let private hasEdge (graph: CfpqGraph<'t>) (u: int) (label: 't) (v: int) : bool =
        match Map.tryFind label graph.Labels with
        | None -> false
        | Some m -> m.[u, v]

    let private hasEpsilonRule (rules: WcnfRule<'t, 'nt> list) (a: Nonterminal<'nt>) : bool =
        List.exists
            (function
            | Epsilon x -> x = a
            | _ -> false)
            rules

    /// Extract one path from the singlePath semiring's fixed-point matrices (LAGraph PR #8): a
    /// height-1 cell is a direct derivation (epsilon when i = j, else one terminal edge), and a taller
    /// cell splits at Middle through a binary rule whose subpath heights are consistent. Returns None
    /// for no path, [] for epsilon, else the edge sequence as transitions (from, label, to).
    let extractSinglePath
        (rules: WcnfRule<'t, 'nt> list)
        (graph: CfpqGraph<'t>)
        (matrices: Map<Nonterminal<'nt>, Matrix<PathIndex option>>)
        (nt: Nonterminal<'nt>)
        (i: int)
        (j: int)
        : Trans<'t> list option =

        let rec extract (a: Nonterminal<'nt>) (i: int) (j: int) : Trans<'t> list option =
            match matrices.[a].[i, j] with
            | None -> None
            | Some p when p.Height = 1 ->
                // Direct derivation: epsilon (only when i = j), else one terminal edge.
                if i = j && hasEpsilonRule rules a then
                    Some []
                else
                    rules
                    |> List.tryFind (function
                        | Terminal(x, t) -> x = a && hasEdge graph i t j
                        | _ -> false)
                    |> Option.map (function
                        | Terminal(_, t) -> [ { From = i; Label = t; To = j } ]
                        | _ -> [])
            | Some p ->
                // Split at the middle vertex through a binary rule A -> BC with consistent heights.
                let k = p.Middle

                let rule =
                    rules
                    |> List.tryFind (function
                        | Binary(x, b, c) ->
                            x = a
                            && match matrices.[b].[i, k], matrices.[c].[k, j] with
                               | Some pb, Some pc -> max pb.Height pc.Height + 1 = p.Height
                               | _ -> false
                        | _ -> false)

                match rule with
                | None -> None
                | Some(Binary(_, b, c)) ->
                    match extract b i k, extract c k j with
                    | Some left, Some right -> Some(left @ right)
                    | _ -> None
                | _ -> None

        extract nt i j

    let private isSimplePath (p: Trans<'t> list) : bool =
        let verts =
            match p with
            | [] -> []
            | e :: _ -> e.From :: (List.map (fun e -> e.To) p)

        Set.count (Set.ofList verts) = List.length verts

    /// Enumerate all simple paths from i to j derived by nt from the allPaths semiring's fixed-point
    /// matrices (LAGraph PR #9): a Base cell yields the direct derivations (epsilon when i = j, one
    /// edge per matching terminal rule), and each split vertex k of a Splits cell joins a B-path i..k
    /// with a C-path k..j for every binary rule A -> BC; a concatenation is kept only if it is a
    /// simple path. The visited set (initially {i, j}) bounds the recursion depth: after toCnf no
    /// right-hand-side nonterminal derives epsilon, so every split vertex of a simple path is an
    /// internal vertex and is never skipped by the visited filter.
    let enumerateAllPaths
        (rules: WcnfRule<'t, 'nt> list)
        (graph: CfpqGraph<'t>)
        (matrices: Map<Nonterminal<'nt>, Matrix<AllPathsElem option>>)
        (nt: Nonterminal<'nt>)
        (i: int)
        (j: int)
        : Trans<'t> list list =

        let directPaths (a: Nonterminal<'nt>) (i: int) (j: int) : Trans<'t> list list =
            let eps = if i = j && hasEpsilonRule rules a then [ [] ] else []

            let edges =
                rules
                |> List.collect (function
                    | Terminal(x, t) when x = a && hasEdge graph i t j -> [ [ { From = i; Label = t; To = j } ] ]
                    | _ -> [])

            eps @ edges

        let rec enumerate (a: Nonterminal<'nt>) (i: int) (j: int) (visited: Set<int>) : Trans<'t> list list =
            match matrices.[a].[i, j] with
            | None -> []
            | Some Base -> directPaths a i j
            | Some(Splits ks) ->
                NonEmptyList.toList ks
                |> List.collect (fun k ->
                    if Set.contains k visited then
                        []
                    else if k = allPathsSentinel then
                        directPaths a i j
                    else
                        rules
                        |> List.collect (function
                            | Binary(x, b, c) when x = a ->
                                match matrices.[b].[i, k], matrices.[c].[k, j] with
                                | Some _, Some _ ->
                                    let lefts = enumerate b i k (Set.add k visited)
                                    let rights = enumerate c k j (Set.add k visited)

                                    lefts
                                    |> List.collect (fun l ->
                                        rights |> List.map (fun r -> l @ r) |> List.filter isSimplePath)
                                | _ -> []
                            | _ -> []))
                |> List.distinct

        enumerate nt i j (Set.ofList [ i; j ])
