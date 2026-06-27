namespace FLPQ.Core

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
        (tByNt: System.Collections.Generic.Dictionary<Nonterminal<string>, Matrix<bool>>)
        (pByPair: System.Collections.Generic.Dictionary<Nonterminal<string> * Nonterminal<string>, Matrix<bool>>)
        (tasks: (Submatrix * Submatrix * Submatrix) list)
        (pairs: (Nonterminal<string> * Nonterminal<string>) list)
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
                        let rows = leftSlice.rows
                        let cols = rightSlice.cols
                        // full table dimensions match the target's full matrix
                        let mat = pByPair.Values |> Seq.head
                        mat

                writeSlice pairMatrix mTarget product

    let rec private complete
        (tByNt: System.Collections.Generic.Dictionary<Nonterminal<string>, Matrix<bool>>)
        (pByPair: System.Collections.Generic.Dictionary<Nonterminal<string> * Nonterminal<string>, Matrix<bool>>)
        (m: Submatrix)
        (terminalRules: Map<string, Nonterminal<string> list>)
        (binaryRules: (Nonterminal<string> * (Nonterminal<string> * Nonterminal<string>)) list)
        (pairs: (Nonterminal<string> * Nonterminal<string>) list)
        (input: string)
        : unit =
        if m.Size = 1 then
            let i = m.A - m.Size + 1
            let j = m.B

            if i + 1 = j && i < input.Length then
                let ch = input.[i].ToString()

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

            complete tByNt pByPair b terminalRules binaryRules pairs input
            performMultiplications tByNt pByPair [ (l, leftGrounded l, b) ] pairs
            complete tByNt pByPair l terminalRules binaryRules pairs input
            performMultiplications tByNt pByPair [ (r, b, rightGrounded r) ] pairs
            complete tByNt pByPair r terminalRules binaryRules pairs input
            performMultiplications tByNt pByPair [ (t, leftGrounded t, r) ] pairs
            performMultiplications tByNt pByPair [ (t, l, rightGrounded t) ] pairs
            complete tByNt pByPair t terminalRules binaryRules pairs input

    and private compute
        (tByNt: System.Collections.Generic.Dictionary<Nonterminal<string>, Matrix<bool>>)
        (pByPair: System.Collections.Generic.Dictionary<Nonterminal<string> * Nonterminal<string>, Matrix<bool>>)
        (i: int)
        (j: int)
        (terminalRules: Map<string, Nonterminal<string> list>)
        (binaryRules: (Nonterminal<string> * (Nonterminal<string> * Nonterminal<string>)) list)
        (pairs: (Nonterminal<string> * Nonterminal<string>) list)
        (input: string)
        : unit =
        if j - i >= 4 then
            let mid = (i + j) / 2
            compute tByNt pByPair i mid terminalRules binaryRules pairs input
            compute tByNt pByPair mid j terminalRules binaryRules pairs input

        let a = (i + j) / 2 - 1
        let b = (i + j) / 2
        let size = (j - i) / 2

        let m = { A = a; B = b; Size = size }
        complete tByNt pByPair m terminalRules binaryRules pairs input

    /// Parse a string using Valiant's algorithm.
    /// Returns the final T table (Boolean decomposition: map from nonterminal to Boolean matrix)
    /// and a flag indicating whether the start symbol is in T[0,n].
    let parseWithTable (g: Grammar<string, string>) (input: string) : Map<Nonterminal<string>, Matrix<bool>> * bool =
        let cnf = Grammar.toCnf g
        let n = input.Length
        let originalN = n
        let paddedN = nextPowerOfTwo (n + 1) - 1
        let tableSize = paddedN + 1

        let allNt = cnf.rules |> List.map (fun r -> r.lhs) |> List.distinct

        let terminalRules =
            cnf.rules
            |> List.choose (fun r ->
                match r.rhs with
                | [ T(Terminal t) ] -> Some(t, r.lhs)
                | _ -> None)
            |> List.groupBy fst
            |> List.map (fun (t, pairs) -> t, pairs |> List.map snd)
            |> Map.ofList

        let binaryRules =
            cnf.rules
            |> List.choose (fun r ->
                match r.rhs with
                | [ N left; N right ] -> Some(r.lhs, (left, right))
                | _ -> None)

        let pairs = binaryRules |> List.map snd |> List.distinct

        let tByNt =
            System.Collections.Generic.Dictionary<Nonterminal<string>, Matrix<bool>>()

        for nt in allNt do
            tByNt.[nt] <- Matrix.init tableSize tableSize false

        let pByPair =
            System.Collections.Generic.Dictionary<Nonterminal<string> * Nonterminal<string>, Matrix<bool>>()

        for pair in pairs do
            pByPair.[pair] <- Matrix.init tableSize tableSize false

        if input = "" then
            let epsAccepted =
                cnf.rules |> List.exists (fun r -> r.lhs = cnf.start && r.rhs = [])

            let result = tByNt |> Seq.map (fun kv -> (kv.Key, kv.Value)) |> Map.ofSeq
            (result, epsAccepted)
        else
            compute tByNt pByPair 0 tableSize terminalRules binaryRules pairs input

            let result = tByNt |> Seq.map (fun kv -> (kv.Key, kv.Value)) |> Map.ofSeq

            let accepted =
                match tByNt.TryGetValue cnf.start with
                | true, mat -> mat.data.[0, originalN]
                | _ -> false

            (result, accepted)

    /// Check whether a string is accepted by a grammar using Valiant's algorithm.
    let parse (g: Grammar<string, string>) (input: string) : bool = parseWithTable g input |> snd
