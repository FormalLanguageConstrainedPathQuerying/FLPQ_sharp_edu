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
        Matrix.create m.Size m.Size (fun i j -> Matrix.get fullMatrix (m.Row - m.Size + 1 + i) (m.Col + j))

    let private writeSliceUnion
        (target: Matrix<Set<Nonterminal<'nt>>>)
        (m: Submatrix)
        (slice: Matrix<Set<Nonterminal<'nt>>>)
        : unit =
        for i in 0 .. m.Size - 1 do
            for j in 0 .. m.Size - 1 do
                let toAdd = Matrix.get slice i j

                if not (Set.isEmpty toAdd) then
                    let ti = m.Row - m.Size + 1 + i
                    let tj = m.Col + j
                    let existing = Matrix.get target ti tj
                    Matrix.set target ti tj (Set.union existing toAdd)

    let private snapshot (table: Matrix<Set<Nonterminal<'nt>>>) (n: int) : ParsingTable<'nt> =
        Matrix.create n n (fun ri rj -> Matrix.get table ri (rj + 1))

    let private copyFullTable (table: Matrix<Set<Nonterminal<'nt>>>) (tableSize: int) : ParsingTable<'nt> =
        Matrix.create tableSize tableSize (fun i j -> Matrix.get table i j)

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
                    let leftCell = Matrix.get a i k
                    let rightCell = Matrix.get b k j

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
                  let oldSet = Matrix.get before i j
                  let newSet = Matrix.get after i j

                  if Set.difference newSet oldSet |> (not << Set.isEmpty) then
                      yield (m.Row - m.Size + 1 + i, m.Col + j) ]

    let private doMultiplications
        (init: InitData<'t, 'nt>)
        (table: Matrix<Set<Nonterminal<'nt>>>)
        (tasks: (Submatrix * Submatrix * Submatrix) list)
        (traceAcc: ResizeArray<ValiantTraceStep<'nt>> option)
        : unit =
        if List.isEmpty init.BinaryRules then
            ()
        else
            for (mTarget, m1, m2) in tasks do
                let before =
                    Matrix.create mTarget.Size mTarget.Size (fun i j ->
                        let ti = mTarget.Row - mTarget.Size + 1 + i
                        let tj = mTarget.Col + j
                        Matrix.get table ti tj)

                let leftSlice = extractSlice table m1
                let rightSlice = extractSlice table m2

                let product = mxmSet init.BinaryRules leftSlice rightSlice
                writeSliceUnion table mTarget product

                let after =
                    Matrix.create mTarget.Size mTarget.Size (fun i j ->
                        let ti = mTarget.Row - mTarget.Size + 1 + i
                        let tj = mTarget.Col + j
                        Matrix.get table ti tj)

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

                let existing = Matrix.get table i j

                match Map.tryFind ch init.TerminalRules with
                | Some nts -> Matrix.set table i j (Set.union existing (Set.ofList nts))
                | None -> ()

            ()
        else
            let b = bottomSubmatrix m
            let l = leftSubmatrix m
            let r = rightSubmatrix m
            let te = topSubmatrix m

            complete init table b traceAcc
            doMultiplications init table [ (l, leftGrounded l, b) ] traceAcc
            complete init table l traceAcc
            doMultiplications init table [ (r, b, rightGrounded r) ] traceAcc
            complete init table r traceAcc
            doMultiplications init table [ (te, leftGrounded te, r) ] traceAcc
            doMultiplications init table [ (te, l, rightGrounded te) ] traceAcc
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

                        let existing = Matrix.get table i j

                        match Map.tryFind ch init.TerminalRules with
                        | Some nts -> Matrix.set table i j (Set.union existing (Set.ofList nts))
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
                  yield (m, leftGrounded m, rightNeighbor m)
              for m in rightSubLayer do
                  yield (m, leftNeighbor m, rightGrounded m) ]

        let collectTaskChanges (tasks: (Submatrix * Submatrix * Submatrix) list) : unit =
            if List.isEmpty init.BinaryRules then
                ()
            else
                for (mTarget, m1, m2) in tasks do
                    let leftSlice = extractSlice table m1
                    let rightSlice = extractSlice table m2

                    let product = mxmSet init.BinaryRules leftSlice rightSlice
                    writeSliceUnion table mTarget product

        collectTaskChanges firstTasks

        completeLayerModified init table (leftSubLayer @ rightSubLayer) traceAcc

        let secondTasks =
            [ for m in topSubLayer do
                  yield (m, leftGrounded m, rightNeighbor m) ]

        collectTaskChanges secondTasks

        let thirdTasks =
            [ for m in topSubLayer do
                  yield (m, leftNeighbor m, rightGrounded m) ]

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
            | Some nts -> Matrix.set table i (i + 1) (Set.ofList nts)
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
                Matrix.create init.TableSize init.TableSize (fun i j -> Matrix.get init.Table i j)

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
                Matrix.create init.TableSize init.TableSize (fun i j -> Matrix.get init.Table i j)

            compute init table 0 init.TableSize None
            let finalTable = snapshot table init.N

            let accepted =
                Set.contains cnf.Start (Matrix.get finalTable 0 (Matrix.cols finalTable - 1))

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
            let table = Matrix.create tableSize tableSize (fun i j -> Matrix.get init.Table i j)
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

            let accepted =
                Set.contains cnf.Start (Matrix.get finalTable 0 (Matrix.cols finalTable - 1))

            (finalTable, accepted)

    let parseModified (freshNonterminal: int -> 'nt) (g: Grammar<'t, 'nt>) (terminals: Terminal<'t> list) : bool =
        parseModifiedWithTable freshNonterminal g terminals |> snd
