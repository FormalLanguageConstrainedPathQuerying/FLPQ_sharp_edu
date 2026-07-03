namespace FLPQ.Languages

/// Visualization step shared by LL and LR parser visualizers.
/// treeAndStack is a single combined DOT graph (tree with stack chain overlay).
/// input is a TeX one-row pNiceMatrix with the current position underlined.
[<Struct>]
type VisualizationStep = { treeAndStack: string; input: string }

/// Input state for LL/LR parser step visualization.
[<Struct>]
type StepInput<'t> =
    { tokens: Terminal<'t> list
      position: int }

/// Frame on the unified LL parser stack.
/// LLTree carries a tree node (frontier symbol).
/// LLMarker marks the boundary of a nonterminal expansion with expected child count.
type LLStackFrame<'t, 'nt> =
    | LLTree of DerivationTree<'t, 'nt>
    | LLMarker of Nonterminal<'nt> * int

module LLStackFrame =

    let symbol (frame: LLStackFrame<'t, 'nt>) : Symbol<'t, 'nt> =
        match frame with
        | LLTree tree -> DerivationTree.rootSymbol tree
        | LLMarker(nt, _) -> N nt

    let tree (frame: LLStackFrame<'t, 'nt>) : DerivationTree<'t, 'nt> =
        match frame with
        | LLTree tree -> tree
        | LLMarker(nt, _) -> Node(nt, [])

    let create sym = LLTree(Leaf sym)

/// Data for a single LL parser visualization step.
[<Struct>]
type LLParsingStep<'t, 'nt> =
    { stack: LLStackFrame<'t, 'nt> list
      completed: DerivationTree<'t, 'nt> list
      input: StepInput<'t> }

/// Frame on the unified LR parser stack.
/// Tree nodes are symbols: roots of partial trees are placed in stack and used as symbols.
[<Struct>]
type LRStackFrame<'t, 'nt> =
    | LRState of state: int
    | LRSymbol of tree: DerivationTree<'t, 'nt>

module LRSymbol =

    let symbol frame =
        match frame with
        | LRSymbol tree -> DerivationTree.rootSymbol tree
        | LRState _ -> failwith "LRSymbol.symbol called on LRState"

    let tree frame =
        match frame with
        | LRSymbol tree -> tree
        | LRState _ -> failwith "LRSymbol.tree called on LRState"

/// Data for a single LR parser visualization step.
[<Struct>]
type LRParsingStep<'t, 'nt> =
    { stack: LRStackFrame<'t, 'nt> list
      input: StepInput<'t> }
