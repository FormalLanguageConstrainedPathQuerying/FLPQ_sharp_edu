module PathSemiringTests

open Xunit
open FsCheck.Xunit
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.RPQ
open FLPQ.TestUtilities

let private matricesEqual (a: Matrix<Set<int list>>) (b: Matrix<Set<int list>>) : bool =
    if Matrix.rows a <> Matrix.rows b || Matrix.cols a <> Matrix.cols b then
        false
    else
        [ for i in 0 .. Matrix.rows a - 1 do
              for j in 0 .. Matrix.cols a - 1 do
                  if a.[i, j] <> b.[i, j] then
                      false ]
        |> List.forall id

// --- isSimple / extend ---

[<Fact>]
let ``isSimple: no repeated vertex`` () =
    Assert.True(PathSemiring.isSimple [ 0; 1; 2 ])
    Assert.True(PathSemiring.isSimple [ 5 ])
    Assert.True(PathSemiring.isSimple [])
    Assert.False(PathSemiring.isSimple [ 0; 1; 0 ])
    Assert.False(PathSemiring.isSimple [ 2; 2 ])

[<Fact>]
let ``extend: new vertex appended, repeated vertex rejected`` () =
    Assert.Equal<Option<int list>>(Some [ 0; 1; 2 ], PathSemiring.extend [ 0; 1 ] 2)
    Assert.Equal(None, PathSemiring.extend [ 0; 1 ] 1)
    Assert.Equal<Option<int list>>(Some [ 3 ], PathSemiring.extend [] 3)

// --- mulCells ---

[<Fact>]
let ``mulCells: matching endpoints concatenate`` () =
    let a = Set.singleton [ 0; 1 ]
    let b = Set.singleton [ 1; 2 ]
    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1; 2 ], PathSemiring.mulCells a b)

[<Fact>]
let ``mulCells: mismatching endpoints give empty`` () =
    let a = Set.singleton [ 0; 1 ]
    let b = Set.singleton [ 2; 3 ]
    Assert.True(PathSemiring.isZero (PathSemiring.mulCells a b))

[<Fact>]
let ``mulCells: non-simple concatenation dropped`` () =
    let a = Set.singleton [ 0; 1 ]
    let b = Set.singleton [ 1; 0; 2 ]
    Assert.True(PathSemiring.isZero (PathSemiring.mulCells a b))

[<Fact>]
let ``mulCells: multiple paths`` () =
    let a = Set.ofList [ [ 0; 1 ]; [ 0; 2 ] ]
    let b = Set.ofList [ [ 1; 3 ]; [ 2; 3 ] ]
    Assert.Equal<Set<int list>>(Set.ofList [ [ 0; 1; 3 ]; [ 0; 2; 3 ] ], PathSemiring.mulCells a b)

// --- mxm / identity ---

[<Fact>]
let ``mxm: 2x2 with trivial diagonal path`` () =
    let a =
        Matrix.create 2 2 (fun i j -> if i = 0 && j = 1 then Set.singleton [ 0; 1 ] else Set.empty)

    let b =
        Matrix.create 2 2 (fun i j -> if i = 1 && j = 1 then Set.singleton [ 1 ] else Set.empty)

    let c = PathSemiring.mxm a b
    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1 ], c.[0, 1])
    Assert.True(PathSemiring.isZero c.[0, 0])
    Assert.True(PathSemiring.isZero c.[1, 0])
    Assert.True(PathSemiring.isZero c.[1, 1])

[<Fact>]
let ``mxm: 3x3 chain`` () =
    let a =
        Matrix.create 3 3 (fun i j ->
            if (i, j) = (0, 1) then Set.singleton [ 0; 1 ]
            elif (i, j) = (1, 2) then Set.singleton [ 1; 2 ]
            else Set.empty)

    let c = PathSemiring.mxm a a
    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1; 2 ], c.[0, 2])
    Assert.True(PathSemiring.isZero c.[0, 0])
    Assert.True(PathSemiring.isZero c.[0, 1])

[<Fact>]
let ``identity is the multiplicative unit`` () =
    let m =
        Matrix.create 3 3 (fun i j ->
            if (i, j) = (0, 1) then Set.ofList [ [ 0; 1 ]; [ 0; 2; 1 ] ]
            elif (i, j) = (1, 2) then Set.singleton [ 1; 2 ]
            else Set.empty)

    let left = PathSemiring.mxm (PathSemiring.identity 3) m
    let right = PathSemiring.mxm m (PathSemiring.identity 3)
    Assert.True(matricesEqual m left)
    Assert.True(matricesEqual m right)

// --- boolSelectRows / boolExtendCols ---

[<Fact>]
let ``boolSelectRows: union over rows selected by the boolean matrix`` () =
    let n = Matrix.create 2 2 (fun i j -> (i, j) = (0, 1) || (i, j) = (1, 1))

    let m =
        Matrix.create 2 3 (fun i j ->
            if i = 0 && j = 0 then Set.singleton [ 0; 0 ]
            elif i = 1 && j = 2 then Set.singleton [ 1; 2 ]
            else Set.empty)

    let r = PathSemiring.boolSelectRows n m
    Assert.Equal(2, Matrix.rows r)
    Assert.Equal(3, Matrix.cols r)

    for v in 0..2 do
        Assert.True(PathSemiring.isZero r.[0, v])

    Assert.Equal<Set<int list>>(Set.singleton [ 0; 0 ], r.[1, 0])
    Assert.True(PathSemiring.isZero r.[1, 1])
    Assert.Equal<Set<int list>>(Set.singleton [ 1; 2 ], r.[1, 2])

[<Fact>]
let ``boolExtendCols: paths extended by edge targets, non-simple dropped`` () =
    let m =
        Matrix.create 2 3 (fun i j ->
            if i = 0 && j = 1 then Set.singleton [ 0; 1 ]
            elif i = 1 && j = 2 then Set.singleton [ 1; 2 ]
            else Set.empty)

    let g =
        Matrix.create 3 3 (fun i j -> (i, j) = (1, 2) || (i, j) = (2, 0) || (i, j) = (1, 0))

    let r = PathSemiring.boolExtendCols m g

    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1; 2 ], r.[0, 2])
    // [0; 1] extended by 0 revisits 0: dropped
    Assert.True(PathSemiring.isZero r.[0, 0])
    Assert.Equal<Set<int list>>(Set.singleton [ 1; 2; 0 ], r.[1, 0])
    Assert.True(PathSemiring.isZero r.[1, 2])

// --- transitiveClosure ---

[<Fact>]
let ``transitiveClosure: 3-cycle keeps only simple paths`` () =
    let m =
        Matrix.create 3 3 (fun i j ->
            if (i + 1) % 3 = j then
                Set.singleton [ i; j ]
            else
                Set.empty)

    let tc = PathSemiring.transitiveClosure m

    Assert.Equal<Set<int list>>(Set.singleton [ 0 ], tc.[0, 0])
    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1 ], tc.[0, 1])
    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1; 2 ], tc.[0, 2])
    Assert.Equal<Set<int list>>(Set.singleton [ 1; 2; 0 ], tc.[1, 0])
    Assert.Equal<Set<int list>>(Set.singleton [ 2; 0; 1 ], tc.[2, 1])

[<Fact>]
let ``transitiveClosure: acyclic 4-vertex graph`` () =
    let m =
        Matrix.create 4 4 (fun i j ->
            if (i, j) = (0, 1) || (i, j) = (1, 2) || (i, j) = (0, 2) || (i, j) = (2, 3) then
                Set.singleton [ i; j ]
            else
                Set.empty)

    let tc = PathSemiring.transitiveClosure m

    Assert.Equal<Set<int list>>(Set.ofList [ [ 0; 1; 2; 3 ]; [ 0; 2; 3 ] ], tc.[0, 3])
    Assert.Equal<Set<int list>>(Set.singleton [ 1; 2; 3 ], tc.[1, 3])
    Assert.Equal<Set<int list>>(Set.ofList [ [ 0; 2 ]; [ 0; 1; 2 ] ], tc.[0, 2])
    Assert.Equal<Set<int list>>(Set.singleton [ 0 ], tc.[0, 0])

// --- isEmpty / allPaths / booleanProjection ---

[<Fact>]
let ``isEmpty and allPaths over a small matrix`` () =
    let m =
        Matrix.create 2 2 (fun i j ->
            if (i, j) = (0, 1) then
                Set.ofList [ [ 0; 1 ]; [ 0; 2 ] ]
            else
                Set.empty)

    Assert.False(PathSemiring.isEmpty m)
    Assert.Equal<Set<int list>>(Set.ofList [ [ 0; 1 ]; [ 0; 2 ] ], PathSemiring.allPaths m)

    let empty = Matrix.init 2 2 Set.empty
    Assert.True(PathSemiring.isEmpty empty)
    Assert.True(PathSemiring.isZero (PathSemiring.allPaths empty))

[<Fact>]
let ``booleanProjection marks exactly the non-empty cells`` () =
    let m =
        Matrix.create 2 2 (fun i j ->
            if (i, j) = (1, 0) then
                Set.singleton [ 1; 0 ]
            else
                Set.empty)

    let p = PathSemiring.booleanProjection m
    Assert.False(p.[0, 0])
    Assert.False(p.[0, 1])
    Assert.True(p.[1, 0])
    Assert.False(p.[1, 1])

// --- Properties ---

[<Properties(Arbitrary = [| typeof<PathSemiringGenerators> |])>]
module PathSemiringProperties =

    /// Local Boolean transitive closure: the same repeated-squaring loop as
    /// ArroyueloRPQ's private transitiveClosure, over MsBfs.boolAdd/boolMul.
    let private boolTransitiveClosure (m: Matrix<bool>) : Matrix<bool> =
        let n = Matrix.rows m

        let mutable a = Matrix.init n n false

        for i in 0 .. n - 1 do
            a.[i, i] <- true

        a <- MsBfs.boolAdd a m

        let mutable k = 1

        while k < n do
            a <- MsBfs.boolMul a a
            k <- k * 2

        a

    /// Every path produced by mxm is simple and has the cell's endpoints.
    [<Property>]
    let ``mxm paths are simple with correct endpoints`` (pair: Matrix<Set<int list>> * Matrix<Set<int list>>) =
        let (a, b) = pair
        let c = PathSemiring.mxm a b

        let bad =
            [ for i in 0 .. Matrix.rows c - 1 do
                  for j in 0 .. Matrix.cols c - 1 do
                      for p in Set.toSeq c.[i, j] do
                          if not (PathSemiring.isSimple p) || List.head p <> i || List.last p <> j then
                              true ]
            |> List.exists id

        Assert.False(bad, "mxm produced a non-simple or mis-terminated path at some cell")

    /// For edge matrices the boolean projection of the path transitive closure equals
    /// the Boolean transitive closure of the projection: walk existence iff simple-path
    /// existence in the same unlabeled graph.
    [<Property(Arbitrary = [| typeof<PathEdgeMatrixGenerators> |])>]
    let ``path TC projects to Boolean TC for edge matrices`` (m: Matrix<Set<int list>>) =
        let pathTc = PathSemiring.booleanProjection (PathSemiring.transitiveClosure m)
        let boolTc = boolTransitiveClosure (PathSemiring.booleanProjection m)

        let equal =
            [ for i in 0 .. Matrix.rows pathTc - 1 do
                  for j in 0 .. Matrix.cols pathTc - 1 do
                      if pathTc.[i, j] <> boolTc.[i, j] then
                          false ]
            |> List.forall id

        Assert.True(equal, "boolean projection of the path TC differs from the Boolean TC")
