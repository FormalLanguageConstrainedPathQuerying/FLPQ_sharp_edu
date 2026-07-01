namespace FLPQ.Languages

open FLPQ.LinearAlgebra

module Valiant =

    type Submatrix = { A: int; B: int; Size: int }

    [<Struct>]
    type ValiantTraceStep<'nt when 'nt: comparison> =
        { table: Matrix<Set<Nonterminal<'nt>>> }

    [<Struct>]
    type ModifiedValiantTraceStep<'nt when 'nt: comparison> =
        { table: Matrix<Set<Nonterminal<'nt>>>
          layerSize: int
          submatrices: Submatrix list }

    type private InitData<'t, 'nt when 't: comparison and 'nt: comparison> =
        { tByNt: System.Collections.Generic.Dictionary<Nonterminal<'nt>, Matrix<bool>>
          pByPair: System.Collections.Generic.Dictionary<Nonterminal<'nt> * Nonterminal<'nt>, Matrix<bool>>
          tokensArr: 't[]
          tableSize: int
          n: int
          allNt: Nonterminal<'nt> list
          binaryRules: (Nonterminal<'nt> * (Nonterminal<'nt> * Nonterminal<'nt>)) list
          pairs: (Nonterminal<'nt> * Nonterminal<'nt>) list
          terminalRules: Map<'t, Nonterminal<'nt> list> }

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

    let private rightNeighbor (m: Submatrix) : Submatrix = sshift m m.Size 0

    let private leftNeighbor (m: Submatrix) : Submatrix = sshift m 0 (-m.Size)

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

    let private recomposeStep
        (allNt: Nonterminal<'nt> list)
        (tByNt: System.Collections.Generic.Dictionary<Nonterminal<'nt>, Matrix<bool>>)
        (tableSize: int)
        (n: int)
        : ValiantTraceStep<'nt> =
        let decompMap =
            allNt
            |> List.map (fun nt ->
                match tByNt.TryGetValue nt with
                | true, mat -> (nt, mat)
                | _ -> (nt, Matrix.init tableSize tableSize false))
            |> Map.ofList

        let fullMatrix = BooleanDecomposition.recompose decompMap

        let recomposed = Matrix.create n n (fun ri rj -> fullMatrix.data.[ri, rj + 1])

        { table = recomposed }

    let rec private complete
        (init: InitData<'t, 'nt>)
        (m: Submatrix)
        (traceAcc: ResizeArray<ValiantTraceStep<'nt>> option)
        : unit =
        if m.Size = 1 then
            let i = m.A - m.Size + 1
            let j = m.B

            if i + 1 = j && i < init.tokensArr.Length then
                let ch = init.tokensArr.[i]

                match Map.tryFind ch init.terminalRules with
                | Some nts ->
                    for nt in nts do
                        match init.tByNt.TryGetValue nt with
                        | true, mat -> mat.data.[i, j] <- true
                        | _ -> ()
                | None -> ()
            else
                for pair in init.pairs do
                    let pairHasValue =
                        match init.pByPair.TryGetValue pair with
                        | true, mat -> mat.data.[i, j]
                        | _ -> false

                    if pairHasValue then
                        for (a, bc) in init.binaryRules do
                            if bc = pair then
                                match init.tByNt.TryGetValue a with
                                | true, mat -> mat.data.[i, j] <- true
                                | _ -> ()

            match traceAcc with
            | Some steps -> steps.Add(recomposeStep init.allNt init.tByNt init.tableSize init.n)
            | None -> ()
        else
            let b = bottomSubmatrix m
            let l = leftSubmatrix m
            let r = rightSubmatrix m
            let te = topSubmatrix m

            complete init b traceAcc
            performMultiplications init.tByNt init.pByPair [ (l, leftGrounded l, b) ] init.pairs
            complete init l traceAcc
            performMultiplications init.tByNt init.pByPair [ (r, b, rightGrounded r) ] init.pairs
            complete init r traceAcc
            performMultiplications init.tByNt init.pByPair [ (te, leftGrounded te, r) ] init.pairs
            performMultiplications init.tByNt init.pByPair [ (te, l, rightGrounded te) ] init.pairs
            complete init te traceAcc

            match traceAcc with
            | Some steps -> steps.Add(recomposeStep init.allNt init.tByNt init.tableSize init.n)
            | None -> ()

    and private compute
        (init: InitData<'t, 'nt>)
        (i: int)
        (j: int)
        (traceAcc: ResizeArray<ValiantTraceStep<'nt>> option)
        : unit =
        if j - i >= 4 then
            let mid = (i + j) / 2
            compute init i mid traceAcc
            compute init mid j traceAcc

        let a = (i + j) / 2 - 1
        let b = (i + j) / 2
        let size = (j - i) / 2

        let m = { A = a; B = b; Size = size }
        complete init m traceAcc

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

    let private initValiant (cnf: Grammar<'t, 'nt>) (tokensArr: 't[]) : InitData<'t, 'nt> =
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

        { tByNt = tByNt
          pByPair = pByPair
          tokensArr = tokensArr
          tableSize = tableSize
          n = n
          allNt = allNt
          binaryRules = binaryRules
          pairs = pairs
          terminalRules = terminalRules }

    let parseWithTable
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : Matrix<Set<Nonterminal<'nt>>> * bool =
        let cnf = Grammar.toCnf freshNonterminal g
        let tokensArr = terminals |> List.map (fun (Terminal t) -> t) |> Array.ofList

        if tokensArr.Length = 0 then
            let epsAccepted =
                cnf.rules |> List.exists (fun r -> r.lhs = cnf.start && Rhs.isEpsilon r.rhs)

            let emptyResult = Matrix.init 0 0 Set.empty
            (emptyResult, epsAccepted)
        else
            let init = initValiant cnf tokensArr
            compute init 0 init.tableSize None

            let decompMap =
                init.allNt
                |> List.map (fun nt ->
                    match init.tByNt.TryGetValue nt with
                    | true, mat -> (nt, mat)
                    | _ -> (nt, Matrix.init init.tableSize init.tableSize false))
                |> Map.ofList

            let fullMatrix = BooleanDecomposition.recompose decompMap

            let result = Matrix.create init.n init.n (fun i j -> fullMatrix.data.[i, j + 1])

            let accepted =
                match init.tByNt.TryGetValue cnf.start with
                | true, mat -> mat.data.[0, init.n]
                | _ -> false

            (result, accepted)

    let parse (freshNonterminal: int -> 'nt) (g: Grammar<'t, 'nt>) (terminals: Terminal<'t> list) : bool =
        parseWithTable freshNonterminal g terminals |> snd

    let parseWithTrace
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : ValiantTraceStep<'nt> list =
        let cnf = Grammar.toCnf freshNonterminal g
        let tokensArr = terminals |> List.map (fun (Terminal t) -> t) |> Array.ofList

        if tokensArr.Length = 0 then
            []
        else
            let init = initValiant cnf tokensArr
            let steps = ResizeArray<ValiantTraceStep<'nt>>()
            compute init 0 init.tableSize (Some steps)
            List.ofSeq steps

    let parseModifiedWithTable
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : Matrix<Set<Nonterminal<'nt>>> * bool =
        let cnf = Grammar.toCnf freshNonterminal g
        let tokensArr = terminals |> List.map (fun (Terminal t) -> t) |> Array.ofList
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

        if tokensArr.Length = 0 then
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
                | true, mat -> mat.data.[0, n]
                | _ -> false

            (result, accepted)

    let parseModified (freshNonterminal: int -> 'nt) (g: Grammar<'t, 'nt>) (terminals: Terminal<'t> list) : bool =
        parseModifiedWithTable freshNonterminal g terminals |> snd

    let parseModifiedWithTrace
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : ModifiedValiantTraceStep<'nt> list =
        let cnf = Grammar.toCnf freshNonterminal g
        let tokensArr = terminals |> List.map (fun (Terminal t) -> t) |> Array.ofList
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

        if tokensArr.Length = 0 then
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
