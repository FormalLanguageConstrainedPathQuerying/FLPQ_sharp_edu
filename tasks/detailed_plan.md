# Detailed Plan: Tasks 81--82

## Task 81: Implement automata acceptance algorithm

### Goal

Implement classical automata acceptance for both NFA (with epsilon transitions) and DFA. The algorithm uses a working set of configurations, processing one configuration at a time.

### Design

#### Configuration type

```fsharp
/// Configuration for automaton acceptance: a state index and the current input position.
type Config =
    { state: int
      position: int }
```

This is a simple record type. It belongs in `Automaton.fs` alongside NFA/DFA types since it's fundamental to automaton operations.

#### NFA acceptance algorithm

Pseudo-code:
```
let accept (nfa: NFA<'t,'s>) (input: Terminal<'t> list) : bool =
    let n = List.length input
    let mutable currentConfigs = Set.empty
    for s in nfa.startStates do
        for c in epsilonClosure nfa s do
            currentConfigs <- add {state=c; position=0} currentConfigs
    let visited = mutable set currentConfigs
    
    while currentConfigs is not empty:
        remove one config {state=s; position=p}
        if s in nfa.finalStates and p = n then return true
        if p < n then
            let sym = input[p]
            let targets = move nfa s sym
            for t in targets do
                for ec in epsilonClosure nfa t do
                    let newConfig = {state=ec; position=p+1}
                    if newConfig not in visited then
                        visited <- add newConfig visited
                        currentConfigs <- add newConfig currentConfigs
    
    return false
```

Detailed steps:
1. Extract the bare value from each `Terminal<'t>` to get the actual symbol for `move`.
2. Initialize `currentConfigs` with all start states (and their epsilon closures) at position 0.
3. `visited` tracks all seen configurations to prevent infinite loops from epsilon cycles or loops.
4. Main loop: pick any config from `currentConfigs`. If state is final and position equals input length, accept. Otherwise, if position < input length, transition on the current input symbol, expand epsilon closure of targets, add new configs not yet visited.
5. If `currentConfigs` becomes empty, reject.

#### DFA acceptance algorithm

Simpler — no epsilon closure, single start state:
```
let accept (dfa: DFA<'t,'s>) (input: Terminal<'t> list) : bool =
    let mutable state = dfa.startState
    for symTerm in input do
        let sym = ... // extract value from Terminal
        match move dfa state sym with
        | Some next -> state <- next
        | None -> return false
    state in dfa.finalStates
```

### Files changed

- `src/FLPQ.Languages/Automaton.fs`:
  - Add `Config` type (before NFA/DFA types)
  - Add `Nfa.accept` function
  - Add `Dfa.accept` function

- `tests/FLPQ.Languages.Tests/AutomatonTests.fs`:
  - Add test module for acceptance with 12 test cases from task spec

### Test cases

Test NFA and DFA acceptance for:
1. `re: a+` — NFA: 0 -[a]-> 1 -[a]-> 1; start=0, final={1}. Accepts: a, aa, aaa. Rejects: <empty>
2. `re: a*` — NFA: 0 -[a]-> 0; start=0, final={0}. Accepts: <empty>, a, aa, aaa. Rejects: b
3. `re: (ab)*` — NFA with epsilon. Accepts: <empty>, ab, abab. Rejects: ba, bab, aaa, bbb, b, a
4. `re: c(ab)*` — NFA with epsilon. Accepts: c, cab, cabab. Rejects: cba, bab, etc.
5. `re: c(ab)+` — NFA with epsilon. Accepts: cab, cabab. Rejects: c, cba, etc.
6. `re: c(a|b)*` — NFA with epsilon. Accepts: c, cab, cabab, cba, cbab, caaa, cbbb, ca, cb. Rejects: ba, bab, aaa, bbb, b, a, <empty>
7. DFA: state 0 start and final, no transitions. Accepts: <empty>. Rejects: a
8. NFA: 0 -[eps]-> 1, start=0, final={1}. Accepts: <empty>. Rejects: a
9. NFA: 0 -[eps]-> 1; 1 -[eps]-> 0, start=0, final={1}. Accepts: <empty>. Rejects: a
10. Same as 9 (duplicate in task — skip)
11. DFA: 0 -[a]-> 1; 1 -[a]-> 1, start=0, final={1}. Accepts: a, aa, aaa. Rejects: <empty>
12. NFA: 0 -[a]-> 0; 0 -[a]-> 1, start=0, final={1}. Accepts: a, aa, aaa. Rejects: <empty>

### Implementation in Automaton.fs

Position: after the NFA/DFA type definitions and their modules, add a shared section:

```fsharp
/// Configuration for automaton acceptance.
[<Struct>]
type Config =
    { state: int
      position: int }

module Nfa =
    // ... existing code ...

    /// Classical NFA acceptance with working set of configurations.
    /// Handles epsilon transitions via epsilon closure expansion.
    let accept (a: NFA<'t,'s>) (input: Terminal<'t> list) : bool = ...

module Dfa =
    // ... existing code ...

    /// DFA acceptance — sequential state transitions, no epsilon.
    let accept (a: DFA<'t,'s>) (input: Terminal<'t> list) : bool = ...
```

Note: The input is `Terminal<'t> list` as specified. We unwrap `Terminal sym` to get the symbol for `move`.

---

## Task 82: Implement two automaton intersection

### Goal

Intersect two NFAs (without epsilon transitions) using linear algebra. Result is an NFA whose language is the intersection of the two input languages.

### Algorithm

Input: Two NFAs `A` and `B`, both without epsilon transitions.

1. **Kronecker product of transition matrices**: For each label `a` that appears in both automata, construct `K_a = N_A^a ⊗ N_B^a` (where `N_X^a` is the boolean transition matrix for label `a` in automaton X). Sum all `K_a` via element-wise OR into one combined matrix `K` of size `(nA*nB) × (nA*nB)`.

2. **Forward MS-BFS** from start pairs: `S = {(sA, sB) | sA ∈ A.startStates, sB ∈ B.startStates}`. Run MS-BFS on `K` from sources `S`. Result: `forwardVisited` — a `|S| × (nA*nB)` boolean matrix where row `i` indicates which product states are reachable from start pair `i`.

3. **Backward MS-BFS** from final pairs: Transpose `K` to reverse all edges. Create sources from final pairs `F = {(fA, fB) | fA ∈ A.finalStates, fB ∈ B.finalStates}`. Run MS-BFS on `K^T`. Result: `backwardVisited` — which product states can reach a final pair.

4. **Intersect forward and backward visited**: A product state `(q1, q2)` is on a path from start to final iff it's reachable from some start pair AND can reach some final pair. We OR-across rows: for forward, any row that has the state visited makes it "useful"; similarly for backward. Then intersect: `usefulStates = forwardReachableStates ∩ backwardReachingStates`. Also include start pairs and final pairs (reflexive: a state always reaches itself in 0 steps, but MS-BFS doesn't include the sources themselves... wait, MS-BFS initializes currentFront with the sources, and on first iteration adds them to visited. So visited includes sources. But MS-BFS doesn't count 0-length paths since it only adds after multiplication. Actually let me re-check: MS-BFS does:
   - init: currentFront[i, sources[i]] = true
   - while currentFront != 0: visited += currentFront; newFront = currentFront * M; currentFront = newFront - visited
   
   So yes, visited includes the sources on the first iteration. So forwardVisited includes start pairs and backwardVisited includes final pairs. Good.

5. **Filter edges**: Build the product graph's edges. For each product state `(iA, iB)` and `(jA, jB)` in `usefulStates`, if there's an edge in `K` between them, keep it. This can be done via `Graph.filterOutgoing` then `Graph.filterIncoming` on the product graph.

6. **Construct result NFA**: States are the useful product states. Start states are start pairs that are in usefulStates. Final states are final pairs that are in usefulStates. No epsilon transitions.

### Implementation

New file or add to Automaton.fs? Since the algorithm operates on NFA types and produces an NFA, it makes sense to put it in `Automaton.fs` within the `Nfa` module.

Function signature:
```fsharp
/// Intersect two NFAs without epsilon transitions.
/// Uses Kronecker product of transition matrices, forward MS-BFS from start pairs,
/// backward MS-BFS from final pairs, and edge filtering.
/// Returns an NFA whose language is L(A) ∩ L(B).
let intersect (a: NFA<'t,'s>) (b: NFA<'t,'t>) : NFA<'t, int * int>
```

Note: The result's state type is `int * int` (product state = (index in A, index in B)).

Wait — but the return type of `intersect` should allow both NFAs to have possibly different state label types. The result's state type... Since we're working with indices internally, `int * int` is natural. But what about the NFA state type parameter? The existing NFA type is `NFA<'t,'s>`. For the result, `'s` would be `int * int` — the product of indices.

Actually, let me think about this differently. The task says "implement two automaton intersection." The test says "intersection must accept strings that accepted by both automaton and rejects string that rejected by at least one automata." So the primary thing to test is acceptance. We could either:
1. Return the intersected NFA and test via `Nfa.accept`
2. Or just return a boolean matrix / acceptance function

Since the task says "this algo is applicable for NFA without epsilon-transitions" and lists 5 steps to produce a filtered automaton, I think we should return the intersected NFA. But the state labels would be `int * int` (product indices).

Actually, let me reconsider. The steps describe filtering edges. The final result should be an automaton (NFA). Let's look at step 5: "Select edges between vertices from step 4. Use filtering from task 80. Now can you filter out useless transitions."

Yes, the output is an NFA. The state type for the result can just be `Set<int * int>` as a single "meta state" or we can map to fresh indices. Let me think...

The simplest: result states are the product pairs that survived filtering. Map them to fresh indices 0..k-1. Return an NFA with these k states. The vertex labels can just be the product pairs `(int * int)`.

Actually, looking at how NFA stores states: `NFA<'t, 's>` has `Graph<'s, Option<NonEmptySet<'t>>>` where `vertexMap: Map<int, 's>`. So the state labels `'s` can be anything.

For the result, I'll use `int * int` as the label type (product of indices in A and B). The indices in the result NFA are 0..k-1, and the vertex map maps them to `(iA, iB)` pairs.

Let me code this up:

```fsharp
let intersect (a: NFA<'t,'s>) (b: NFA<'t,'v>) : NFA<'t, int * int> =
    let nA = stateCount a
    let nB = stateCount b
    let n = nA * nB
    
    // Index mapping
    let idx (iA: int) (iB: int) = iA * nB + iB
    
    // 1. Kronecker product of transition matrices per label
    let perLabelA = BooleanDecomposition.decomposeNonEmptySet a.transitions
    let perLabelB = BooleanDecomposition.decomposeNonEmptySet b.transitions
    
    let k = Matrix.init n n false
    for KeyValue(label, matB) in perLabelB do
        match Map.tryFind label perLabelA with
        | Some matA ->
            let kronMat = LinearAlgebra.kron matA matB (&&) false
            for i in 0 .. n - 1 do
                for j in 0 .. n - 1 do
                    if kronMat.data.[i, j] then k.data.[i, j] <- true
        | None -> ()
    
    // 2. Forward MS-BFS from start pairs
    let startPairs = 
        [| for sA in a.startStates do
               for sB in b.startStates -> idx sA sB |]
    
    let forwardVisited = MsBfs.msBfs startPairs k
    
    // 3. Backward MS-BFS from final pairs (on transposed K)
    let kT = Matrix.transpose k
    let finalPairs =
        [| for fA in a.finalStates do
               for fB in b.finalStates -> idx fA fB |]
    
    let backwardVisited = MsBfs.msBfs finalPairs kT
    
    // 4. Intersect forward and backward visited to find useful product states
    // OR across all source rows for forward (any source can reach this state)
    // OR across all final rows for backward (this state can reach any final)
    let reachableFromStart = Array.create n false
    for s in 0 .. startPairs.Length - 1 do
        for i in 0 .. n - 1 do
            if forwardVisited.data.[s, i] then reachableFromStart.[i] <- true
    
    let canReachFinal = Array.create n false
    for s in 0 .. finalPairs.Length - 1 do
        for i in 0 .. n - 1 do
            if backwardVisited.data.[s, i] then canReachFinal.[i] <- true
    
    // Include start and final pairs explicitly (reflexive relation)
    for sp in startPairs do reachableFromStart.[sp] <- true
    for fp in finalPairs do canReachFinal.[fp] <- true
    
    let usefulStates = 
        [| for i in 0 .. n - 1 do
               if reachableFromStart.[i] && canReachFinal.[i] then i |]
    
    // 5. Filter edges between useful states
    let usefulSet = Set.ofArray usefulStates
    // Rebuild automaton from useful product states
    ...
```

Actually, let me reconsider the approach for step 5. The task says "Select edges between vertices from step 4. Use filtering from task 80." 

The filtering from task 80 is `Graph.filterOutgoing` and `Graph.filterIncoming`, which work on `Graph<'v, bool>`. So I need to:
1. Create a boolean graph from the K matrix with all n vertices
2. Filter outgoing edges from useful states
3. Filter incoming edges to useful states
4. The resulting graph has edges only between useful states

```fsharp
let productGraph = Graph.fromEdges [0..n-1] k
let filtered = 
    productGraph 
    |> Graph.filterOutgoing usefulSet 
    |> Graph.filterIncoming usefulSet
```

Then extract edges from the filtered graph. But we need to map product states back to fresh indices. Let me think about this...

Actually, the simplest approach: after getting usefulStates, build a fresh NFA with states indexed 0..|usefulStates|-1. The state labels are the product pairs. Transitions: for each pair (i, j) of useful states, if k has an edge between them (across all labels), add transitions for all labels on that edge.

Wait, but k is a boolean matrix (just says "there is an edge" without labels). We need label information. Let me reconsider...

The Kronecker product k encodes edge existence. But to build the intersected NFA, I need per-label transition information. Let me think about this differently.

For step 5, after identifying useful product states, I need to extract the actual transitions (with labels). For each label `a` that appears in both automata:
- For each useful state pair (i1,i2) and (j1,j2), if A has a transition i1-[a]->j1 AND B has a transition i2-[a]->j2, then add transition usefulIdx(i1,i2) -[a]-> usefulIdx(j1,j2).

This is more direct than using boolean filtering. The task says "Use filtering from task 80" but I think the spirit is about the linear algebra approach. Let me use the per-label approach for correctness.

Actually, wait. Let me re-read: "Select edges between vertices from step 4. Use filtering form task 80. Now can you filter out useless transitions."

So step 4 identifies useful product states (the intersection of forward-reachable and backward-reaching). Step 5 filters edges: keep only edges between useful states in the combined transition matrix. The filtering from task 80 (`Graph.filterOutgoing` followed by `Graph.filterIncoming`) does exactly this on a boolean graph.

Then after filtering, I need to reconstruct label information. Hmm... Actually, the filtered boolean graph tells me which edges exist between useful states. But I need the labels. Let me think...

The combined boolean matrix `k` already encodes ALL edges (for all labels). After filtering with `Graph.filterOutgoing` + `Graph.filterIncoming`, the resulting boolean matrix still has only existence info. To get labels, I need to go back to the per-label info.

Alternative approach that's cleaner:
1. Build the combined boolean adjacency `k` (step 1)
2. Forward + backward MS-BFS (steps 2-3)
3. Intersect to find useful states (step 4)
4. For each label present in both automata, build the Kronecker product `kron(matA, matB)` and then apply the filtering: zero out rows/cols for non-useful states → this gives the per-label transition matrix
5. Build the NFA from per-label matrices

Actually, this is getting complicated. Let me simplify: after finding useful states, I can just iterate and build the transitions directly, which is simpler and correct.

```fsharp
// Rebuild automaton from useful states
let usefulStateMap = usefulStates |> Array.mapi (fun i prodIdx -> (prodIdx, i)) |> Map.ofArray

let resultStates = usefulStates |> Array.map (fun prodIdx -> 
    let iA = prodIdx / nB
    let iB = prodIdx % nB
    (iA, iB)) |> Array.toList

let resultStartStates = 
    startPairs |> Array.filter (fun sp -> Set.contains sp usefulSet) 
    |> Array.map (fun sp -> Map.find sp usefulStateMap)
    |> Set.ofArray

let resultFinalStates =
    finalPairs |> Array.filter (fun fp -> Set.contains fp usefulSet)
    |> Array.map (fun fp -> Map.find fp usefulStateMap)
    |> Set.ofArray

// Build transitions: for each label, for each pair of useful states
let transitions = ResizeArray()
for KeyValue(label, matB) in perLabelB do
    match Map.tryFind label perLabelA with
    | Some matA ->
        let kronMat = LinearAlgebra.kron matA matB (&&) false
        for pIdx in usefulStates do
            for qIdx in usefulStates do
                if kronMat.data.[pIdx, qIdx] then
                    transitions.Add(Map.find pIdx usefulStateMap, label, Map.find qIdx usefulStateMap)
    | None -> ()

{ graph = Graph.fromEdges resultStates (Nfa.buildMatrix resultStates.Length (List.ofSeq transitions))
  epsTransitions = Set.empty
  startStates = resultStartStates
  finalStates = resultFinalStates }
```

Wait, but this approach multiplies via Kronecker for each label again, which is redundant with step 1. And step 1 already built `k` by summing kronecker products. The issue is `k` loses label info.

Let me just bite the bullet and do it in a straightforward way. After computing useful states, I'll rebuild the transition info from the original automata directly without redoing Kronecker. That's simpler and more correct:

```fsharp
// Rebuild transitions from original automata
let transitions = ResizeArray()
for pIdx in usefulStates do
    let pA = pIdx / nB
    let pB = pIdx % nB
    for qIdx in usefulStates do
        let qA = qIdx / nB
        let qB = qIdx % nB
        // Check if there's a common label on edges pA->qA in A and pB->qB in B
        match a.transitions.data.[pA, qA], b.transitions.data.[pB, qB] with
        | Some nesA, Some nesB ->
            let common = Set.intersect (NonEmptySet.toSet nesA) (NonEmptySet.toSet nesB)
            for label in common do
                transitions.Add(Map.find pIdx usefulStateMap, label, Map.find qIdx usefulStateMap)
        | _ -> ()
```

This is cleaner. But it uses Set.intersect. We already have NonEmptySet... Let me check if NonEmptySet has intersection. Looking at FSharpPlus docs...

Actually, `NonEmptySet` is from FSharpPlus. It has `NonEmptySet.intersect` which returns `Set<'t>` (possibly empty). That works fine.

Now about the step 4 refinement: "Do not forget include start and final states (reflexive relation)." This means that start pairs and final pairs should always be considered useful even if MS-BFS doesn't mark them (but MS-BFS does mark sources in visited, so they'll be included). Still, adding them explicitly is defensive.

However, there's a subtle issue: backward MS-BFS uses `kT = Matrix.transpose k`. Running MS-BFS from final pairs on `kT` finds states that can reach final pairs (i.e., by following edges forward). Actually wait, no — MS-BFS on `kT` from final pairs follows edges from final pairs *backward*. So if final pairs are sources, and we multiply by `kT` (which has edges reversed), then `kT[i,j] = true` means there's an edge j→i in the original graph. So MS-BFS on `kT` from final pairs: `newFront = currentFront * kT` means:
- If final pair f can reach state x via one backward step in kT, that means in the original graph, there's an edge x → f. So backwardVisited[s, x] = true means: from final pair f_s, by following reverse edges, we can reach x. I.e., x can reach f_s in the original graph.

Yes, this is correct. `backwardVisited` gives us states that can reach final pairs.

Now for intersecting the results: `forwardVisited` is `|S| × n` and `backwardVisited` is `|F| × n`. I need to collapse across rows:
- A product state is "forward useful" if ∃s: forwardVisited[s, prodIdx] = true
- A product state is "backward useful" if ∃f: backwardVisited[f, prodIdx] = true

A state is on a start-to-final path if both are true.

For the "reflexive" inclusion: start pairs are always in forward useful (MS-BFS includes them in visited) and final pairs are always in backward useful. But to be safe, I'll explicitly mark them.

### Files changed

- `src/FLPQ.Languages/Automaton.fs`:
  - Add `Nfa.intersect` function

- `tests/FLPQ.Languages.Tests/AutomatonTests.fs`:
  - Add property-based test: intersection language equals L(A) ∩ L(B)

### Test strategy

Property-based test using FsCheck:
1. Generate two random NFAs (without epsilon transitions) over the same alphabet
2. Generate random strings over the alphabet
3. Check: `Nfa.accept intersectionResult string = (Nfa.accept a string && Nfa.accept b string)`

We'll also add some concrete tests with small hand-crafted automata.

For the property test, we need:
- A generator for NFAs (no epsilon)
- A generator for strings (over the common alphabet)
- Use FsCheck to run the property

We can reuse existing generators from the RPQ tests or create simple ones. Let me check RandomGraphGenerators...
