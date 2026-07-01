namespace FLPQ.Languages

/// Visualization step shared by LL and LR parser visualizers.
[<Struct>]
type VisualizationStep =
    { tree: string
      stack: string
      input: string }

/// Input state for LL/LR parser step visualization.
[<Struct>]
type StepInput<'t, 'nt> =
    { tokens: Symbol<'t, 'nt> list
      position: int }

/// Data for a single LL parser visualization step.
[<Struct>]
type LLParsingStep<'t, 'nt> =
    { tree: DerivationTree<'t, 'nt>
      stack: Symbol<'t, 'nt> list
      input: StepInput<'t, 'nt> }

/// Frame on the unified LR parser stack.
[<Struct>]
type LRStackFrame<'t, 'nt> =
    | LRState of int
    | LRSymbol of Symbol<'t, 'nt> * DerivationTree<'t, 'nt>

/// Data for a single LR parser visualization step.
[<Struct>]
type LRParsingStep<'t, 'nt> =
    { tree: DerivationTree<'t, 'nt>
      stack: LRStackFrame<'t, 'nt> list
      input: StepInput<'t, 'nt> }
