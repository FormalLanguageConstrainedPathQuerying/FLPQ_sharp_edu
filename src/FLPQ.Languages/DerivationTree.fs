namespace FLPQ.Languages

/// Derivation tree produced by a parsing algorithm.
/// Leaves carry terminal symbols; internal nodes carry nonterminals and children.
type DerivationTree<'t, 'nt> =
    | Leaf of Terminal<'t>
    | Epsilon
    | Node of Nonterminal<'nt> * DerivationTree<'t, 'nt> list

module DerivationTree =

    /// Collect all leaf terminals from a derivation tree (left-to-right).
    let rec leaves (tree: DerivationTree<'t, 'nt>) : 't list =
        match tree with
        | Leaf(Terminal t) -> [ t ]
        | Epsilon -> []
        | Node(_, children) -> children |> List.collect leaves
