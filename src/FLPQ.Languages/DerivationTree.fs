namespace FLPQ.Languages

/// Derivation tree produced by a parsing algorithm.
/// Leaves carry grammar symbols; internal nodes carry nonterminals and children.
type DerivationTree<'t, 'nt> =
    | Leaf of Symbol<'t, 'nt>
    | Node of Nonterminal<'nt> * DerivationTree<'t, 'nt> list

/// Mutable derivation tree node for in-place construction during LL parsing.
/// Children can be added after node creation (when a nonterminal leaf is expanded).
/// Parent pointers enable path computation for visualization.
type MutableTree<'t, 'nt>(sym: Symbol<'t, 'nt>) =
    member val Symbol = sym with get, set
    member val Children: MutableTree<'t, 'nt> list = [] with get, set
    member val Parent: MutableTree<'t, 'nt> option = None with get, set

    member this.ToImmutable() : DerivationTree<'t, 'nt> =
        match this.Symbol with
        | N nt when not (List.isEmpty this.Children) -> Node(nt, this.Children |> List.map (fun c -> c.ToImmutable()))
        | _ -> Leaf this.Symbol

    member this.GetPath() : int list =
        let rec go (n: MutableTree<'t, 'nt>) acc =
            match n.Parent with
            | None -> acc
            | Some parent ->
                let idx = parent.Children |> List.findIndex (fun c -> obj.ReferenceEquals(c, n))

                go parent (idx :: acc)

        go this []

module DerivationTree =

    /// Collect all leaf terminal values from a derivation tree (left-to-right).
    /// Epsilon leaves contribute nothing.
    let rec leaves (tree: DerivationTree<'t, 'nt>) : 't list =
        match tree with
        | Leaf(T(Terminal t)) -> [ t ]
        | Leaf(Epsilon) -> []
        | Leaf(N _) -> []
        | Node(_, children) -> children |> List.collect leaves

    /// Extract the root symbol of a derivation tree.
    let rootSymbol (tree: DerivationTree<'t, 'nt>) : Symbol<'t, 'nt> =
        match tree with
        | Leaf sym -> sym
        | Node(nt, _) -> N nt
