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
        { Table: Matrix<Set<Nonterminal<'nt>>>
          TokensArr: 't[]
          TableSize: int
          N: int
          BinaryRules: (Nonterminal<'nt> * BinaryPair<'nt>) list
          TerminalRules: Map<'t, Nonterminal<'nt> list> }

    let private submatrixCells (m: Submatrix) : (int * int) list =
        [ for i in m.Row - m.Size + 1 .. m.Row do
              for j in m.Col .. m.Col + m.Size - 1 do
                  yield (i, j) ]

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

    let private extractSlice (fullMatrix: Matrix<'a>) (m: Submatrix) : Matrix<'a> =
        Matrix.create m.Size m.Size (fun i j -> fullMatrix.[m.Row - m.Size + 1 + i, m.Col + j])

    let private writeSliceUnion
        (target: Matrix<Set<Nonterminal<'nt>>>)
        (m: Submatrix)
        (slice: Matrix<Set<Nonterminal<'nt>>>)
        : unit =
        for i in 0 .. m.Size - 1 do
            for j in 0 .. m.Size - 1 do
                let toAdd = slice.[i, j]

                if not (Set.isEmpty toAdd) then
                    let ti = m.Row - m.Size + 1 + i
                    let tj = m.Col + j
                    let existing = target.[ti, tj]
                    target.[ti, tj] <- Set.union existing toAdd

    let private snapshot (table: Matrix<'a>) (n: int) : Matrix<'a> =
        Matrix.create n n (fun ri rj -> table.[ri, rj + 1])

    let private copyFullTable (table: Matrix<Set<Nonterminal<'nt>>>) (tableSize: int) : ParsingTable<'nt> =
        Matrix.create tableSize tableSize (fun i j -> table.[i, j])

    let private setMult
        (binaryRules: (Nonterminal<'nt> * BinaryPair<'nt>) list)
        (a: Set<Nonterminal<'nt>>)
        (b: Set<Nonterminal<'nt>>)
        : Set<Nonterminal<'nt>> =
        if Set.isEmpty a || Set.isEmpty b then
            Set.empty
        else
            binaryRules
            |> List.choose (fun (lhs, pair) ->
                if Set.contains pair.Left a && Set.contains pair.Right b then
                    Some lhs
                else
                    None)
            |> Set.ofList

    let private mxmSet
        (binaryRules: (Nonterminal<'nt> * BinaryPair<'nt>) list)
        (a: Matrix<Set<Nonterminal<'nt>>>)
        (b: Matrix<Set<Nonterminal<'nt>>>)
        : Matrix<Set<Nonterminal<'nt>>> =
        let rA = Matrix.rows a
        let cB = Matrix.cols b
        let inner = Matrix.cols a

        Matrix.create rA cB (fun i j ->
            [ 0 .. inner - 1 ]
            |> List.fold
                (fun acc k ->
                    let leftCell = a.[i, k]
                    let rightCell = b.[k, j]

                    if Set.isEmpty leftCell || Set.isEmpty rightCell then
                        acc
                    else
                        Set.union acc (setMult binaryRules leftCell rightCell))
                Set.empty)

    let private diffCells
        (before: Matrix<Set<Nonterminal<'nt>>>)
        (after: Matrix<Set<Nonterminal<'nt>>>)
        (m: Submatrix)
        : (int * int) list =
        let size = Matrix.rows before

        [ for i in 0 .. size - 1 do
              for j in 0 .. size - 1 do
                  let oldSet = before.[i, j]
                  let newSet = after.[i, j]

                  if Set.difference newSet oldSet |> (not << Set.isEmpty) then
                      yield (m.Row - m.Size + 1 + i, m.Col + j) ]

    let private doMultiplications
        (init: InitData<'t, 'nt>)
        (table: Matrix<Set<Nonterminal<'nt>>>)
        (tasks: SubmatrixTask list)
        (traceAcc: ResizeArray<ValiantTraceStep<'nt>> option)
        : unit =
        if List.isEmpty init.BinaryRules then
            ()
        else
            for task in tasks do
                let mTarget = task.Submatrix
                let m1 = task.Left
                let m2 = task.Right

                let before =
                    Matrix.create mTarget.Size mTarget.Size (fun i j ->
                        let ti = mTarget.Row - mTarget.Size + 1 + i
                        let tj = mTarget.Col + j
                        table.[ti, tj])

                let leftSlice = extractSlice table m1
                let rightSlice = extractSlice table m2

                let product = mxmSet init.BinaryRules leftSlice rightSlice
                writeSliceUnion table mTarget product

                let after =
                    Matrix.create mTarget.Size mTarget.Size (fun i j ->
                        let ti = mTarget.Row - mTarget.Size + 1 + i
                        let tj = mTarget.Col + j
                        table.[ti, tj])

                let changed = diffCells before after mTarget

                match traceAcc with
                | Some steps ->
                    steps.Add(
                        { Table = copyFullTable table init.TableSize
                          Target = mTarget
                          Multiplied = [ (m1, m2) ]
                          ChangedCells = changed }
                    )
                | None -> ()

    let rec private complete
        (init: InitData<'t, 'nt>)
        (table: Matrix<Set<Nonterminal<'nt>>>)
        (m: Submatrix)
        (traceAcc: ResizeArray<ValiantTraceStep<'nt>> option)
        : unit =
        if m.Size = 1 then
            let i = m.Row - m.Size + 1
            let j = m.Col

            if i + 1 = j && i < init.TokensArr.Length then
                let ch = init.TokensArr.[i]

                let existing = table.[i, j]

                match Map.tryFind ch init.TerminalRules with
                | Some nts -> table.[i, j] <- Set.union existing (Set.ofList nts)
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
        (table: Matrix<Set<Nonterminal<'nt>>>)
        (i: int)
        (j: int)
        (traceAcc: ResizeArray<ValiantTraceStep<'nt>> option)
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
        (table: Matrix<Set<Nonterminal<'nt>>>)
        (mList: Submatrix list)
        (traceAcc: ResizeArray<ModifiedValiantTraceStep<'nt>> option)
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
                        | Some nts -> table.[i, j] <- Set.union existing (Set.ofList nts)
                        | None -> ()
            else
                let bottomLayer = mList |> List.map bottomSubmatrix
                completeLayerModified init table bottomLayer traceAcc
                completeVLayerModified init table mList traceAcc

    and private completeVLayerModified
        (init: InitData<'t, 'nt>)
        (table: Matrix<Set<Nonterminal<'nt>>>)
        (mList: Submatrix list)
        (traceAcc: ResizeArray<ModifiedValiantTraceStep<'nt>> option)
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

        let collectTaskChanges (tasks: SubmatrixTask list) : unit =
            if List.isEmpty init.BinaryRules then
                ()
            else
                for task in tasks do
                    let mTarget = task.Submatrix
                    let m1 = task.Left
                    let m2 = task.Right
                    let leftSlice = extractSlice table m1
                    let rightSlice = extractSlice table m2

                    let product = mxmSet init.BinaryRules leftSlice rightSlice
                    writeSliceUnion table mTarget product

        collectTaskChanges firstTasks

        completeLayerModified init table (leftSubLayer @ rightSubLayer) traceAcc

        let secondTasks =
            [ for m in topSubLayer do
                  yield
                      { Submatrix = m
                        Left = leftGrounded m
                        Right = rightNeighbor m } ]

        collectTaskChanges secondTasks

        let thirdTasks =
            [ for m in topSubLayer do
                  yield
                      { Submatrix = m
                        Left = leftNeighbor m
                        Right = rightGrounded m } ]

        collectTaskChanges thirdTasks

        completeLayerModified init table topSubLayer traceAcc

    let private terminalRulesFromGrammar (cnf: Grammar<'t, 'nt>) : Map<'t, Nonterminal<'nt> list> =
        cnf.Rules
        |> List.choose (fun r ->
            match Rhs.toNonEpsilonList r.Rhs with
            | [ Symbol.T(Terminal t) ] -> Some(t, r.Lhs)
            | _ -> None)
        |> List.groupBy fst
        |> List.map (fun (t, pairs) -> t, pairs |> List.map snd)
        |> Map.ofList

    let private binaryRulesFromGrammar (cnf: Grammar<'t, 'nt>) : (Nonterminal<'nt> * BinaryPair<'nt>) list =
        cnf.Rules
        |> List.choose (fun r ->
            match Rhs.toNonEpsilonList r.Rhs with
            | [ Symbol.N left; Symbol.N right ] -> Some(r.Lhs, { Left = left; Right = right })
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
            | Some nts -> table.[i, i + 1] <- Set.ofList nts
            | None -> ()

        { Table = table
          TokensArr = tokensArr
          TableSize = tableSize
          N = n
          BinaryRules = binaryRules
          TerminalRules = terminalRules }

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

            let table =
                Matrix.create init.TableSize init.TableSize (fun i j -> init.Table.[i, j])

            let steps = ResizeArray<ValiantTraceStep<'nt>>()
            compute init table 0 init.TableSize (Some steps)
            List.ofSeq steps

    let parseWithTable
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : ParsingTable<'nt> * bool =
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

            let accepted = Set.contains cnf.Start finalTable.[0, Matrix.cols finalTable - 1]

            (finalTable, accepted)

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
            let tableSize = init.TableSize
            let table = Matrix.create tableSize tableSize (fun i j -> init.Table.[i, j])
            let n = init.N

            let maxLayer = int (System.Math.Log(float tableSize, 2.0))

            let mutable steps: ModifiedValiantTraceStep<'nt> list = []

            for layer in 1..maxLayer do
                let layerSubmatrices = constructLayer layer tableSize

                if not (List.isEmpty layerSubmatrices) then
                    steps <- LayerForward(snapshot table n, 1 <<< layer, layerSubmatrices) :: steps

                    completeLayerModified init table layerSubmatrices None

                    steps <- LayerBackward(snapshot table n, 1 <<< layer, layerSubmatrices, []) :: steps

            if List.isEmpty steps then
                steps <- LayerBackward(snapshot table n, 1, [], []) :: steps

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

            let finalTable =
                match lastStep with
                | LayerForward(table, _, _) -> table
                | LayerBackward(table, _, _, _) -> table

            let accepted = Set.contains cnf.Start finalTable.[0, Matrix.cols finalTable - 1]

            (finalTable, accepted)

    let parseModified (freshNonterminal: int -> 'nt) (g: Grammar<'t, 'nt>) (terminals: Terminal<'t> list) : bool =
        parseModifiedWithTable freshNonterminal g terminals |> snd

    let private terminalRulesFromGrammarSppf (cnf: Grammar<'t, 'nt>) : Map<'t, (Nonterminal<'nt> * int) list> =
        cnf.Rules
        |> List.indexed
        |> List.choose (fun (idx, r) ->
            match Rhs.toNonEpsilonList r.Rhs with
            | [ Symbol.T(Terminal t) ] -> Some(t, (r.Lhs, idx))
            | _ -> None)
        |> List.groupBy fst
        |> List.map (fun (t, pairs) -> t, pairs |> List.map snd)
        |> Map.ofList

    let private binaryRulesFromGrammarSppf (cnf: Grammar<'t, 'nt>) : (Nonterminal<'nt> * BinaryPair<'nt> * int) list =
        cnf.Rules
        |> List.indexed
        |> List.choose (fun (idx, r) ->
            match Rhs.toNonEpsilonList r.Rhs with
            | [ Symbol.N left; Symbol.N right ] -> Some(r.Lhs, { Left = left; Right = right }, idx)
            | _ -> None)

    type private InitDataSppf<'t, 'nt when 't: comparison and 'nt: comparison> =
        { Table: SppfParsingTable<'nt>
          TokensArr: 't[]
          TableSize: int
          N: int
          BinaryRules: (Nonterminal<'nt> * BinaryPair<'nt> * int) list
          TerminalRules: Map<'t, (Nonterminal<'nt> * int) list> }

    let private initValiantSppf (cnf: Grammar<'t, 'nt>) (tokensArr: 't[]) : InitDataSppf<'t, 'nt> =
        let n = tokensArr.Length
        let paddedN = nextPowerOfTwo (n + 1) - 1
        let tableSize = paddedN + 1

        let terminalRules = terminalRulesFromGrammarSppf cnf
        let binaryRules = binaryRulesFromGrammarSppf cnf

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

    let private mxmSetSppf
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

    let private writeSliceUnionSppf
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

    let private copyFullTableSppf (table: SppfParsingTable<'nt>) (tableSize: int) : SppfParsingTable<'nt> =
        Matrix.create tableSize tableSize (fun i j -> table.[i, j])

    let private diffCellsSppf
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

    let private extractSliceSppf (table: SppfParsingTable<'nt>) (m: Submatrix) : Matrix<Set<SppfParsingEntry<'nt>>> =
        Matrix.create m.Size m.Size (fun i j -> table.[m.Row - m.Size + 1 + i, m.Col + j])

    let private doMultiplicationsSppf
        (init: InitDataSppf<'t, 'nt>)
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

                let product = mxmSetSppf init.BinaryRules leftSlice rightSlice
                writeSliceUnionSppf table mTarget product

                match traceAcc with
                | Some steps ->
                    let afterSnapshot = extractSlice table mTarget

                    steps.Add(
                        { Table = copyFullTableSppf table init.TableSize
                          Target = mTarget
                          Multiplied = [ (m1, m2) ]
                          ChangedCells = diffCellsSppf beforeSnapshot afterSnapshot mTarget }
                    )
                | None -> ()

    let rec private completeSppf
        (init: InitDataSppf<'t, 'nt>)
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

            completeSppf init table b traceAcc

            doMultiplicationsSppf
                init
                table
                [ { Submatrix = l
                    Left = leftGrounded l
                    Right = b } ]
                traceAcc

            completeSppf init table l traceAcc

            doMultiplicationsSppf
                init
                table
                [ { Submatrix = r
                    Left = b
                    Right = rightGrounded r } ]
                traceAcc

            completeSppf init table r traceAcc

            doMultiplicationsSppf
                init
                table
                [ { Submatrix = te
                    Left = leftGrounded te
                    Right = r } ]
                traceAcc

            doMultiplicationsSppf
                init
                table
                [ { Submatrix = te
                    Left = l
                    Right = rightGrounded te } ]
                traceAcc

            completeSppf init table te traceAcc

    and private computeSppf
        (init: InitDataSppf<'t, 'nt>)
        (table: SppfParsingTable<'nt>)
        (i: int)
        (j: int)
        (traceAcc: ResizeArray<ValiantSppfTraceStep<'nt>> option)
        : unit =
        if j - i >= 4 then
            let mid = (i + j) / 2
            computeSppf init table i mid traceAcc
            computeSppf init table mid j traceAcc

        let a = (i + j) / 2 - 1
        let b = (i + j) / 2
        let size = (j - i) / 2

        let m = { Row = a; Col = b; Size = size }
        completeSppf init table m traceAcc

    let rec private completeLayerModifiedSppf
        (init: InitDataSppf<'t, 'nt>)
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
                completeLayerModifiedSppf init table bottomLayer
                completeVLayerModifiedSppf init table mList

    and private completeVLayerModifiedSppf
        (init: InitDataSppf<'t, 'nt>)
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

        doMultiplicationsSppf init table firstTasks None
        completeLayerModifiedSppf init table (leftSubLayer @ rightSubLayer)

        let secondTasks =
            [ for m in topSubLayer do
                  yield
                      { Submatrix = m
                        Left = leftGrounded m
                        Right = rightNeighbor m } ]

        doMultiplicationsSppf init table secondTasks None

        let thirdTasks =
            [ for m in topSubLayer do
                  yield
                      { Submatrix = m
                        Left = leftNeighbor m
                        Right = rightGrounded m } ]

        doMultiplicationsSppf init table thirdTasks None
        completeLayerModifiedSppf init table topSubLayer

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
            let init = initValiantSppf cnf tokensArr

            let table =
                Matrix.create init.TableSize init.TableSize (fun i j -> init.Table.[i, j])

            computeSppf init table 0 init.TableSize None
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
            let init = initValiantSppf cnf tokensArr

            let table =
                Matrix.create init.TableSize init.TableSize (fun i j -> init.Table.[i, j])

            computeSppf init table 0 init.TableSize None
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
            let init = initValiantSppf cnf tokensArr

            let table =
                Matrix.create init.TableSize init.TableSize (fun i j -> init.Table.[i, j])

            let steps = ResizeArray<ValiantSppfTraceStep<'nt>>()
            computeSppf init table 0 init.TableSize (Some steps)
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
            let init = initValiantSppf cnf tokensArr
            let tableSize = init.TableSize
            let table = Matrix.create tableSize tableSize (fun i j -> init.Table.[i, j])
            let n = init.N

            let maxLayer = int (System.Math.Log(float tableSize, 2.0))

            for layer in 1..maxLayer do
                let layerSubmatrices = constructLayer layer tableSize

                if not (List.isEmpty layerSubmatrices) then
                    completeLayerModifiedSppf init table layerSubmatrices

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
            let init = initValiantSppf cnf tokensArr
            let tableSize = init.TableSize
            let table = Matrix.create tableSize tableSize (fun i j -> init.Table.[i, j])
            let n = init.N

            let maxLayer = int (System.Math.Log(float tableSize, 2.0))
            let mutable steps = []

            for layer in 1..maxLayer do
                let layerSubmatrices = constructLayer layer tableSize

                if not (List.isEmpty layerSubmatrices) then
                    steps <- LayerForwardSppf(snapshot table n, 1 <<< layer, layerSubmatrices) :: steps

                    completeLayerModifiedSppf init table layerSubmatrices

                    steps <- LayerBackwardSppf(snapshot table n, 1 <<< layer, layerSubmatrices, []) :: steps

            if List.isEmpty steps then
                steps <- LayerBackwardSppf(snapshot table n, 1, [], []) :: steps

            List.rev steps
