# Detailed Plan: Task 74 — LL Unified Stack

## Goal

Replace LL parser's dual stacks (separate `Symbol list` for grammar symbols and `DerivationTree list` for tree) with a single unified stack where tree nodes are also symbols, matching the approach used in LR parser (task 49).

## Current State

```fsharp
// Two separate stacks
let rec parseLoop (stack: Symbol list) (pos: int) (treeStack: DerivationTree list)

// LLParsingStep
type LLParsingStep<'t,'nt> = {
    tree: DerivationTree<'t,'nt>
    stack: Symbol<'t,'nt> list          // separate symbol stack
    input: StepInput<'t,'nt> }
```

## Target State

```fsharp
// Single unified stack
let rec parseLoop (stack: LLStackFrame list) (pos: int)

// LLStackFrame — analogous to LR's LRSymbol
type LLStackFrame<'t,'nt> = LLFrame of Symbol<'t,'nt> * DerivationTree<'t,'nt>

// Updated step type
type LLParsingStep<'t,'nt> = {
    tree: DerivationTree<'t,'nt>
    stack: LLStackFrame<'t,'nt> list    // unified symbol+tree stack
    input: StepInput<'t,'nt> }
```

## Key Design Decision

The tree node IS the symbol. Each `LLFrame(sym, tree)` carries both a symbol (for parsing decisions) and a derivation tree node. Tree structure: flat `Node(start, allLeaves)` — same as current approach. The stack represents the tree frontier (current leaves of the partial tree).

## Changes

### 1. `VisualizationTypes.fs` — Add LLStackFrame, update LLParsingStep

- Add `LLStackFrame` struct type: `LLFrame of Symbol * DerivationTree`
- Add `LLStackFrame` active pattern helpers if needed:
  - `LLStackFrame.symbol: LLStackFrame<'t,'nt> -> Symbol<'t,'nt>`
  - `LLStackFrame.tree: LLStackFrame<'t,'nt> -> DerivationTree<'t,'nt>`
  - `LLStackFrame.create: Symbol<'t,'nt> -> LLStackFrame<'t,'nt>` (creates with Leaf(sym))
- Change `LLParsingStep.stack` from `Symbol list` to `LLStackFrame list`

### 2. `LLParser.fs` — Rewrite parser to use unified stack

Current algorithm (dual stack):
```
stack = [N(S)]
treeStack = []
---
Terminal match: pop sym from stack, push Leaf(T(t)) to treeStack
Epsilon: pop from stack, push Leaf(Eps) to treeStack
Nonterminal: pop N(nt), push RHS symbols to stack (treeStack unchanged)
Final tree: Node(start, treeStack)
```

New algorithm (unified stack):
```
stack = [LLFrame(N(S), Node(S, []))]
---
Terminal match T(t): match input, pop LLFrame(T(t), Leaf(T(t))) from stack
Epsilon: pop LLFrame(Epsilon, Leaf(Epsilon)) from stack
Nonterminal N(nt): pop LLFrame(N(nt), _), look up rule, push RHS as LLFrame(sym, Leaf(sym))
---

Wait, there's a subtlety. When we pop a terminal/epsilon from stack, it "disappears." The current code accumulates them in treeStack. Where do they go in the unified approach?

Option A: Keep a separate accumulator for completed tree nodes.
Option B: The stack itself contains the completed nodes (they stay on stack).

Let's use Option A for minimal changes — keep a `completed: DerivationTree list` accumulator:

```fsharp
let rec parseLoop (stack: LLStackFrame list) (pos: int) (completed: DerivationTree list)
```

Terminal match: pop frame, add tree to completed
Epsilon: pop frame, add tree to completed
Nonterminal: pop frame, push RHS as LLFrame(sym, Leaf(sym))
Final tree: Node(start, completed)

For step recording, reconstruct `currentTree` from stack + completed:
```fsharp
let currentTree =
    let stackTrees = stack |> List.map LLStackFrame.tree |> List.rev
    Node(g.start, completed @ stackTrees)
```

Actually, the simpler approach: make LLStackFrame's tree field carry the DerivationTree. For symbols, it's always `Leaf(sym)`. The "current tree" for a step is reconstructed from the stack's tree nodes.

For the current tree at each step:
- The stack has LLFrame(sym, Leaf(sym)) frames
- Completed subtrees are in the completed list
- Current partial tree: Node(start, completed) if stack is empty, or Node(start, completed @ [trees on stack])

Let me refine: for step recording:

```fsharp
let recordStep (stack: LLStackFrame<'t,'nt> list) (pos: int) =
    let stackTrees = stack |> List.map (fun (LLFrame(_, tree)) -> tree) |> List.rev
    let currentTree =
        match stack with
        | [] -> Leaf(Epsilon)  // shouldn't happen during parsing
        | _ -> Node(g.start, stackTrees)  // stack IS the tree frontier
    steps <- { tree = currentTree; stack = stack; input = ... } :: steps
```

The stack IS the tree frontiert. The tree nodes on the stack ARE the tree.

But wait: when we consume a terminal, it's removed from the stack. So the tree "should" show that terminal was consumed. But with the above approach, the tree at the next step wouldn't include the consumed terminal.

In the current code:
```fsharp
let currentTree =
    match treeStack with
    | [t] -> t
    | [] -> Leaf(Epsilon)
    | _ -> Node(g.start, treeStack)
```

So the current tree is built from treeStack (completed subtrees) only. The stack symbols are NOT part of the tree. The tree represents "what has been built so far."

With unified stack: the completed subtrees are no longer in a separate treeStack. Where do they go?

After a terminal is matched, it's removed from the stack. We need to remember it was consumed. That's the purpose of `treeStack` in the current code.

So we DO need an accumulator for completed subtrees in the unified approach. Let me refine:

```fsharp
let rec parseLoop (stack: LLStackFrame list) (pos: int) (completed: DerivationTree list)
```

Terminal match: pop, add LLFrame's tree (Leaf(T(t))) to completed
Epsilon: pop, add LLFrame's tree (Leaf(Epsilon)) to completed

Step recording:
```fsharp
let currentTree =
    let stackTrees = stack |> List.map (fun (LLFrame(_, t)) -> t)
    let allTrees = completed @ (List.rev stackTrees)
    if List.isEmpty allTrees then Leaf(Epsilon)
    elif allTrees.Length = 1 then allTrees.Head
    else Node(g.start, allTrees)
```

Hmm wait, the current approach uses `allTrees` which is `treeStack`. That's exactly `completed` in our new scheme (the stack trees are the unprocessed frontiert, not completed subtrees).

Actually let me re-examine the current code:
```fsharp
| (T _ as sym) :: restStack ->
    parseLoop restStack (pos + 1) (treeStack @ [ Leaf(sym) ])
```

So treeStack = all the Leaf nodes of matched terminals/epsilons so far. They are in left-to-right order (appended at end).

Current tree = Node(g.start, treeStack). This is a flat tree of all matched leaves.

With unified approach:
```fsharp
| LLFrame(T _ as sym, tree) :: restStack ->
    parseLoop restStack (pos + 1) (completed @ [tree])
```

completed = all matched leaf trees so far.

currentTree = Node(g.start, completed)

This gives the same result. The stack on each step = LLFrame(sym, Leaf(sym)) for unprocessed symbols.

### 3. `StepInput` token list type

The input tokens are `Symbol list` but the parser takes `Terminal list`. In `parseWithSteps`, tokens are `Symbol list` (T(Terminal t)). This is unchanged.

### 4. Rewriting `parseWithSteps` — detailed algorithm

```fsharp
let parseWithSteps g table k terminals =
    let tokens = terminals |> List.map (fun (Terminal t) -> T(Terminal t))
    let mutable steps = []
    let mutable stack: LLStackFrame list = [LLFrame(N g.start, Node(g.start, []))]
    let mutable pos = 0
    let mutable completed: DerivationTree list = []
    
    let recordStep () =
        let currentTree = 
            if List.isEmpty completed then Leaf(Epsilon)
            elif completed.Length = 1 then completed.Head
            else Node(g.start, completed)
        steps <- { tree = currentTree; stack = stack; input = { tokens = tokens; position = pos } } :: steps
    
    let rec parseLoop (stack: LLStackFrame list) (pos: int) (completed: DerivationTree list) =
        recordStep()
        match stack with
        | [] -> if pos = tokens.Length then Some(pos, completed) else None
        | LLFrame(T _, tree) :: restStack ->
            if pos < tokens.Length && tokens.[pos] = treeRootSymbol tree then
                parseLoop restStack (pos + 1) (completed @ [tree])
            else None
        | LLFrame(Epsilon, tree) :: restStack ->
            parseLoop restStack pos (completed @ [tree])
        | LLFrame(N nt, _) :: restStack ->
            let la = lookahead tokens pos k
            match Map.tryFind (nt, la) table with
            | Some ruleIdx ->
                let rule = g.rules.[ruleIdx]
                let rhsSymbols = Rhs.toList rule.rhs
                let rhsFrames = rhsSymbols |> List.map (fun sym -> LLFrame(sym, Leaf(sym)))
                parseLoop (rhsFrames @ restStack) pos completed
            | None -> None
        | _ -> None  // Node(nt, _) on stack shouldn't happen in this flat-tree approach
    
    match parseLoop ([LLFrame(N g.start, Node(g.start, []))]) 0 [] with
    | Some(finalPos, leafTrees) when finalPos = tokens.Length ->
        Some(Node(g.start, leafTrees)), List.rev steps
    | _ -> None, List.rev steps
```

## Helper functions

```fsharp
module LLStackFrame =
    let symbol (LLFrame(sym, _)) = sym
    let tree (LLFrame(_, tree)) = tree
    let create sym = LLFrame(sym, Leaf(sym))
```

## Test Updates

1. `LLParserTests.fs` — Tests that access `step.stack` need updating (stack is now `LLStackFrame list`, not `Symbol list`). Most tests don't inspect the stack directly, but some visualization tests might.

2. `LLVisualizerTests.fs` — The `LLStepVisualizer.visualizeSteps` will be updated in Task 76, but for now, make it compile with the new types (temporary compatibility).

## Documentation Updates

1. `docs/visualization-types.md` — Add LLStackFrame description
2. `docs/ll-parser.md` — Update to describe unified stack
