namespace FLPQ.Languages

open FLPQ.LinearAlgebra

module Valiant =

    type Submatrix = { Row: int; Col: int; Size: int }

    [<Struct>]
    type ValiantTraceStep<'nt when 'nt: comparison> =
        { Table: ParsingTable<'nt>
          Target: Submatrix
          Multiplied: (Submatrix * Submatrix) list
          ChangedCells: (int * int) list }

    [<Struct>]
    type ModifiedValiantTraceStep<'nt when 'nt: comparison> =
        | LayerForward of table: ParsingTable<'nt> * layerSize: int * submatrices: Submatrix list
        | LayerBackward of
            table: ParsingTable<'nt> *
            layerSize: int *
            submatrices: Submatrix list *
            ChangedCells: (int * int) list

    [<Struct>]
    type ValiantSppfTraceStep<'nt when 'nt: comparison> =
        { Table: SppfParsingTable<'nt>
          Target: Submatrix
          Multiplied: (Submatrix * Submatrix) list
          ChangedCells: (int * int) list }

    [<Struct>]
    type ModifiedValiantSppfTraceStep<'nt when 'nt: comparison> =
        | LayerForwardSppf of table: SppfParsingTable<'nt> * layerSize: int * submatrices: Submatrix list
        | LayerBackwardSppf of
            table: SppfParsingTable<'nt> *
            layerSize: int *
            submatrices: Submatrix list *
            ChangedCells: (int * int) list

    [<Struct>]
    type private SubmatrixTask =
        { Submatrix: Submatrix
          Left: Submatrix
          Right: Submatrix }

    type private InitData<'t, 'nt when 't: comparison and 'nt: comparison> =
        { Table: SppfParsingTable<'nt>
          TokensArr: 't[]
          TableSize: int
          N: int
          BinaryRules: (Nonterminal<'nt> * BinaryPair<'nt> * int) list
          TerminalRules: Map<'t, (Nonterminal<'nt> * int) list> }

    let private bottomSubmatrix (m: Submatrix) : Submatrix =
        let half = m.Size / 2

        { Row = m.Row
          Col = m.Col
          Size = half }

    let private leftSubmatrix (m: Submatrix) : Submatrix =
        let half = m.Size / 2

        { Row = m.Row - half
          Col = m.Col
          Size = half }

    let private rightSubmatrix (m: Submatrix) : Submatrix =
        let half = m.Size / 2

        { Row = m.Row
          Col = m.Col + half
          Size = half }

    let private topSubmatrix (m: Submatrix) : Submatrix =
        let half = m.Size / 2

        { Row = m.Row - half
          Col = m.Col + half
          Size = half }

    let private sshift (m: Submatrix) (di: int) (dj: int) : Submatrix =
        { Row = m.Row + di
          Col = m.Col + dj
          Size = m.Size }

    let private rightGrounded (m: Submatrix) : Submatrix = sshift m (m.Col - m.Row - 1) 0

    let private leftGrounded (m: Submatrix) : Submatrix = sshift m 0 (-(m.Col - m.Row - 1))

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
                  yield { Row = a; Col = b; Size = size } ]

    let private nextPowerOfTwo (n: int) : int =
        let mutable p = 1

        while p < n do
            p <- p * 2

        p

    let private snapshot (table: Matrix<'a>) (n: int) : Matrix<'a> =
        Matrix.create n n (fun ri rj -> table.[ri, rj + 1])

    let private terminalRulesFromGrammar (cnf: Grammar<'t, 'nt>) : Map<'t, (Nonterminal<'nt> * int) list> =
        cnf.Rules
        |> List.indexed
        |> List.choose (fun (idx, r) ->
            match Rhs.toNonEpsilonList r.Rhs with
            | [ Symbol.T(Terminal t) ] -> Some(t, (r.Lhs, idx))
            | _ -> None)
        |> List.groupBy fst
        |> List.map (fun (t, pairs) -> t, pairs |> List.map snd)
        |> Map.ofList

    let private binaryRulesFromGrammar (cnf: Grammar<'t, 'nt>) : (Nonterminal<'nt> * BinaryPair<'nt> * int) list =
        cnf.Rules
        |> List.indexed
        |> List.choose (fun (idx, r) ->
            match Rhs.toNonEpsilonList r.Rhs with
            | [ Symbol.N left; Symbol.N right ] -> Some(r.Lhs, { Left = left; Right = right }, idx)
            | _ -> None)

    let private initValiant (cnf: Grammar<'t, 'nt>) (tokensArr: 't[]) : InitData<'t, 'nt> =
        let n = tokensArr.Length
        let paddedN = nextPowerOfTwo (n + 1) - 1
        let tableSize = paddedN + 1

        let terminalRules = terminalRulesFromGrammar cnf
        let binaryRules = binaryRulesFromGrammar cnf

        let table = Matrix.init tableSize tableSize Set.empty

        for i in 0 .. tokensArr.Length - 1 do
            match Map.tryFind tokensArr.[i] terminalRules with
            | Some pairs ->
                table.[i, i + 1] <-
                    pairs
                    |> List.map (fun (nt, prodIdx) ->
                        { Nt = nt
                          SplitPoint = i
                          ProdIdx = prodIdx })
                    |> Set.ofList
            | None -> ()

        { Table = table
          TokensArr = tokensArr
          TableSize = tableSize
          N = n
          BinaryRules = binaryRules
          TerminalRules = terminalRules }

    let private mxmSet
        (binaryRules: (Nonterminal<'nt> * BinaryPair<'nt> * int) list)
        (a: Matrix<Set<SppfParsingEntry<'nt>>>)
        (b: Matrix<Set<SppfParsingEntry<'nt>>>)
        : Matrix<Set<SppfParsingEntry<'nt>>> =
        Matrix.mxmi
            (fun _ _ acc newSet -> Set.union acc newSet)
            (fun i k j leftSet rightSet ->
                if Set.isEmpty leftSet || Set.isEmpty rightSet then
                    Set.empty
                else
                    binaryRules
                    |> List.choose (fun (lhs, pair, prodIdx) ->
                        let hasLeft = leftSet |> Set.exists (fun entry -> entry.Nt = pair.Left)

                        let hasRight = rightSet |> Set.exists (fun entry -> entry.Nt = pair.Right)

                        if hasLeft && hasRight then
                            Some
                                { Nt = lhs
                                  SplitPoint = k
                                  ProdIdx = prodIdx }
                        else
                            None)
                    |> Set.ofList)
            Set.empty
            a
            b

    let private writeSliceUnion
        (target: SppfParsingTable<'nt>)
        (m: Submatrix)
        (slice: Matrix<Set<SppfParsingEntry<'nt>>>)
        : unit =
        for i in 0 .. m.Size - 1 do
            for j in 0 .. m.Size - 1 do
                let toAdd = slice.[i, j]

                if not (Set.isEmpty toAdd) then
                    let ti = m.Row - m.Size + 1 + i
                    let tj = m.Col + j
                    let existing = target.[ti, tj]
                    target.[ti, tj] <- Set.union existing toAdd

    let private copyFullTable (table: SppfParsingTable<'nt>) (tableSize: int) : SppfParsingTable<'nt> =
        Matrix.create tableSize tableSize (fun i j -> table.[i, j])

    let private diffCells
        (before: Matrix<Set<SppfParsingEntry<'nt>>>)
        (after: Matrix<Set<SppfParsingEntry<'nt>>>)
        (m: Submatrix)
        : (int * int) list =
        let size = m.Size

        [ for i in 0 .. size - 1 do
              for j in 0 .. size - 1 do
                  let oldSet = before.[i, j]
                  let newSet = after.[i, j]

                  if Set.difference newSet oldSet |> (not << Set.isEmpty) then
                      yield (m.Row - m.Size + 1 + i, m.Col + j) ]

    let private extractSlice (table: SppfParsingTable<'nt>) (m: Submatrix) : Matrix<Set<SppfParsingEntry<'nt>>> =
        Matrix.create m.Size m.Size (fun i j -> table.[m.Row - m.Size + 1 + i, m.Col + j])

    let private doMultiplications
        (init: InitData<'t, 'nt>)
        (table: SppfParsingTable<'nt>)
        (tasks: SubmatrixTask list)
        (traceAcc: ResizeArray<ValiantSppfTraceStep<'nt>> option)
        : unit =
        if List.isEmpty init.BinaryRules then
            ()
        else
            for task in tasks do
                let mTarget = task.Submatrix
                let m1 = task.Left
                let m2 = task.Right
                let beforeSnapshot = extractSlice table mTarget

                let leftSlice = extractSlice table m1
                let rightSlice = extractSlice table m2

                let product = mxmSet init.BinaryRules leftSlice rightSlice

                let shift = m1.Col - 1

                let adjustedProduct =
                    Matrix.map
                        (fun cell ->
                            cell
                            |> Set.map (fun entry ->
                                { entry with
                                    SplitPoint = shift + entry.SplitPoint }))
                        product

                writeSliceUnion table mTarget adjustedProduct

                match traceAcc with
                | Some steps ->
                    let afterSnapshot = extractSlice table mTarget

                    steps.Add(
                        { Table = copyFullTable table init.TableSize
                          Target = mTarget
                          Multiplied = [ (m1, m2) ]
                          ChangedCells = diffCells beforeSnapshot afterSnapshot mTarget }
                    )
                | None -> ()

    let rec private complete
        (init: InitData<'t, 'nt>)
        (table: SppfParsingTable<'nt>)
        (m: Submatrix)
        (traceAcc: ResizeArray<ValiantSppfTraceStep<'nt>> option)
        : unit =
        if m.Size = 1 then
            let i = m.Row - m.Size + 1
            let j = m.Col

            if i + 1 = j && i < init.TokensArr.Length then
                let ch = init.TokensArr.[i]

                let existing = table.[i, j]

                match Map.tryFind ch init.TerminalRules with
                | Some pairs ->
                    table.[i, j] <-
                        Set.union
                            existing
                            (pairs
                             |> List.map (fun (nt, prodIdx) ->
                                 { Nt = nt
                                   SplitPoint = i
                                   ProdIdx = prodIdx })
                             |> Set.ofList)
                | None -> ()

            ()
        else
            let b = bottomSubmatrix m
            let l = leftSubmatrix m
            let r = rightSubmatrix m
            let te = topSubmatrix m

            complete init table b traceAcc

            doMultiplications
                init
                table
                [ { Submatrix = l
                    Left = leftGrounded l
                    Right = b } ]
                traceAcc

            complete init table l traceAcc

            doMultiplications
                init
                table
                [ { Submatrix = r
                    Left = b
                    Right = rightGrounded r } ]
                traceAcc

            complete init table r traceAcc

            doMultiplications
                init
                table
                [ { Submatrix = te
                    Left = leftGrounded te
                    Right = r } ]
                traceAcc

            doMultiplications
                init
                table
                [ { Submatrix = te
                    Left = l
                    Right = rightGrounded te } ]
                traceAcc

            complete init table te traceAcc

    and private compute
        (init: InitData<'t, 'nt>)
        (table: SppfParsingTable<'nt>)
        (i: int)
        (j: int)
        (traceAcc: ResizeArray<ValiantSppfTraceStep<'nt>> option)
        : unit =
        if j - i >= 4 then
            let mid = (i + j) / 2
            compute init table i mid traceAcc
            compute init table mid j traceAcc

        let a = (i + j) / 2 - 1
        let b = (i + j) / 2
        let size = (j - i) / 2

        let m = { Row = a; Col = b; Size = size }
        complete init table m traceAcc

    let rec private completeLayerModified
        (init: InitData<'t, 'nt>)
        (table: SppfParsingTable<'nt>)
        (mList: Submatrix list)
        : unit =
        match mList with
        | [] -> ()
        | first :: _ ->
            if first.Size = 1 then
                for m in mList do
                    let i = m.Row - m.Size + 1
                    let j = m.Col

                    if i + 1 = j && i < init.TokensArr.Length then
                        let ch = init.TokensArr.[i]

                        let existing = table.[i, j]

                        match Map.tryFind ch init.TerminalRules with
                        | Some pairs ->
                            table.[i, j] <-
                                Set.union
                                    existing
                                    (pairs
                                     |> List.map (fun (nt, prodIdx) ->
                                         { Nt = nt
                                           SplitPoint = i
                                           ProdIdx = prodIdx })
                                     |> Set.ofList)
                        | None -> ()
            else
                let bottomLayer = mList |> List.map bottomSubmatrix
                completeLayerModified init table bottomLayer
                completeVLayerModified init table mList

    and private completeVLayerModified
        (init: InitData<'t, 'nt>)
        (table: SppfParsingTable<'nt>)
        (mList: Submatrix list)
        : unit =
        let leftSubLayer = mList |> List.map leftSubmatrix
        let rightSubLayer = mList |> List.map rightSubmatrix
        let topSubLayer = mList |> List.map topSubmatrix

        let firstTasks =
            [ for m in leftSubLayer do
                  yield
                      { Submatrix = m
                        Left = leftGrounded m
                        Right = rightNeighbor m }
              for m in rightSubLayer do
                  yield
                      { Submatrix = m
                        Left = leftNeighbor m
                        Right = rightGrounded m } ]

        doMultiplications init table firstTasks None
        completeLayerModified init table (leftSubLayer @ rightSubLayer)

        let secondTasks =
            [ for m in topSubLayer do
                  yield
                      { Submatrix = m
                        Left = leftGrounded m
                        Right = rightNeighbor m } ]

        doMultiplications init table secondTasks None

        let thirdTasks =
            [ for m in topSubLayer do
                  yield
                      { Submatrix = m
                        Left = leftNeighbor m
                        Right = rightGrounded m } ]

        doMultiplications init table thirdTasks None
        completeLayerModified init table topSubLayer

    /// Run Valiant and return an enriched parsing table with SPPF construction data.
    let parseWithSppfInfo
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : SppfParsingTable<'nt> =
        let cnf = Grammar.toCnf freshNonterminal g
        let tokensArr = terminals |> List.map (fun (Terminal t) -> t) |> Array.ofList

        if tokensArr.Length = 0 then
            Matrix.init 0 0 Set.empty
        else
            let init = initValiant cnf tokensArr

            let table =
                Matrix.create init.TableSize init.TableSize (fun i j -> init.Table.[i, j])

            compute init table 0 init.TableSize None
            snapshot table init.N

    /// Run Valiant and return the enriched parsing table with acceptance status.
    let parseWithSppfTable
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : SppfParsingTable<'nt> * bool =
        let cnf = Grammar.toCnf freshNonterminal g
        let tokensArr = terminals |> List.map (fun (Terminal t) -> t) |> Array.ofList

        if tokensArr.Length = 0 then
            let epsAccepted = Grammar.isEpsilonAccepted cnf
            let emptyResult = Matrix.init 0 0 Set.empty
            (emptyResult, epsAccepted)
        else
            let init = initValiant cnf tokensArr

            let table =
                Matrix.create init.TableSize init.TableSize (fun i j -> init.Table.[i, j])

            compute init table 0 init.TableSize None
            let finalTable = snapshot table init.N

            let accepted =
                Set.exists (fun entry -> entry.Nt = cnf.Start) finalTable.[0, Matrix.cols finalTable - 1]

            (finalTable, accepted)

    /// Run Valiant with SPPF data and return the sequence of trace steps.
    let parseWithSppfTrace
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : ValiantSppfTraceStep<'nt> list =
        let cnf = Grammar.toCnf freshNonterminal g
        let tokensArr = terminals |> List.map (fun (Terminal t) -> t) |> Array.ofList

        if tokensArr.Length = 0 then
            []
        else
            let init = initValiant cnf tokensArr

            let table =
                Matrix.create init.TableSize init.TableSize (fun i j -> init.Table.[i, j])

            let steps = ResizeArray<ValiantSppfTraceStep<'nt>>()
            compute init table 0 init.TableSize (Some steps)
            List.ofSeq steps

    /// Run modified Valiant and return an enriched parsing table with SPPF construction data.
    let parseModifiedWithSppfInfo
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : SppfParsingTable<'nt> =
        let cnf = Grammar.toCnf freshNonterminal g
        let tokensArr = terminals |> List.map (fun (Terminal t) -> t) |> Array.ofList

        if tokensArr.Length = 0 then
            Matrix.init 0 0 Set.empty
        else
            let init = initValiant cnf tokensArr
            let tableSize = init.TableSize
            let table = Matrix.create tableSize tableSize (fun i j -> init.Table.[i, j])
            let n = init.N

            let maxLayer = int (System.Math.Log(float tableSize, 2.0))

            for layer in 1..maxLayer do
                let layerSubmatrices = constructLayer layer tableSize

                if not (List.isEmpty layerSubmatrices) then
                    completeLayerModified init table layerSubmatrices

            snapshot table n

    /// Run modified Valiant and return the enriched parsing table with acceptance status.
    let parseModifiedWithSppfTable
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : SppfParsingTable<'nt> * bool =
        let cnf = Grammar.toCnf freshNonterminal g

        if terminals.IsEmpty then
            let epsAccepted = Grammar.isEpsilonAccepted cnf
            let emptyResult = Matrix.init 0 0 Set.empty
            (emptyResult, epsAccepted)
        else
            let finalTable = parseModifiedWithSppfInfo freshNonterminal g terminals

            let accepted =
                Set.exists (fun entry -> entry.Nt = cnf.Start) finalTable.[0, Matrix.cols finalTable - 1]

            (finalTable, accepted)

    /// Run modified Valiant with SPPF data and return the sequence of trace steps.
    let parseModifiedWithSppfTrace
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : ModifiedValiantSppfTraceStep<'nt> list =
        let cnf = Grammar.toCnf freshNonterminal g
        let tokensArr = terminals |> List.map (fun (Terminal t) -> t) |> Array.ofList

        if tokensArr.Length = 0 then
            []
        else
            let init = initValiant cnf tokensArr
            let tableSize = init.TableSize
            let table = Matrix.create tableSize tableSize (fun i j -> init.Table.[i, j])
            let n = init.N

            let maxLayer = int (System.Math.Log(float tableSize, 2.0))
            let mutable steps = []

            for layer in 1..maxLayer do
                let layerSubmatrices = constructLayer layer tableSize

                if not (List.isEmpty layerSubmatrices) then
                    steps <- LayerForwardSppf(snapshot table n, 1 <<< layer, layerSubmatrices) :: steps

                    completeLayerModified init table layerSubmatrices

                    steps <- LayerBackwardSppf(snapshot table n, 1 <<< layer, layerSubmatrices, []) :: steps

            if List.isEmpty steps then
                steps <- LayerBackwardSppf(snapshot table n, 1, [], []) :: steps

            List.rev steps

    let private sppfTableToNtTable (sppfTable: SppfParsingTable<'nt>) : ParsingTable<'nt> =
        Matrix.create (Matrix.rows sppfTable) (Matrix.cols sppfTable) (fun i j ->
            sppfTable.[i, j] |> Set.map (fun entry -> entry.Nt))

    let private sppfTraceStepToNtTraceStep (step: ValiantSppfTraceStep<'nt>) : ValiantTraceStep<'nt> =
        { Table = sppfTableToNtTable step.Table
          Target = step.Target
          Multiplied = step.Multiplied
          ChangedCells = step.ChangedCells }

    let private sppfModifiedTraceStepToNtTraceStep
        (step: ModifiedValiantSppfTraceStep<'nt>)
        : ModifiedValiantTraceStep<'nt> =
        match step with
        | LayerForwardSppf(table, layerSize, submatrices) ->
            LayerForward(sppfTableToNtTable table, layerSize, submatrices)
        | LayerBackwardSppf(table, layerSize, submatrices, changedCells) ->
            LayerBackward(sppfTableToNtTable table, layerSize, submatrices, changedCells)

    let parse (freshNonterminal: int -> 'nt) (g: Grammar<'t, 'nt>) (terminals: Terminal<'t> list) : bool =
        parseWithSppfTable freshNonterminal g terminals |> snd

    let parseWithTable
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : ParsingTable<'nt> * bool =
        let sppfTable, accepted = parseWithSppfTable freshNonterminal g terminals
        let ntTable = sppfTableToNtTable sppfTable
        (ntTable, accepted)

    let parseWithTrace
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : ValiantTraceStep<'nt> list =
        parseWithSppfTrace freshNonterminal g terminals
        |> List.map sppfTraceStepToNtTraceStep

    let parseModified (freshNonterminal: int -> 'nt) (g: Grammar<'t, 'nt>) (terminals: Terminal<'t> list) : bool =
        parseModifiedWithSppfTable freshNonterminal g terminals |> snd

    let parseModifiedWithTable
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : ParsingTable<'nt> * bool =
        let sppfTable, accepted = parseModifiedWithSppfTable freshNonterminal g terminals
        let ntTable = sppfTableToNtTable sppfTable
        (ntTable, accepted)

    let parseModifiedWithTrace
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : ModifiedValiantTraceStep<'nt> list =
        parseModifiedWithSppfTrace freshNonterminal g terminals
        |> List.map sppfModifiedTraceStepToNtTraceStep
