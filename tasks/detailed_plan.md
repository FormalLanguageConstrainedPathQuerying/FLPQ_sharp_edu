# Detailed Plan: Task 111 — Fix Valiant crash, Deduplicate init, Deduplicate epsilon check

## Part 1: Add `Grammar.isEpsilonAccepted` (deduplicate epsilon check)

### Current state
Same 6-line block appears 4 times:
- `Cyk.fs` line 143 (in `parse`): `cnf.rules |> List.exists (fun r -> r.lhs = cnf.start && Rhs.isEpsilon r.rhs)`
- `Cyk.fs` lines 157-158 (in `parseWithTable`): same
- `Valiant.fs` lines 390-391 (in `parseWithTable`): same
- `Valiant.fs` lines 494-495 (in `parseModifiedWithTable`): same

### Changes
1. Add `Grammar.isEpsilonAccepted` to `Grammar.fs` (public, in `Grammar` module):
   ```fsharp
   let isEpsilonAccepted (cnf: Grammar<'t, 'nt>) : bool =
       cnf.rules |> List.exists (fun r -> r.lhs = cnf.start && Rhs.isEpsilon r.rhs)
   ```
2. Replace all 4 call sites with `Grammar.isEpsilonAccepted cnf`

## Part 2: Fix `Seq.head` crash on grammar without binary rules

### Problem
`Valiant.fs` line 127: `let mat = pByPair.Values |> Seq.head`
When grammar has no binary rules (e.g., `S -> a | b`), `pByPair` is empty, `Seq.head` throws.

### Fix
In `performMultiplications`, skip the multiplication loop entirely when `pairs` is empty. Also safeguard the `pByPair` lookup:
```fsharp
let rec private performMultiplications ... =
    if List.isEmpty pairs then
        ()
    else
        for (mTarget, m1, m2) in tasks do
            for pair in pairs do
                ...
                let pairMatrix =
                    match pByPair.TryGetValue pair with
                    | true, mat -> mat
                    | None ->
                        let mat = pByPair.Values |> Seq.head
                        mat
                ...
```

Actually, the cleaner fix: check if `pairs` is empty at the start of `performMultiplications` and return early:
```fsharp
let private performMultiplications ... =
    if List.isEmpty pairs then ()
    else
        for (mTarget, m1, m2) in tasks do
            for pair in pairs do
                ...
```

This avoids `Seq.head` entirely when there are no pairs.

## Part 3: Deduplicate Valiant init

### Current state
`initValiant` (lines 320-362) and `parseModifiedWithTrace` (lines 411-444) contain the same ~35 lines of initialization logic.

### Changes
Make `parseModifiedWithTrace` call `initValiant` and extract fields:
```fsharp
let parseModifiedWithTrace
    (freshNonterminal: int -> 'nt)
    (g: Grammar<'t, 'nt>)
    (terminals: Terminal<'t> list)
    : ModifiedValiantTraceStep<'nt> list =
    let cnf = Grammar.toCnf freshNonterminal g
    let tokensArr = terminals |> List.map (fun (Terminal t) -> t) |> Array.ofList

    if tokensArr.Length = 0 then
        []
    else
        let init = initValiant cnf tokensArr
        let tByNt = init.tByNt
        let pByPair = init.pByPair
        let allNt = init.allNt
        let tableSize = init.tableSize
        let n = init.n
        let terminalRules = init.terminalRules
        let binaryRules = init.binaryRules
        let pairs = init.pairs

        let recomposeTable () = ...
        let mutable steps = ...
        ...
```

This eliminates the duplicated lines 411-444 while keeping `recomposeTable` (which is different in the modified version since it doesn't need the step-specific recompose logic).

## Part 4: Equivalence verification

After refactoring:
1. All existing Valiant tests must pass
2. Modified Valiant results must be identical to standard Valiant
3. All CYK tests must pass
4. Format code, compile, run full test suite
