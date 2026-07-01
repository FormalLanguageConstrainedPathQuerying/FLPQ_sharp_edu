namespace FLPQ.Languages

/// Visualization step shared by LL and LR parser visualizers.
/// treeAndStack is a single combined DOT graph (tree with stack chain overlay).
/// input is a TeX one-row pNiceMatrix with the current position underlined.
[<Struct>]
type VisualizationStep = { treeAndStack: string; input: string }

/// Input state for LL/LR parser step visualization.
[<Struct>]
type StepInput<'t, 'nt> =
    { tokens: Symbol<'t, 'nt> list
      position: int }

/// Frame on the unified LL parser stack.
/// Each frame carries a symbol for parsing decisions and its associated derivation tree node.
/// Tree nodes are symbols: current leaves of the partial tree are placed in stack.
[<Struct>]
type LLStackFrame<'t, 'nt> = LLFrame of Symbol<'t, 'nt> * DerivationTree<'t, 'nt>

module LLStackFrame =

    let symbol (LLFrame(sym, _)) = sym

    let tree (LLFrame(_, tree)) = tree

    let create sym = LLFrame(sym, Leaf sym)

/// Data for a single LL parser visualization step.
[<Struct>]
type LLParsingStep<'t, 'nt> =
    { tree: DerivationTree<'t, 'nt>
      stack: LLStackFrame<'t, 'nt> list
      input: StepInput<'t, 'nt> }

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
    { tree: DerivationTree<'t, 'nt>
      stack: LRStackFrame<'t, 'nt> list
      input: StepInput<'t, 'nt> }
