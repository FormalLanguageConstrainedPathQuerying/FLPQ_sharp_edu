namespace FLPQ.Languages

open FLPQ.LinearAlgebra

module Valiant =

    type Submatrix = { A: int; B: int; Size: int }

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

        for nt in allNt do
            tByNt.[nt] <- Matrix.init tableSize tableSize false

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

            let result =
                Matrix.create n n (fun i j ->
                    allNt
                    |> List.filter (fun nt ->
                        match tByNt.TryGetValue nt with
                        | true, mat -> mat.data.[i, j + 1]
                        | _ -> false)
                    |> Set.ofList)

            let accepted =
                match tByNt.TryGetValue cnf.start with
                | true, mat -> mat.data.[0, originalN]
                | _ -> false

            (result, accepted)

    let parse (g: Grammar<'t, 'nt>) (tokens: 't list) : bool = parseWithTable g tokens |> snd

    /// Run Valiant with step-by-step trace.
    let parseWithTrace (g: Grammar<'t, 'nt>) (tokens: 't list) : (Matrix<Set<Nonterminal<'nt>>> * string) list =
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

        for nt in allNt do
            tByNt.[nt] <- Matrix.init tableSize tableSize false

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

                    let recomposed =
                        Matrix.create n n (fun ri rj ->
                            allNt
                            |> List.filter (fun nt ->
                                match tByNt.TryGetValue nt with
                                | true, mat -> mat.data.[ri, rj + 1]
                                | _ -> false)
                            |> Set.ofList)

                    let tex =
                        Matrix.toTeX false false (fun s -> if Set.isEmpty s then @"\cdot" else string s) recomposed

                    steps <- (recomposed, tex) :: steps
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

                    let recomposed =
                        Matrix.create n n (fun ri rj ->
                            allNt
                            |> List.filter (fun nt ->
                                match tByNt.TryGetValue nt with
                                | true, mat -> mat.data.[ri, rj + 1]
                                | _ -> false)
                            |> Set.ofList)

                    let tex =
                        Matrix.toTeX false false (fun s -> if Set.isEmpty s then @"\cdot" else string s) recomposed

                    steps <- (recomposed, tex) :: steps

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
