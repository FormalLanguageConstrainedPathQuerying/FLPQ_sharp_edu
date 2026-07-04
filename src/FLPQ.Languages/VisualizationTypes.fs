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

/// A stack leaf node in an LL parsing step.
/// Contains the immutable snapshot of the leaf and its path from the tree root.
[<Struct>]
type LLStackLeaf<'t, 'nt> =
    { tree: DerivationTree<'t, 'nt>
      path: int list }

/// Data for a single LL parser visualization step.
[<Struct>]
type LLParsingStep<'t, 'nt> =
    { tree: DerivationTree<'t, 'nt>
      stack: LLStackLeaf<'t, 'nt> list
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
