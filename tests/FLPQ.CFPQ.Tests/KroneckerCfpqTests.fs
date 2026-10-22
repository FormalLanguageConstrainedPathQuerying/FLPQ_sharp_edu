module KroneckerCfpqTests

open Xunit
open FLPQ.CFPQ
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

// The book's per-iteration edge additions (TensorProduct.tex example): one new S-cell per
// productive iteration, in the order the book lists them.
let private bookStepCells: AzimovCfpq.CfpqCell<string> list =
    [ (1, 3); (0, 2); (2, 3); (1, 2); (0, 3); (2, 2) ]
    |> List.map (fun (i, j) ->
        { Nt = Nonterminal "S"
          Row = i
          Col = j })

// The book's final answer: L(S) = a^k b^k (k >= 1) over the example graph.
let private bookFinalCells: Set<int * int> =
    Set.ofList [ (0, 2); (0, 3); (1, 2); (1, 3); (2, 2); (2, 3) ]

/// Product-state index (mirrors KroneckerCfpq.evaluate): the pair (RSM state s, graph vertex v)
/// is s * n + v. The source for block A at vertex v sits at (start(A), v).
let private idxOf (rsm: RSM<'t, 'nt>) (n: int) (a: Nonterminal<'nt>) (v: int) : int = rsm.BlockStart.[a] * n + v

[<Fact>]
let ``tensor product example matches the book trace: iterations, per-step cells, and final matrix`` () =
    let result =
        KroneckerCfpq.evaluate CfpqTestHelpers.tensorBookRsm CfpqTestHelpers.tensorBookGraph

    // Seven iterations: six productive plus one no-change (the book's own count).
    Assert.Equal(7, result.Iterations)
    Assert.Equal(6, List.length result.Steps)

    // Exactly the one nonterminal of the book's RSM.
    let nts =
        result.Matrices
        |> Map.toList
        |> List.map (fun (nt, _) ->
            match nt with
            | Nonterminal s -> s)
        |> Set.ofList

    Assert.Equal<Set<string>>(Set.singleton "S", nts)

    // Per-step changed cells equal the book sequence; iteration numbers are 1..6.
    let actualSteps = result.Steps |> List.map (fun s -> s.ChangedCells)

    Assert.Equal<AzimovCfpq.CfpqCell<string> list list>(List.map (fun c -> [ c ]) bookStepCells, actualSteps)

    Assert.Equal<int list>([ 1..6 ], result.Steps |> List.map (fun s -> s.Iteration))

    // Each step's snapshot holds exactly the cells derived up to that iteration (the book's
    // per-iteration M2 matrices).
    for k, step in List.indexed result.Steps do
        let expected =
            bookStepCells
            |> List.take (k + 1)
            |> List.map (fun c -> (c.Row, c.Col))
            |> Set.ofList

        let actual =
            Map.find (Nonterminal "S") step.Matrices
            |> Matrix.map (function
                | Some true -> true
                | _ -> false)
            |> CfpqTestHelpers.trueCells

        Assert.Equal<Set<int * int>>(expected, actual)

    // The final matrix equals the book's answer cell by cell.
    let actual =
        KroneckerCfpq.answer result CfpqTestHelpers.tensorBookRsm
        |> CfpqTestHelpers.trueCells

    Assert.Equal<Set<int * int>>(bookFinalCells, actual)

[<Fact>]
let ``each step carries a well-formed product and MS-BFS fronts`` () =
    let rsm = CfpqTestHelpers.tensorBookRsm
    let graph = CfpqTestHelpers.tensorBookGraph
    let result = KroneckerCfpq.evaluate rsm graph
    let n = graph.VertexCount
    let q = RSM.stateCount rsm

    let sources =
        [ for a in RSM.nonterminals rsm do
              for v in 0 .. n - 1 -> idxOf rsm n a v ]

    // The book RSM has one nonterminal S with start state 0 and final state 3; the product is
    // 4 states x 4 vertices = 16 product states. The initial front seeds the 4 sources
    // (S, v_i) at product column start(S)=0 -> columns 0,1,2,3.
    let expectedInitialCells = sources |> List.mapi (fun i s -> (i, s)) |> Set.ofList
    let expectedFinalStateCols = Set.ofList [ for j in 0 .. n - 1 -> 3 * n + j ]

    for step in result.Steps do
        // Product mask K is qn x qn and equals the boolean mask of the Kronecker product.
        Assert.Equal(q * n, Matrix.rows step.K)
        Assert.Equal(q * n, Matrix.cols step.K)

        let kFromOperands =
            LinearAlgebra.kron step.M1 step.M2 Nfa.intersectEdgeSets None
            |> Matrix.map Option.isSome

        for i in 0 .. q * n - 1 do
            for j in 0 .. q * n - 1 do
                Assert.Equal(kFromOperands.[i, j], step.K.[i, j])

        // M1 is the RSM transition matrix (constant); M2 is an n x n edge-label matrix.
        Assert.Equal(q, Matrix.rows step.M1)
        Assert.Equal(n, Matrix.rows step.M2)
        Assert.Equal(n, Matrix.cols step.M2)

        // Initial front: seeds exactly the source cells, one 1 per source row.
        let initialCells = CfpqTestHelpers.trueCells step.InitialFront
        Assert.Equal<Set<int * int>>(expectedInitialCells, initialCells)

        // Final front = visited from the same sources; it contains the initial front.
        Assert.Equal(q * n, Matrix.cols step.FinalFront)
        Assert.Equal(List.length sources, Matrix.rows step.FinalFront)

        let finalCells = CfpqTestHelpers.trueCells step.FinalFront
        Assert.True(Set.isSubset expectedInitialCells finalCells)

        // The final-state columns are exactly (final state 3, vertex j) for every vertex j.
        Assert.Equal<Set<int>>(expectedFinalStateCols, step.FinalStateCols)

    // M2 of the first step is terminal-only; each later step adds exactly that step's new
    // nonterminal cells.
    let m2Cells (step: KroneckerCfpq.CfpqKronStep<string, string>) : Set<int * int> =
        step.M2
        |> Matrix.map (function
            | Some _ -> true
            | None -> false)
        |> CfpqTestHelpers.trueCells

    let labelMats = graph.Labels |> Map.toList |> List.map snd

    let terminalCells: Set<int * int> =
        labelMats |> List.map CfpqTestHelpers.trueCells |> Set.unionMany

    Assert.Equal<Set<int * int>>(terminalCells, m2Cells result.Steps.[0])

    let cumulative: Set<int * int> list =
        result.Steps
        |> List.scan
            (fun acc step -> Set.union acc (Set.ofList (step.ChangedCells |> List.map (fun c -> (c.Row, c.Col)))))
            terminalCells

    for k in 0 .. result.Steps.Length - 1 do
        Assert.Equal<Set<int * int>>(cumulative.[k], m2Cells result.Steps.[k])

[<Fact>]
let ``K masks and final fronts grow monotonically across iterations`` () =
    let result =
        KroneckerCfpq.evaluate CfpqTestHelpers.tensorBookRsm CfpqTestHelpers.tensorBookGraph

    // The K mask (the accumulated product) and the MS-BFS final front only grow across
    // iterations (the algorithm accumulates into `prod` and the visited front is monotone).
    // This is the precondition that makes the "newly filled cells" diff (current \ previous)
    // well-defined for the step visualization.
    for i in 1 .. result.Steps.Length - 1 do
        let prevK = CfpqTestHelpers.trueCells result.Steps.[i - 1].K
        let curK = CfpqTestHelpers.trueCells result.Steps.[i].K
        Assert.True(Set.isSubset prevK curK, sprintf "K not monotone at step %d" i)

        let prevF = CfpqTestHelpers.trueCells result.Steps.[i - 1].FinalFront
        let curF = CfpqTestHelpers.trueCells result.Steps.[i].FinalFront
        Assert.True(Set.isSubset prevF curF, sprintf "FinalFront not monotone at step %d" i)
