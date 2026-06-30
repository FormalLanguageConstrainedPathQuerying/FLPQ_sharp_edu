namespace FLPQ.Languages

open FLPQ.LinearAlgebra

module Valiant =

    type Submatrix = { A: int; B: int; Size: int }

    /// Data for a single Valiant algorithm trace step.
    [<Struct>]
    type ValiantTraceStep<'nt when 'nt: comparison> =
        { table: Matrix<Set<Nonterminal<'nt>>> }

    /// Data for a single modified Valiant algorithm trace step.
    /// Each step corresponds to a layer of disjoint submatrices.
    [<Struct>]
    type ModifiedValiantTraceStep<'nt when 'nt: comparison> =
        { table: Matrix<Set<Nonterminal<'nt>>>
          layerSize: int
          submatrices: Submatrix list }

    let private submatrixCells (m: Submatrix) : (int * int) list =
        [ for i in m.A - m.Size + 1 .. m.A do
              for j in m.B .. m.B + m.Size - 1 do
                  yield (i, j) ]

    let private bottomSubmatrix (m: Submatrix) : Submatrix =
        let half = m.Size / 2
        { A = m.A; B = m.B; Size = half }

    let private leftSubmatrix (m: Submatrix) : Submatrix =
        let half = m.Size / 2
        { A = m.A - half; B = m.B; Size = half }

    let private rightSubmatrix (m: Submatrix) : Submatrix =
        let half = m.Size / 2
        { A = m.A; B = m.B + half; Size = half }

    let private topSubmatrix (m: Submatrix) : Submatrix =
        let half = m.Size / 2

        { A = m.A - half
          B = m.B + half
          Size = half }

    let private sshift (m: Submatrix) (di: int) (dj: int) : Submatrix =
        { A = m.A + di
          B = m.B + dj
          Size = m.Size }

    let private rightGrounded (m: Submatrix) : Submatrix = sshift m (m.B - m.A - 1) 0

    let private leftGrounded (m: Submatrix) : Submatrix = sshift m 0 (-(m.B - m.A - 1))

    /// Shift submatrix down by its size (maps left quarter to bottom quarter of the same parent).
    let private rightNeighbor (m: Submatrix) : Submatrix = sshift m m.Size 0

    /// Shift submatrix left by its size (maps right quarter to bottom quarter of the same parent).
    let private leftNeighbor (m: Submatrix) : Submatrix = sshift m 0 (-m.Size)

    /// Construct V-layer i: disjoint submatrices of size 2^i fitting within the table.
    let private constructLayer (layer: int) (tableSize: int) : Submatrix list =
        let size = 1 <<< layer
        let baseA = size - 1
        let baseB = size

        let maxK = (tableSize - 2 * size) / size

        [ for k in 0..maxK do
              let a = baseA + k * size
              let b = baseB + k * size

              if a < tableSize && b + size <= tableSize then
                  yield { A = a; B = b; Size = size } ]

    let private nextPowerOfTwo (n: int) : int =
        let mutable p = 1

        while p < n do
            p <- p * 2

        p

    let private extractSlice (fullMatrix: Matrix<bool>) (m: Submatrix) : Matrix<bool> =
        Matrix.create m.Size m.Size (fun i j -> fullMatrix.data.[m.A - m.Size + 1 + i, m.B + j])

    let private writeSlice (target: Matrix<bool>) (m: Submatrix) (slice: Matrix<bool>) : unit =
        for i in 0 .. m.Size - 1 do
            for j in 0 .. m.Size - 1 do
                if slice.data.[i, j] then
                    target.data.[m.A - m.Size + 1 + i, m.B + j] <- true

    let private performMultiplications
        (tByNt: System.Collections.Generic.Dictionary<Nonterminal<'nt>, Matrix<bool>>)
        (pByPair: System.Collections.Generic.Dictionary<Nonterminal<'nt> * Nonterminal<'nt>, Matrix<bool>>)
        (tasks: (Submatrix * Submatrix * Submatrix) list)
        (pairs: (Nonterminal<'nt> * Nonterminal<'nt>) list)
        : unit =
        for (mTarget, m1, m2) in tasks do
            for pair in pairs do
                let leftNt, rightNt = pair

                let leftSlice =
                    match tByNt.TryGetValue leftNt with
                    | true, mat -> extractSlice mat m1
                    | _ -> Matrix.init m1.Size m1.Size false

                let rightSlice =
                    match tByNt.TryGetValue rightNt with
                    | true, mat -> extractSlice mat m2
                    | _ -> Matrix.init m2.Size m2.Size false

                let product = LinearAlgebra.mxm leftSlice rightSlice (&&) (||) false

                let pairMatrix =
                    match pByPair.TryGetValue pair with
                    | true, mat -> mat
                    | _ ->
                        let mat = pByPair.Values |> Seq.head
                        mat

                writeSlice pairMatrix mTarget product

    let rec private complete
        (tByNt: System.Collections.Generic.Dictionary<Nonterminal<'nt>, Matrix<bool>>)
        (pByPair: System.Collections.Generic.Dictionary<Nonterminal<'nt> * Nonterminal<'nt>, Matrix<bool>>)
        (m: Submatrix)
        (terminalRules: Map<'t, Nonterminal<'nt> list>)
        (binaryRules: (Nonterminal<'nt> * (Nonterminal<'nt> * Nonterminal<'nt>)) list)
        (pairs: (Nonterminal<'nt> * Nonterminal<'nt>) list)
        (tokens: 't[])
        : unit =
        if m.Size = 1 then
            let i = m.A - m.Size + 1
            let j = m.B

            if i + 1 = j && i < tokens.Length then
                let ch = tokens.[i]

                match Map.tryFind ch terminalRules with
                | Some nts ->
                    for nt in nts do
                        match tByNt.TryGetValue nt with
                        | true, mat -> mat.data.[i, j] <- true
                        | _ -> ()
                | None -> ()
            else
                for pair in pairs do
                    let pairHasValue =
                        match pByPair.TryGetValue pair with
                        | true, mat -> mat.data.[i, j]
                        | _ -> false

                    if pairHasValue then
                        for (a, bc) in binaryRules do
                            if bc = pair then
                                match tByNt.TryGetValue a with
                                | true, mat -> mat.data.[i, j] <- true
                                | _ -> ()
        else
            let b = bottomSubmatrix m
            let l = leftSubmatrix m
            let r = rightSubmatrix m
            let t = topSubmatrix m

            complete tByNt pByPair b terminalRules binaryRules pairs tokens
            performMultiplications tByNt pByPair [ (l, leftGrounded l, b) ] pairs
            complete tByNt pByPair l terminalRules binaryRules pairs tokens
            performMultiplications tByNt pByPair [ (r, b, rightGrounded r) ] pairs
            complete tByNt pByPair r terminalRules binaryRules pairs tokens
            performMultiplications tByNt pByPair [ (t, leftGrounded t, r) ] pairs
            performMultiplications tByNt pByPair [ (t, l, rightGrounded t) ] pairs
            complete tByNt pByPair t terminalRules binaryRules pairs tokens

    and private compute
        (tByNt: System.Collections.Generic.Dictionary<Nonterminal<'nt>, Matrix<bool>>)
        (pByPair: System.Collections.Generic.Dictionary<Nonterminal<'nt> * Nonterminal<'nt>, Matrix<bool>>)
        (i: int)
        (j: int)
        (terminalRules: Map<'t, Nonterminal<'nt> list>)
        (binaryRules: (Nonterminal<'nt> * (Nonterminal<'nt> * Nonterminal<'nt>)) list)
        (pairs: (Nonterminal<'nt> * Nonterminal<'nt>) list)
        (tokens: 't[])
        : unit =
        if j - i >= 4 then
            let mid = (i + j) / 2
            compute tByNt pByPair i mid terminalRules binaryRules pairs tokens
            compute tByNt pByPair mid j terminalRules binaryRules pairs tokens

        let a = (i + j) / 2 - 1
        let b = (i + j) / 2
        let size = (j - i) / 2

        let m = { A = a; B = b; Size = size }
        complete tByNt pByPair m terminalRules binaryRules pairs tokens

    /// Modified Valiant: process a set M of submatrices of equal size.
    /// Recursively fills T[i,j] for bottom cells where i+1 ≠ j (base case),
    /// or delegates to completeVLayer after processing bottom quarters.
    let rec private completeLayerModified
        (tByNt: System.Collections.Generic.Dictionary<Nonterminal<'nt>, Matrix<bool>>)
        (pByPair: System.Collections.Generic.Dictionary<Nonterminal<'nt> * Nonterminal<'nt>, Matrix<bool>>)
        (mList: Submatrix list)
        (terminalRules: Map<'t, Nonterminal<'nt> list>)
        (binaryRules: (Nonterminal<'nt> * (Nonterminal<'nt> * Nonterminal<'nt>)) list)
        (pairs: (Nonterminal<'nt> * Nonterminal<'nt>) list)
        : unit =
        match mList with
        | [] -> ()
        | first :: _ ->
            if first.Size = 1 then
                for m in mList do
                    let i = m.A - m.Size + 1
                    let j = m.B

                    if i + 1 <> j then
                        for pair in pairs do
                            let pairHasValue =
                                match pByPair.TryGetValue pair with
                                | true, mat -> mat.data.[i, j]
                                | _ -> false

                            if pairHasValue then
                                for (a, bc) in binaryRules do
                                    if bc = pair then
                                        match tByNt.TryGetValue a with
                                        | true, mat -> mat.data.[i, j] <- true
                                        | _ -> ()
            else
                let bottomLayer = mList |> List.map bottomSubmatrix
                completeLayerModified tByNt pByPair bottomLayer terminalRules binaryRules pairs
                completeVLayerModified tByNt pByPair mList terminalRules binaryRules pairs

    /// Modified Valiant: process a V-layer M of disjoint submatrices of equal size.
    /// Groups multiplications into three parallel batches across all submatrices.
    and private completeVLayerModified
        (tByNt: System.Collections.Generic.Dictionary<Nonterminal<'nt>, Matrix<bool>>)
        (pByPair: System.Collections.Generic.Dictionary<Nonterminal<'nt> * Nonterminal<'nt>, Matrix<bool>>)
        (mList: Submatrix list)
        (terminalRules: Map<'t, Nonterminal<'nt> list>)
        (binaryRules: (Nonterminal<'nt> * (Nonterminal<'nt> * Nonterminal<'nt>)) list)
        (pairs: (Nonterminal<'nt> * Nonterminal<'nt>) list)
        : unit =
        let leftSubLayer = mList |> List.map leftSubmatrix
        let rightSubLayer = mList |> List.map rightSubmatrix
        let topSubLayer = mList |> List.map topSubmatrix

        let firstTasks =
            [ for m in leftSubLayer do
                  yield (m, leftGrounded m, rightNeighbor m)
              for m in rightSubLayer do
                  yield (m, leftNeighbor m, rightGrounded m) ]

        performMultiplications tByNt pByPair firstTasks pairs

        completeLayerModified tByNt pByPair (leftSubLayer @ rightSubLayer) terminalRules binaryRules pairs

        let secondTasks =
            [ for m in topSubLayer do
                  yield (m, leftGrounded m, rightNeighbor m) ]

        performMultiplications tByNt pByPair secondTasks pairs

        let thirdTasks =
            [ for m in topSubLayer do
                  yield (m, leftNeighbor m, rightGrounded m) ]

        performMultiplications tByNt pByPair thirdTasks pairs

        completeLayerModified tByNt pByPair topSubLayer terminalRules binaryRules pairs

    let private terminalRulesFromGrammar (cnf: Grammar<'t, 'nt>) : Map<'t, Nonterminal<'nt> list> =
        cnf.rules
        |> List.choose (fun r ->
            match Rhs.toSymbols r.rhs with
            | [ T(Terminal t) ] -> Some(t, r.lhs)
            | _ -> None)
        |> List.groupBy fst
        |> List.map (fun (t, pairs) -> t, pairs |> List.map snd)
        |> Map.ofList

    let private binaryRulesFromGrammar
        (cnf: Grammar<'t, 'nt>)
        : (Nonterminal<'nt> * (Nonterminal<'nt> * Nonterminal<'nt>)) list =
        cnf.rules
        |> List.choose (fun r ->
            match Rhs.toSymbols r.rhs with
            | [ N left; N right ] -> Some(r.lhs, (left, right))
            | _ -> None)

    /// Parse pre-tokenized input using Valiant's algorithm.
    let parseWithTable (g: Grammar<'t, 'nt>) (tokens: 't list) : Matrix<Set<Nonterminal<'nt>>> * bool =
        let cnf = Grammar.toCnf g
        let tokensArr = tokens |> Array.ofList
        let n = tokensArr.Length
        let originalN = n
        let paddedN = nextPowerOfTwo (n + 1) - 1
        let tableSize = paddedN + 1

        let allNt = cnf.rules |> List.map (fun r -> r.lhs) |> List.distinct

        let terminalRules = terminalRulesFromGrammar cnf
        let binaryRules = binaryRulesFromGrammar cnf
        let pairs = binaryRules |> List.map snd |> List.distinct

        let tByNt = System.Collections.Generic.Dictionary<Nonterminal<'nt>, Matrix<bool>>()

        let initMatrix = Matrix.init tableSize tableSize (Set.empty: Set<Nonterminal<'nt>>)

        for i in 0 .. tokensArr.Length - 1 do
            match Map.tryFind tokensArr.[i] terminalRules with
            | Some nts ->
                for nt in nts do
                    initMatrix.data.[i, i + 1] <- Set.add nt initMatrix.data.[i, i + 1]
            | None -> ()

        let initDecomp = BooleanDecomposition.decompose initMatrix

        for nt in allNt do
            match Map.tryFind nt initDecomp with
            | Some mat -> tByNt.[nt] <- mat
            | None -> tByNt.[nt] <- Matrix.init tableSize tableSize false

        let pByPair =
            System.Collections.Generic.Dictionary<Nonterminal<'nt> * Nonterminal<'nt>, Matrix<bool>>()

        for pair in pairs do
            pByPair.[pair] <- Matrix.init tableSize tableSize false

        if tokens.IsEmpty then
            let epsAccepted =
                cnf.rules |> List.exists (fun r -> r.lhs = cnf.start && Rhs.isEpsilon r.rhs)

            let emptyResult = Matrix.init 0 0 Set.empty
            (emptyResult, epsAccepted)
        else
            compute tByNt pByPair 0 tableSize terminalRules binaryRules pairs tokensArr

            let decompMap =
                allNt
                |> List.map (fun nt ->
                    match tByNt.TryGetValue nt with
                    | true, mat -> (nt, mat)
                    | _ -> (nt, Matrix.init tableSize tableSize false))
                |> Map.ofList

            let fullMatrix = BooleanDecomposition.recompose decompMap

            let result = Matrix.create n n (fun i j -> fullMatrix.data.[i, j + 1])

            let accepted =
                match tByNt.TryGetValue cnf.start with
                | true, mat -> mat.data.[0, originalN]
                | _ -> false

            (result, accepted)

    let parse (g: Grammar<'t, 'nt>) (tokens: 't list) : bool = parseWithTable g tokens |> snd

    /// Run Valiant with step-by-step trace.
    let parseWithTrace (g: Grammar<'t, 'nt>) (tokens: 't list) : ValiantTraceStep<'nt> list =
        let cnf = Grammar.toCnf g
        let tokensArr = tokens |> Array.ofList
        let n = tokensArr.Length
        let paddedN = nextPowerOfTwo (n + 1) - 1
        let tableSize = paddedN + 1

        let allNt = cnf.rules |> List.map (fun r -> r.lhs) |> List.distinct

        let terminalRules = terminalRulesFromGrammar cnf
        let binaryRules = binaryRulesFromGrammar cnf
        let pairs = binaryRules |> List.map snd |> List.distinct

        let tByNt = System.Collections.Generic.Dictionary<Nonterminal<'nt>, Matrix<bool>>()

        let initMatrix = Matrix.init tableSize tableSize (Set.empty: Set<Nonterminal<'nt>>)

        for i in 0 .. tokensArr.Length - 1 do
            match Map.tryFind tokensArr.[i] terminalRules with
            | Some nts ->
                for nt in nts do
                    initMatrix.data.[i, i + 1] <- Set.add nt initMatrix.data.[i, i + 1]
            | None -> ()

        let initDecomp = BooleanDecomposition.decompose initMatrix

        for nt in allNt do
            match Map.tryFind nt initDecomp with
            | Some mat -> tByNt.[nt] <- mat
            | None -> tByNt.[nt] <- Matrix.init tableSize tableSize false

        let pByPair =
            System.Collections.Generic.Dictionary<Nonterminal<'nt> * Nonterminal<'nt>, Matrix<bool>>()

        for pair in pairs do
            pByPair.[pair] <- Matrix.init tableSize tableSize false

        if tokens.IsEmpty then
            []
        else
            let mutable steps = []

            let rec completeTrace m : unit =
                if m.Size = 1 then
                    let i = m.A - m.Size + 1
                    let j = m.B

                    if i + 1 = j && i < tokensArr.Length then
                        let ch = tokensArr.[i]

                        match Map.tryFind ch terminalRules with
                        | Some nts ->
                            for nt in nts do
                                match tByNt.TryGetValue nt with
                                | true, mat -> mat.data.[i, j] <- true
                                | _ -> ()
                        | None -> ()
                    else
                        for pair in pairs do
                            let pairHasValue =
                                match pByPair.TryGetValue pair with
                                | true, mat -> mat.data.[i, j]
                                | _ -> false

                            if pairHasValue then
                                for (a, bc) in binaryRules do
                                    if bc = pair then
                                        match tByNt.TryGetValue a with
                                        | true, mat -> mat.data.[i, j] <- true
                                        | _ -> ()

                    let decompMap =
                        allNt
                        |> List.map (fun nt ->
                            match tByNt.TryGetValue nt with
                            | true, mat -> (nt, mat)
                            | _ -> (nt, Matrix.init tableSize tableSize false))
                        |> Map.ofList

                    let fullMatrix = BooleanDecomposition.recompose decompMap

                    let recomposed = Matrix.create n n (fun ri rj -> fullMatrix.data.[ri, rj + 1])

                    steps <- { table = recomposed } :: steps
                else
                    let b = bottomSubmatrix m
                    let l = leftSubmatrix m
                    let r = rightSubmatrix m
                    let t = topSubmatrix m

                    completeTrace b
                    performMultiplications tByNt pByPair [ (l, leftGrounded l, b) ] pairs
                    completeTrace l
                    performMultiplications tByNt pByPair [ (r, b, rightGrounded r) ] pairs
                    completeTrace r
                    performMultiplications tByNt pByPair [ (t, leftGrounded t, r) ] pairs
                    performMultiplications tByNt pByPair [ (t, l, rightGrounded t) ] pairs
                    completeTrace t

                    let decompMap =
                        allNt
                        |> List.map (fun nt ->
                            match tByNt.TryGetValue nt with
                            | true, mat -> (nt, mat)
                            | _ -> (nt, Matrix.init tableSize tableSize false))
                        |> Map.ofList

                    let fullMatrix = BooleanDecomposition.recompose decompMap

                    let recomposed = Matrix.create n n (fun ri rj -> fullMatrix.data.[ri, rj + 1])

                    steps <- { table = recomposed } :: steps

            and computeTrace i j : unit =
                if j - i >= 4 then
                    let mid = (i + j) / 2
                    computeTrace i mid
                    computeTrace mid j

                let a = (i + j) / 2 - 1
                let b = (i + j) / 2
                let size = (j - i) / 2

                let m = { A = a; B = b; Size = size }
                completeTrace m

            computeTrace 0 tableSize

            List.rev steps

    /// Modified Valiant: parse with table.
    /// Uses V-shaped layers of disjoint submatrices for batched parallel multiplications.
    let parseModifiedWithTable (g: Grammar<'t, 'nt>) (tokens: 't list) : Matrix<Set<Nonterminal<'nt>>> * bool =
        let cnf = Grammar.toCnf g
        let tokensArr = tokens |> Array.ofList
        let n = tokensArr.Length
        let originalN = n
        let paddedN = nextPowerOfTwo (n + 1) - 1
        let tableSize = paddedN + 1

        let allNt = cnf.rules |> List.map (fun r -> r.lhs) |> List.distinct

        let terminalRules = terminalRulesFromGrammar cnf
        let binaryRules = binaryRulesFromGrammar cnf
        let pairs = binaryRules |> List.map snd |> List.distinct

        let tByNt = System.Collections.Generic.Dictionary<Nonterminal<'nt>, Matrix<bool>>()

        let initMatrix = Matrix.init tableSize tableSize (Set.empty: Set<Nonterminal<'nt>>)

        for i in 0 .. tokensArr.Length - 1 do
            match Map.tryFind tokensArr.[i] terminalRules with
            | Some nts ->
                for nt in nts do
                    initMatrix.data.[i, i + 1] <- Set.add nt initMatrix.data.[i, i + 1]
            | None -> ()

        let initDecomp = BooleanDecomposition.decompose initMatrix

        for nt in allNt do
            match Map.tryFind nt initDecomp with
            | Some mat -> tByNt.[nt] <- mat
            | None -> tByNt.[nt] <- Matrix.init tableSize tableSize false

        let pByPair =
            System.Collections.Generic.Dictionary<Nonterminal<'nt> * Nonterminal<'nt>, Matrix<bool>>()

        for pair in pairs do
            pByPair.[pair] <- Matrix.init tableSize tableSize false

        if tokens.IsEmpty then
            let epsAccepted =
                cnf.rules |> List.exists (fun r -> r.lhs = cnf.start && Rhs.isEpsilon r.rhs)

            let emptyResult = Matrix.init 0 0 Set.empty
            (emptyResult, epsAccepted)
        else
            let maxLayer = int (System.Math.Log(float tableSize, 2.0))

            for layer in 1..maxLayer do
                let layerSubmatrices = constructLayer layer tableSize

                if not (List.isEmpty layerSubmatrices) then
                    completeLayerModified tByNt pByPair layerSubmatrices terminalRules binaryRules pairs

            let decompMap =
                allNt
                |> List.map (fun nt ->
                    match tByNt.TryGetValue nt with
                    | true, mat -> (nt, mat)
                    | _ -> (nt, Matrix.init tableSize tableSize false))
                |> Map.ofList

            let fullMatrix = BooleanDecomposition.recompose decompMap

            let result = Matrix.create n n (fun i j -> fullMatrix.data.[i, j + 1])

            let accepted =
                match tByNt.TryGetValue cnf.start with
                | true, mat -> mat.data.[0, originalN]
                | _ -> false

            (result, accepted)

    /// Modified Valiant: check acceptance only.
    let parseModified (g: Grammar<'t, 'nt>) (tokens: 't list) : bool = parseModifiedWithTable g tokens |> snd

    /// Modified Valiant: run with step-by-step trace.
    let parseModifiedWithTrace (g: Grammar<'t, 'nt>) (tokens: 't list) : ModifiedValiantTraceStep<'nt> list =
        let cnf = Grammar.toCnf g
        let tokensArr = tokens |> Array.ofList
        let n = tokensArr.Length
        let paddedN = nextPowerOfTwo (n + 1) - 1
        let tableSize = paddedN + 1

        let allNt = cnf.rules |> List.map (fun r -> r.lhs) |> List.distinct

        let terminalRules = terminalRulesFromGrammar cnf
        let binaryRules = binaryRulesFromGrammar cnf
        let pairs = binaryRules |> List.map snd |> List.distinct

        let tByNt = System.Collections.Generic.Dictionary<Nonterminal<'nt>, Matrix<bool>>()

        let initMatrix = Matrix.init tableSize tableSize (Set.empty: Set<Nonterminal<'nt>>)

        for i in 0 .. tokensArr.Length - 1 do
            match Map.tryFind tokensArr.[i] terminalRules with
            | Some nts ->
                for nt in nts do
                    initMatrix.data.[i, i + 1] <- Set.add nt initMatrix.data.[i, i + 1]
            | None -> ()

        let initDecomp = BooleanDecomposition.decompose initMatrix

        for nt in allNt do
            match Map.tryFind nt initDecomp with
            | Some mat -> tByNt.[nt] <- mat
            | None -> tByNt.[nt] <- Matrix.init tableSize tableSize false

        let pByPair =
            System.Collections.Generic.Dictionary<Nonterminal<'nt> * Nonterminal<'nt>, Matrix<bool>>()

        for pair in pairs do
            pByPair.[pair] <- Matrix.init tableSize tableSize false

        if tokens.IsEmpty then
            []
        else
            let mutable steps = []
            let maxLayer = int (System.Math.Log(float tableSize, 2.0))

            for layer in 1..maxLayer do
                let layerSubmatrices = constructLayer layer tableSize

                if not (List.isEmpty layerSubmatrices) then
                    completeLayerModified tByNt pByPair layerSubmatrices terminalRules binaryRules pairs

                    let decompMap =
                        allNt
                        |> List.map (fun nt ->
                            match tByNt.TryGetValue nt with
                            | true, mat -> (nt, mat)
                            | _ -> (nt, Matrix.init tableSize tableSize false))
                        |> Map.ofList

                    let fullMatrix = BooleanDecomposition.recompose decompMap

                    let recomposed = Matrix.create n n (fun ri rj -> fullMatrix.data.[ri, rj + 1])

                    steps <-
                        { ModifiedValiantTraceStep.table = recomposed
                          layerSize = 1 <<< layer
                          submatrices = layerSubmatrices }
                        :: steps

            List.rev steps

    /// Convert a modified Valiant trace step to TeX with highlighted submatrices.
    /// Uses different colors for different submatrices within the layer.
    /// Submatrix coordinates are from the padded matrix and are clipped to the n×n recomposed matrix.
    let stepToTeX (cellPrinter: Set<Nonterminal<'nt>> -> string) (step: ModifiedValiantTraceStep<'nt>) : string =
        let colors =
            [ "red"
              "blue"
              "green"
              "orange"
              "purple"
              "brown"
              "cyan"
              "magenta"
              "teal"
              "olive" ]

        let n = step.table.rows

        let blocks =
            step.submatrices
            |> List.mapi (fun idx m ->
                let color = colors.[idx % colors.Length]

                let startRow = m.A - m.Size + 1
                let endRow = m.A

                let startCol = m.B - 1
                let endCol = m.B + m.Size - 2

                let clippedStartRow = max 0 startRow
                let clippedEndRow = min (n - 1) endRow
                let clippedStartCol = max 0 startCol
                let clippedEndCol = min (n - 1) endCol

                if clippedStartRow <= clippedEndRow && clippedStartCol <= clippedEndCol then
                    let block: Matrix.SubmatrixBlock =
                        { startRow = clippedStartRow
                          startCol = clippedStartCol
                          rowCount = clippedEndRow - clippedStartRow + 1
                          colCount = clippedEndCol - clippedStartCol + 1
                          borderColor = Some color
                          fillColor = None }

                    Some block
                else
                    None)
            |> List.choose id

        Matrix.toTeXStyled false false cellPrinter step.table [] blocks
