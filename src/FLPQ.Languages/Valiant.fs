namespace FLPQ.Languages

open FLPQ.LinearAlgebra

module Valiant =

    type Submatrix = { row: int; col: int; Size: int }

    [<Struct>]
    type ValiantTraceStep<'nt when 'nt: comparison> =
        { table: ParsingTable<'nt>
          currentSubmatrix: Submatrix option
          layerSize: int
          submatrices: Submatrix list }

    [<Struct>]
    type ModifiedValiantTraceStep<'nt when 'nt: comparison> =
        { table: ParsingTable<'nt>
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
        [ for i in m.row - m.Size + 1 .. m.row do
              for j in m.col .. m.col + m.Size - 1 do
                  yield (i, j) ]

    let private bottomSubmatrix (m: Submatrix) : Submatrix =
        let half = m.Size / 2

        { row = m.row
          col = m.col
          Size = half }

    let private leftSubmatrix (m: Submatrix) : Submatrix =
        let half = m.Size / 2

        { row = m.row - half
          col = m.col
          Size = half }

    let private rightSubmatrix (m: Submatrix) : Submatrix =
        let half = m.Size / 2

        { row = m.row
          col = m.col + half
          Size = half }

    let private topSubmatrix (m: Submatrix) : Submatrix =
        let half = m.Size / 2

        { row = m.row - half
          col = m.col + half
          Size = half }

    let private sshift (m: Submatrix) (di: int) (dj: int) : Submatrix =
        { row = m.row + di
          col = m.col + dj
          Size = m.Size }

    let private rightGrounded (m: Submatrix) : Submatrix = sshift m (m.col - m.row - 1) 0

    let private leftGrounded (m: Submatrix) : Submatrix = sshift m 0 (-(m.col - m.row - 1))

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
                  yield { row = a; col = b; Size = size } ]

    let private nextPowerOfTwo (n: int) : int =
        let mutable p = 1

        while p < n do
            p <- p * 2

        p

    let private extractSlice (fullMatrix: Matrix<bool>) (m: Submatrix) : Matrix<bool> =
        Matrix.create m.Size m.Size (fun i j -> Matrix.get fullMatrix (m.row - m.Size + 1 + i) (m.col + j))

    let private writeSlice (target: Matrix<bool>) (m: Submatrix) (slice: Matrix<bool>) : unit =
        for i in 0 .. m.Size - 1 do
            for j in 0 .. m.Size - 1 do
                if Matrix.get slice i j then
                    Matrix.set target (m.row - m.Size + 1 + i) (m.col + j) true

    let private performMultiplications
        (tByNt: System.Collections.Generic.Dictionary<Nonterminal<'nt>, Matrix<bool>>)
        (pByPair: System.Collections.Generic.Dictionary<Nonterminal<'nt> * Nonterminal<'nt>, Matrix<bool>>)
        (tasks: (Submatrix * Submatrix * Submatrix) list)
        (pairs: (Nonterminal<'nt> * Nonterminal<'nt>) list)
        : unit =
        if List.isEmpty pairs then
            ()
        else
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

                    let pairMatrix = pByPair.[pair]
                    writeSlice pairMatrix mTarget product

    let private recomposeStep
        (allNt: Nonterminal<'nt> list)
        (tByNt: System.Collections.Generic.Dictionary<Nonterminal<'nt>, Matrix<bool>>)
        (tableSize: int)
        (n: int)
        (currentSubmatrix: Submatrix option)
        (layerSize: int)
        (submatrices: Submatrix list)
        : ValiantTraceStep<'nt> =
        let decompMap =
            allNt
            |> List.map (fun nt ->
                match tByNt.TryGetValue nt with
                | true, mat -> (nt, mat)
                | _ -> (nt, Matrix.init tableSize tableSize false))
            |> Map.ofList

        let fullMatrix = BooleanDecomposition.recompose decompMap

        let recomposed = Matrix.create n n (fun ri rj -> Matrix.get fullMatrix ri (rj + 1))

        { table = recomposed
          currentSubmatrix = currentSubmatrix
          layerSize = layerSize
          submatrices = submatrices }

    let rec private complete
        (init: InitData<'t, 'nt>)
        (m: Submatrix)
        (traceAcc: ResizeArray<ValiantTraceStep<'nt>> option)
        : unit =
        if m.Size = 1 then
            let i = m.row - m.Size + 1
            let j = m.col

            if i + 1 = j && i < init.tokensArr.Length then
                let ch = init.tokensArr.[i]

                match Map.tryFind ch init.terminalRules with
                | Some nts ->
                    for nt in nts do
                        match init.tByNt.TryGetValue nt with
                        | true, mat -> Matrix.set mat i j true
                        | _ -> ()
                | None -> ()
            else
                for pair in init.pairs do
                    let pairHasValue =
                        match init.pByPair.TryGetValue pair with
                        | true, mat -> Matrix.get mat i j
                        | _ -> false

                    if pairHasValue then
                        for (a, bc) in init.binaryRules do
                            if bc = pair then
                                match init.tByNt.TryGetValue a with
                                | true, mat -> Matrix.set mat i j true
                                | _ -> ()

            match traceAcc with
            | Some steps -> steps.Add(recomposeStep init.allNt init.tByNt init.tableSize init.n (Some m) 1 [])
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
            | Some steps -> steps.Add(recomposeStep init.allNt init.tByNt init.tableSize init.n (Some m) m.Size [])
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

        let m = { row = a; col = b; Size = size }
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
                    let i = m.row - m.Size + 1
                    let j = m.col

                    if i + 1 <> j then
                        for pair in pairs do
                            let pairHasValue =
                                match pByPair.TryGetValue pair with
                                | true, mat -> Matrix.get mat i j
                                | _ -> false

                            if pairHasValue then
                                for (a, bc) in binaryRules do
                                    if bc = pair then
                                        match tByNt.TryGetValue a with
                                        | true, mat -> Matrix.set mat i j true
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
            match Rhs.toNonEpsilonList r.rhs with
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
            match Rhs.toNonEpsilonList r.rhs with
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
                    Matrix.set initMatrix i (i + 1) (Set.add nt (Matrix.get initMatrix i (i + 1)))
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

    let parseWithTable
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : ParsingTable<'nt> * bool =
        let cnf = Grammar.toCnf freshNonterminal g
        let steps = parseWithTrace freshNonterminal g terminals

        if List.isEmpty steps then
            let epsAccepted = Grammar.isEpsilonAccepted cnf
            let emptyResult = Matrix.init 0 0 Set.empty
            (emptyResult, epsAccepted)
        else
            let lastStep = List.last steps

            let accepted =
                Set.contains cnf.start (Matrix.get lastStep.table 0 (Matrix.cols lastStep.table - 1))

            (lastStep.table, accepted)

    let parse (freshNonterminal: int -> 'nt) (g: Grammar<'t, 'nt>) (terminals: Terminal<'t> list) : bool =
        parseWithTable freshNonterminal g terminals |> snd

    let parseModifiedWithTrace
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : ModifiedValiantTraceStep<'nt> list =
        let cnf = Grammar.toCnf freshNonterminal g
        let tokensArr = terminals |> List.map (fun (Terminal t) -> t) |> Array.ofList

        if tokensArr.Length = 0 then
            []
        else
            let init = initValiant cnf tokensArr
            let tByNt = init.tByNt
            let pByPair = init.pByPair
            let allNt = init.allNt
            let tableSize = init.tableSize
            let n = init.n
            let terminalRules = init.terminalRules
            let binaryRules = init.binaryRules
            let pairs = init.pairs

            let recomposeTable () =
                let decompMap =
                    allNt
                    |> List.map (fun nt ->
                        match tByNt.TryGetValue nt with
                        | true, mat -> (nt, mat)
                        | _ -> (nt, Matrix.init tableSize tableSize false))
                    |> Map.ofList

                let fullMatrix = BooleanDecomposition.recompose decompMap
                Matrix.create n n (fun ri rj -> Matrix.get fullMatrix ri (rj + 1))

            let mutable steps = []
            let maxLayer = int (System.Math.Log(float tableSize, 2.0))

            for layer in 1..maxLayer do
                let layerSubmatrices = constructLayer layer tableSize

                if not (List.isEmpty layerSubmatrices) then
                    completeLayerModified tByNt pByPair layerSubmatrices terminalRules binaryRules pairs

                    steps <-
                        { ModifiedValiantTraceStep.table = recomposeTable ()
                          layerSize = 1 <<< layer
                          submatrices = layerSubmatrices }
                        :: steps

            if List.isEmpty steps then
                steps <-
                    { ModifiedValiantTraceStep.table = recomposeTable ()
                      layerSize = 1
                      submatrices = [] }
                    :: steps

            List.rev steps

    let parseModifiedWithTable
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : ParsingTable<'nt> * bool =
        let cnf = Grammar.toCnf freshNonterminal g
        let steps = parseModifiedWithTrace freshNonterminal g terminals

        if List.isEmpty steps then
            let epsAccepted = Grammar.isEpsilonAccepted cnf
            let emptyResult = Matrix.init 0 0 Set.empty
            (emptyResult, epsAccepted)
        else
            let lastStep = List.last steps

            let accepted =
                Set.contains cnf.start (Matrix.get lastStep.table 0 (Matrix.cols lastStep.table - 1))

            (lastStep.table, accepted)

    let parseModified (freshNonterminal: int -> 'nt) (g: Grammar<'t, 'nt>) (terminals: Terminal<'t> list) : bool =
        parseModifiedWithTable freshNonterminal g terminals |> snd
