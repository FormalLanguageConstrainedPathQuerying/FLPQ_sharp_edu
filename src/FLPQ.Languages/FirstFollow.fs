namespace FLPQ.Languages

/// First_k and follow_k computations for context-free grammars.
/// Parameterized by k (the lookahead length).
/// Epsilon is represented as [Epsilon] (a singleton Symbol list).
module FirstFollow =

    let private concat (s1: Symbol<'t, 'nt> list) (s2: Symbol<'t, 'nt> list) : Symbol<'t, 'nt> list =
        match s1, s2 with
        | [ Symbol.Epsilon ], _ -> s2
        | _, [ Symbol.Epsilon ] -> s1
        | _, _ -> s1 @ s2

    let private truncate (lst: Symbol<'t, 'nt> list) (k: int) : Symbol<'t, 'nt> list =
        if k <= 0 then [ Symbol.Epsilon ]
        elif lst.Length <= k then lst
        else lst |> List.take k

    let private concatTrunc (k: int) (s1: Symbol<'t, 'nt> list) (s2: Symbol<'t, 'nt> list) : Symbol<'t, 'nt> list =
        truncate (concat s1 s2) k

    let private productTrunc
        (k: int)
        (set1: Set<Symbol<'t, 'nt> list>)
        (set2: Set<Symbol<'t, 'nt> list>)
        : Set<Symbol<'t, 'nt> list> =
        set1
        |> Set.toSeq
        |> Seq.collect (fun s1 ->
            if s1 = [ Symbol.Epsilon ] then set2 |> Set.toSeq
            elif s1.Length = k then Seq.singleton s1
            else set2 |> Set.toSeq |> Seq.map (concatTrunc k s1))
        |> Set.ofSeq

    let private firstOfSymbols
        (firstMap: Map<Nonterminal<'nt>, Set<Symbol<'t, 'nt> list>>)
        (k: int)
        (symbols: Symbol<'t, 'nt> list)
        : Set<Symbol<'t, 'nt> list> =
        let rec loop (remaining: Symbol<'t, 'nt> list) : Set<Symbol<'t, 'nt> list> =
            match remaining with
            | [] -> set [ [ Symbol.Epsilon ] ]
            | (Symbol.T _ as t) :: rest ->
                let tailFirst = loop rest

                if tailFirst = set [ [ Symbol.Epsilon ] ] then
                    set [ truncate [ t ] k ]
                else
                    tailFirst |> Set.map (fun s -> truncate (t :: s) k)
            | Symbol.Epsilon :: rest -> loop rest
            | Symbol.N nt :: rest ->
                match Map.tryFind nt firstMap with
                | Some ntFirst ->
                    let tailFirst = loop rest
                    productTrunc k ntFirst tailFirst
                | None -> set [ [ Symbol.Epsilon ] ]

        loop symbols

    let private computeFirstK
        (rules: Rule<'t, 'nt> list)
        (allNt: Nonterminal<'nt> list)
        (k: int)
        : Map<Nonterminal<'nt>, Set<Symbol<'t, 'nt> list>> =
        let mutable firstMap =
            allNt
            |> List.map (fun nt ->
                let initials =
                    rules
                    |> List.choose (fun r ->
                        if r.lhs = nt then
                            if Rhs.isEpsilon r.rhs then
                                Some [ Symbol.Epsilon ]
                            else
                                match Rhs.toNonEpsilonList r.rhs with
                                | (Symbol.T _ as t) :: _ -> Some(truncate [ t ] k)
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
                        if r.lhs = nt && not (Rhs.isEpsilon r.rhs) then
                            Some(Rhs.toNonEpsilonList r.rhs)
                        else
                            None)
                    |> List.collect (fun rhs -> firstOfSymbols firstMap k rhs |> Set.toList)
                    |> Set.ofList
                    |> Set.filter (fun s -> not (Set.contains s current))

                if not (Set.isEmpty additions) then
                    changed <- true
                    firstMap <- Map.add nt (Set.union current additions) firstMap

        firstMap

    /// Compute first_k sets for all nonterminals of a grammar.
    /// first_k(A) = set of terminal strings of length ≤ k that can begin strings derived from A.
    /// Epsilon is represented as [Epsilon] (a singleton list).
    let firstK (g: Grammar<'t, 'nt>) (k: int) : Map<Nonterminal<'nt>, Set<Symbol<'t, 'nt> list>> =
        let allNt = g.rules |> List.map (fun r -> r.lhs) |> List.distinct

        computeFirstK g.rules allNt k

    /// Compute follow_k sets for all nonterminals of a grammar.
    /// follow_k(A) = set of terminal strings of length ≤ k that can appear immediately after A
    /// in some derivation starting from the start symbol.
    /// Epsilon is represented as [Epsilon] (a singleton list).
    let followK (g: Grammar<'t, 'nt>) (k: int) : Map<Nonterminal<'nt>, Set<Symbol<'t, 'nt> list>> =
        let allNt = g.rules |> List.map (fun r -> r.lhs) |> List.distinct

        let firstMap = computeFirstK g.rules allNt k

        let mutable followMap =
            allNt
            |> List.map (fun nt ->
                if nt = g.start then
                    (nt, set [ [ Symbol.Epsilon ] ])
                else
                    (nt, Set.empty))
            |> Map.ofList

        let mutable changed = true

        while changed do
            changed <- false

            for rule in g.rules do
                let rhs = Rhs.toNonEpsilonList rule.rhs

                for idx in 0 .. rhs.Length - 1 do
                    match rhs.[idx] with
                    | Symbol.N bNt ->
                        let beta = rhs |> List.skip (idx + 1)
                        let firstBeta = firstOfSymbols firstMap k beta

                        let currentF = Map.find bNt followMap

                        let additions = firstBeta |> Set.filter (fun s -> not (Set.contains s currentF))

                        if not (Set.isEmpty additions) then
                            changed <- true
                            followMap <- Map.add bNt (Set.union currentF additions) followMap

                        if Set.contains [ Symbol.Epsilon ] firstBeta then
                            let aFollow = Map.find rule.lhs followMap

                            let more = aFollow |> Set.filter (fun s -> not (Set.contains s currentF))

                            if not (Set.isEmpty more) then
                                changed <- true
                                followMap <- Map.add bNt (Set.union (Map.find bNt followMap) more) followMap
                    | _ -> ()

        followMap

    /// Compute first_k for a string of symbols (concatenation of first sets).
    let firstKOfString
        (firstMap: Map<Nonterminal<'nt>, Set<Symbol<'t, 'nt> list>>)
        (k: int)
        (symbols: Symbol<'t, 'nt> list)
        : Set<Symbol<'t, 'nt> list> =
        firstOfSymbols firstMap k symbols
