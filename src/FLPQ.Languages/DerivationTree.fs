namespace FLPQ.Languages

/// Derivation tree produced by a parsing algorithm.
/// Leaves carry grammar symbols; internal nodes carry nonterminals and children.
type DerivationTree<'t, 'nt> =
    | Leaf of Symbol<'t, 'nt>
    | Node of Nonterminal<'nt> * DerivationTree<'t, 'nt> list

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
