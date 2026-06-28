namespace FLPQ.Languages

/// First_k and follow_k computations for context-free grammars.
/// Parameterized by k (the lookahead length) and a function to convert terminals to strings.
module FirstFollow =

    let private prefix (s: string) (k: int) : string =
        if s.Length <= k then s else s.Substring(0, k)

    let private concatTrunc (k: int) (s1: string) (s2: string) : string = prefix (s1 + s2) k

    let private productTrunc (k: int) (set1: Set<string>) (set2: Set<string>) : Set<string> =
        set1
        |> Set.toSeq
        |> Seq.collect (fun s1 ->
            if s1 = "" then set2 |> Set.toSeq
            elif s1.Length = k then Seq.singleton s1
            else set2 |> Set.toSeq |> Seq.map (concatTrunc k s1))
        |> Set.ofSeq

    let private firstOfSymbols
        (terminalToString: 't -> string)
        (firstMap: Map<Nonterminal<'nt>, Set<string>>)
        (k: int)
        (symbols: Symbol<'t, 'nt> list)
        : Set<string> =
        let rec loop (remaining: Symbol<'t, 'nt> list) : Set<string> =
            match remaining with
            | [] -> set [ "" ]
            | T(Terminal t) :: rest ->
                let tailFirst = loop rest
                let ts = terminalToString t

                if tailFirst = set [ "" ] then
                    set [ prefix ts k ]
                else
                    tailFirst |> Set.map (fun s -> prefix (ts + s) k)
            | N nt :: rest ->
                match Map.tryFind nt firstMap with
                | Some ntFirst ->
                    let tailFirst = loop rest
                    productTrunc k ntFirst tailFirst
                | None -> set [ "" ]

        loop symbols

    let private computeFirstK
        (terminalToString: 't -> string)
        (rules: Rule<'t, 'nt> list)
        (allNt: Nonterminal<'nt> list)
        (k: int)
        : Map<Nonterminal<'nt>, Set<string>> =
        let mutable firstMap =
            allNt
            |> List.map (fun nt ->
                let initials =
                    rules
                    |> List.choose (fun r ->
                        if r.lhs = nt then
                            match r.rhs with
                            | [] -> Some ""
                            | T(Terminal t) :: _ -> Some(prefix (terminalToString t) k)
                            | _ -> None
                        else
                            None)
                    |> Set.ofList

                (nt, initials))
            |> Map.ofList

        let mutable changed = true

        while changed do
            changed <- false

            for nt in allNt do
                let current = Map.find nt firstMap

                let additions =
                    rules
                    |> List.choose (fun r ->
                        if r.lhs = nt && not (r.rhs.IsEmpty) then
                            Some r.rhs
                        else
                            None)
                    |> List.collect (fun rhs -> firstOfSymbols terminalToString firstMap k rhs |> Set.toList)
                    |> Set.ofList
                    |> Set.filter (fun s -> not (Set.contains s current))

                if not (Set.isEmpty additions) then
                    changed <- true
                    firstMap <- Map.add nt (Set.union current additions) firstMap

        firstMap

    /// Compute first_k sets for all nonterminals of a grammar.
    /// first_k(A) = set of terminal strings of length ≤ k that can begin strings derived from A.
    /// The empty string ε is represented as "".
    /// terminalToString converts terminal values to their string representation.
    let firstK (terminalToString: 't -> string) (g: Grammar<'t, 'nt>) (k: int) : Map<Nonterminal<'nt>, Set<string>> =
        let allNt = g.rules |> List.map (fun r -> r.lhs) |> List.distinct

        computeFirstK terminalToString g.rules allNt k

    /// Compute follow_k sets for all nonterminals of a grammar.
    /// follow_k(A) = set of terminal strings of length ≤ k that can appear immediately after A
    /// in some derivation starting from the start symbol.
    /// terminalToString converts terminal values to their string representation.
    let followK (terminalToString: 't -> string) (g: Grammar<'t, 'nt>) (k: int) : Map<Nonterminal<'nt>, Set<string>> =
        let allNt = g.rules |> List.map (fun r -> r.lhs) |> List.distinct

        let firstMap = computeFirstK terminalToString g.rules allNt k

        let mutable followMap =
            allNt
            |> List.map (fun nt -> if nt = g.start then (nt, set [ "" ]) else (nt, Set.empty))
            |> Map.ofList

        let mutable changed = true

        while changed do
            changed <- false

            for rule in g.rules do
                let rhs = rule.rhs

                for idx in 0 .. rhs.Length - 1 do
                    match rhs.[idx] with
                    | N bNt ->
                        let beta = rhs |> List.skip (idx + 1)
                        let firstBeta = firstOfSymbols terminalToString firstMap k beta

                        let currentF = Map.find bNt followMap

                        let additions = firstBeta |> Set.filter (fun s -> not (Set.contains s currentF))

                        if not (Set.isEmpty additions) then
                            changed <- true
                            followMap <- Map.add bNt (Set.union currentF additions) followMap

                        if Set.contains "" firstBeta then
                            let aFollow = Map.find rule.lhs followMap

                            let more = aFollow |> Set.filter (fun s -> not (Set.contains s currentF))

                            if not (Set.isEmpty more) then
                                changed <- true
                                followMap <- Map.add bNt (Set.union (Map.find bNt followMap) more) followMap
                    | _ -> ()

        followMap

    /// Compute first_k for a string of symbols (concatenation of first sets).
    let firstKOfString
        (terminalToString: 't -> string)
        (firstMap: Map<Nonterminal<'nt>, Set<string>>)
        (k: int)
        (symbols: Symbol<'t, 'nt> list)
        : Set<string> =
        firstOfSymbols terminalToString firstMap k symbols
