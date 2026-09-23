namespace FLPQ.TestUtilities

open FsCheck
open FLPQ.LinearAlgebra
open FLPQ.Languages
open FLPQ.RPQ
open FLPQ.GraphAnalysis

module MyGen = FsCheck.FSharp.Gen
module MyArb = FsCheck.FSharp.Arb

type MatrixGenerators =

    static member Matrix() : Arbitrary<Matrix<int>> =
        MyGen.choose (1, 5)
        |> MyGen.bind (fun rows ->
            MyGen.choose (1, 5)
            |> MyGen.bind (fun cols ->
                MyGen.choose (-100, 100)
                |> MyGen.listOfLength (rows * cols)
                |> MyGen.map (fun values ->
                    let arr = Array2D.init rows cols (fun i j -> values.[i * cols + j])
                    Matrix.ofArray2D arr)))
        |> MyArb.fromGen

    static member SameDimMatrixPair() : Arbitrary<Matrix<int> * Matrix<int>> =
        MyGen.choose (1, 5)
        |> MyGen.bind (fun rows ->
            MyGen.choose (1, 5)
            |> MyGen.bind (fun cols ->
                MyGen.choose (-100, 100)
                |> MyGen.listOfLength (rows * cols)
                |> MyGen.bind (fun valuesA ->
                    let arrA = Array2D.init rows cols (fun i j -> valuesA.[i * cols + j])

                    MyGen.choose (-100, 100)
                    |> MyGen.listOfLength (rows * cols)
                    |> MyGen.map (fun valuesB ->
                        let arrB = Array2D.init rows cols (fun i j -> valuesB.[i * cols + j])
                        (Matrix.ofArray2D arrA, Matrix.ofArray2D arrB)))))
        |> MyArb.fromGen

type LinearAlgebraGenerators =

    static member SquareMatrix() : Arbitrary<Matrix<int>> =
        MyGen.choose (1, 5)
        |> MyGen.bind (fun n ->
            MyGen.choose (-100, 100)
            |> MyGen.listOfLength (n * n)
            |> MyGen.map (fun values ->
                let arr = Array2D.init n n (fun i j -> values.[i * n + j])
                Matrix.ofArray2D arr))
        |> MyArb.fromGen

    static member CompatibleDimsPair() : Arbitrary<Matrix<int> * Matrix<int>> =
        MyGen.choose (1, 5)
        |> MyGen.bind (fun m ->
            MyGen.choose (1, 5)
            |> MyGen.bind (fun k ->
                MyGen.choose (1, 5)
                |> MyGen.bind (fun n ->
                    MyGen.choose (-10, 10)
                    |> MyGen.listOfLength (m * k)
                    |> MyGen.bind (fun valuesA ->
                        let arrA = Array2D.init m k (fun i j -> valuesA.[i * k + j])

                        MyGen.choose (-10, 10)
                        |> MyGen.listOfLength (k * n)
                        |> MyGen.map (fun valuesB ->
                            let arrB = Array2D.init k n (fun i j -> valuesB.[i * n + j])
                            (Matrix.ofArray2D arrA, Matrix.ofArray2D arrB))))))
        |> MyArb.fromGen

type SetMatrixGenerators =

    static member SetMatrix() : Arbitrary<Matrix<Set<int>>> =
        MyGen.choose (0, 5)
        |> MyGen.bind (fun rows ->
            MyGen.choose (0, 5)
            |> MyGen.bind (fun cols ->
                MyGen.choose (0, 4)
                |> MyGen.listOfLength (rows * cols)
                |> MyGen.map (fun values ->
                    let array = Array2D.init rows cols (fun i j -> Set.empty)

                    for k in 0 .. min (values.Length - 1) (rows * cols - 1) do
                        let i = k / cols
                        let j = k % cols
                        array.[i, j] <- set [ values.[k] ]

                    Matrix.ofArray2D array)))
        |> MyArb.fromGen

type RandomGraphGenerators =

    static member Graph() : Arbitrary<Matrix<bool> * int[]> =
        MyGen.choose (1, 6)
        |> MyGen.bind (fun n ->
            MyGen.choose (0, n * 2)
            |> MyGen.bind (fun edgeCount ->
                MyGen.listOfLength edgeCount (MyGen.choose (0, n - 1))
                |> MyGen.bind (fun fromList ->
                    MyGen.listOfLength edgeCount (MyGen.choose (0, n - 1))
                    |> MyGen.bind (fun toList ->
                        MyGen.choose (1, n)
                        |> MyGen.bind (fun k ->
                            MyGen.listOfLength k (MyGen.choose (0, n - 1))
                            |> MyGen.map (fun sources ->
                                let m = Matrix.init n n false

                                (fromList, toList)
                                ||> List.iter2 (fun f t ->
                                    if f <> t then
                                        m.[f, t] <- true)

                                (m, Array.ofList sources)))))))
        |> MyArb.fromGen

type RPQTestData =
    { VertexCount: int
      Edges: Trans<string> list
      Sources: int[] }

type RPQGenerators =

    static member RPQTestData() : Arbitrary<RPQTestData> =
        let alphabet = [ "a"; "b" ]

        MyGen.choose (2, 6)
        |> MyGen.bind (fun n ->
            MyGen.choose (2, n * 2)
            |> MyGen.bind (fun edgeCount ->
                MyGen.listOfLength edgeCount (MyGen.choose (0, n - 1))
                |> MyGen.bind (fun fromList ->
                    MyGen.listOfLength edgeCount (MyGen.choose (0, n - 1))
                    |> MyGen.bind (fun toList ->
                        MyGen.listOfLength edgeCount (MyGen.elements alphabet)
                        |> MyGen.bind (fun labelList ->
                            MyGen.choose (1, n)
                            |> MyGen.bind (fun k ->
                                MyGen.listOfLength k (MyGen.choose (0, n - 1))
                                |> MyGen.map (fun sources ->
                                    let edges =
                                        List.zip3 fromList labelList toList
                                        |> List.map (fun (f, l, t) -> { From = f; Label = l; To = t })
                                        |> List.filter (fun t -> t.From <> t.To)

                                    { VertexCount = n
                                      Edges = edges
                                      Sources = Array.ofList sources })))))))
        |> MyArb.fromGen

type RPQExtendedAlphabetGenerators =

    static member RPQTestDataExtended() : Arbitrary<RPQTestData> =
        let alphabet = [ "a"; "b"; "c"; "d"; "e" ]

        MyGen.choose (2, 5)
        |> MyGen.bind (fun n ->
            MyGen.choose (2, n * 2)
            |> MyGen.bind (fun edgeCount ->
                MyGen.listOfLength edgeCount (MyGen.choose (0, n - 1))
                |> MyGen.bind (fun fromList ->
                    MyGen.listOfLength edgeCount (MyGen.choose (0, n - 1))
                    |> MyGen.bind (fun toList ->
                        MyGen.listOfLength edgeCount (MyGen.elements alphabet)
                        |> MyGen.bind (fun labelList ->
                            MyGen.choose (1, n)
                            |> MyGen.bind (fun k ->
                                MyGen.listOfLength k (MyGen.choose (0, n - 1))
                                |> MyGen.map (fun sources ->
                                    let edges =
                                        List.zip3 fromList labelList toList
                                        |> List.map (fun (f, l, t) -> { From = f; Label = l; To = t })
                                        |> List.filter (fun t -> t.From <> t.To)

                                    { VertexCount = n
                                      Edges = edges
                                      Sources = Array.ofList sources })))))))
        |> MyArb.fromGen

type TokenStringGenerators =

    static member TokenString() : Arbitrary<string> =
        MyGen.choose (0, 5)
        |> MyGen.bind (fun n -> MyGen.listOfLength n (MyGen.elements [ "a"; "b"; "c" ]))
        |> MyGen.map (String.concat " ")
        |> MyArb.fromGen

type IntersectionGenerators =

    static member NfaArb() : Arbitrary<NFA<string, int>> =
        let alphabet = [ "a"; "b" ]

        let genNfa =
            MyGen.choose (1, 6)
            |> MyGen.bind (fun stateCount ->
                MyGen.listOf (
                    MyGen.choose (0, stateCount - 1)
                    |> MyGen.bind (fun fromIdx ->
                        MyGen.elements alphabet
                        |> MyGen.bind (fun label ->
                            MyGen.choose (0, stateCount - 1)
                            |> MyGen.map (fun toIdx ->
                                { From = fromIdx
                                  Label = label
                                  To = toIdx })))
                )
                |> MyGen.bind (fun transitions ->
                    MyGen.choose (1, min 2 stateCount)
                    |> MyGen.bind (fun startCount ->
                        MyGen.listOfLength startCount (MyGen.choose (0, stateCount - 1))
                        |> MyGen.bind (fun startStates ->
                            MyGen.choose (1, min 2 stateCount)
                            |> MyGen.bind (fun finalCount ->
                                MyGen.listOfLength finalCount (MyGen.choose (0, stateCount - 1))
                                |> MyGen.map (fun finalStates ->
                                    Nfa.fromTransitions
                                        ([ 0 .. stateCount - 1 ])
                                        transitions
                                        Set.empty
                                        (Set.ofList startStates)
                                        (Set.ofList finalStates)))))))

        MyArb.fromGen genNfa


type RegexGenerators =

    static member RegexPattern() : Arbitrary<Regexp<string, string>> =
        let alphabet = [ Terminal "a"; Terminal "b"; Terminal "c" ]

        let rec genExpr depth =
            if depth <= 0 then
                MyGen.frequency [ (2, MyGen.map RTerm (MyGen.elements alphabet)); (1, MyGen.constant REps) ]
            else
                MyGen.choose (0, 3)
                |> MyGen.bind (fun choice ->
                    match choice with
                    | 0 -> MyGen.map RTerm (MyGen.elements alphabet)
                    | 1 -> MyGen.map2 (fun l r -> RSeq(l, r)) (genExpr (depth - 1)) (genExpr (depth - 1))
                    | 2 -> MyGen.map2 (fun l r -> RAlt(l, r)) (genExpr (depth - 1)) (genExpr (depth - 1))
                    | _ -> MyGen.map RStar (genExpr (depth - 1)))

        MyGen.choose (0, 3) |> MyGen.bind genExpr |> MyArb.fromGen

type RegexAndGraph =
    { Regex: Regexp<string, string>
      VertexCount: int
      Edges: Trans<string> list
      Sources: int[] }

module private AbRegex =

    /// Random regexp over the alphabet {a, b} (shared by the RPQ graph generators).
    let genAbRegex: Gen<Regexp<string, string>> =
        let alphabet = [ Terminal "a"; Terminal "b" ]

        let rec genExpr depth =
            if depth <= 0 then
                MyGen.frequency [ (3, MyGen.map RTerm (MyGen.elements alphabet)); (1, MyGen.constant REps) ]
            else
                MyGen.choose (0, 3)
                |> MyGen.bind (fun choice ->
                    match choice with
                    | 0 -> MyGen.map RTerm (MyGen.elements alphabet)
                    | 1 -> MyGen.map2 (fun l r -> RSeq(l, r)) (genExpr (depth - 1)) (genExpr (depth - 1))
                    | 2 -> MyGen.map2 (fun l r -> RAlt(l, r)) (genExpr (depth - 1)) (genExpr (depth - 1))
                    | _ -> MyGen.map RStar (genExpr (depth - 1)))

        MyGen.choose (0, 3) |> MyGen.bind genExpr

type RegexAndGraphGenerators =

    static member RegexAndGraph() : Arbitrary<RegexAndGraph> =
        MyGen.choose (2, 6)
        |> MyGen.bind (fun n ->
            MyGen.choose (2, n * 2)
            |> MyGen.bind (fun edgeCount ->
                MyGen.listOfLength edgeCount (MyGen.choose (0, n - 1))
                |> MyGen.bind (fun fromList ->
                    MyGen.listOfLength edgeCount (MyGen.choose (0, n - 1))
                    |> MyGen.bind (fun toList ->
                        MyGen.listOfLength edgeCount (MyGen.elements [ "a"; "b" ])
                        |> MyGen.bind (fun labelList ->
                            MyGen.choose (1, n)
                            |> MyGen.bind (fun k ->
                                MyGen.listOfLength k (MyGen.choose (0, n - 1))
                                |> MyGen.bind (fun sources ->
                                    AbRegex.genAbRegex
                                    |> MyGen.map (fun regex ->
                                        let edges =
                                            List.zip3 fromList labelList toList
                                            |> List.map (fun (f, l, t) -> { From = f; Label = l; To = t })
                                            |> List.filter (fun t -> t.From <> t.To)

                                        { Regex = regex
                                          VertexCount = n
                                          Edges = edges
                                          Sources = Array.ofList sources }))))))))
        |> MyArb.fromGen

type AcyclicRpqGenerators =

    /// Acyclic graph (edges only from lower to higher vertex index) with a random regexp over {a, b}.
    static member AcyclicRegexAndGraph() : Arbitrary<RegexAndGraph> =
        MyGen.choose (2, 6)
        |> MyGen.bind (fun n ->
            let forwardEdges =
                [ for i in 0 .. n - 1 do
                      for j in i + 1 .. n - 1 do
                          yield (i, j) ]

            MyGen.listOfLength
                (List.length forwardEdges)
                (MyGen.frequency [ (1, MyGen.constant true); (2, MyGen.constant false) ])
            |> MyGen.bind (fun present ->
                let kept = List.zip forwardEdges present |> List.filter snd |> List.map fst

                MyGen.listOfLength (List.length kept) (MyGen.elements [ "a"; "b" ])
                |> MyGen.bind (fun labels ->
                    MyGen.choose (1, n)
                    |> MyGen.bind (fun k ->
                        MyGen.listOfLength k (MyGen.choose (0, n - 1))
                        |> MyGen.bind (fun sources ->
                            let edges =
                                List.zip kept labels
                                |> List.map (fun ((f, t), l) -> { From = f; Label = l; To = t })

                            AbRegex.genAbRegex
                            |> MyGen.map (fun regex ->
                                { Regex = regex
                                  VertexCount = n
                                  Edges = edges
                                  Sources = Array.ofList sources }))))))
        |> MyArb.fromGen

type StressStringGenerators =

    static member LongAbString() : Arbitrary<string> =
        MyGen.choose (20, 30)
        |> MyGen.map (fun n ->
            let aPart = System.String.Concat(Array.replicate n "a ")
            let bPart = System.String.Concat(Array.replicate n "b ")
            (aPart + bPart).Trim())
        |> MyArb.fromGen

type StressNfaGenerators =

    static member LargeNfa() : Arbitrary<NFA<string, int>> =
        MyGen.choose (30, 80)
        |> MyGen.bind (fun stateCount ->
            MyGen.listOfLength (stateCount * 2) (MyGen.choose (0, stateCount - 1))
            |> MyGen.bind (fun fromStates ->
                MyGen.listOfLength (stateCount * 2) (MyGen.choose (0, stateCount - 1))
                |> MyGen.bind (fun toStates ->
                    MyGen.listOfLength (stateCount * 2) (MyGen.elements [ "a"; "b" ])
                    |> MyGen.bind (fun labels ->
                        MyGen.choose (1, min 3 stateCount)
                        |> MyGen.bind (fun startCount ->
                            MyGen.listOfLength startCount (MyGen.choose (0, stateCount - 1))
                            |> MyGen.bind (fun startStates ->
                                MyGen.choose (1, min 3 stateCount)
                                |> MyGen.bind (fun finalCount ->
                                    MyGen.listOfLength finalCount (MyGen.choose (0, stateCount - 1))
                                    |> MyGen.map (fun finalStates ->
                                        let trans =
                                            List.zip3 fromStates labels toStates
                                            |> List.map (fun (f, l, t) -> { From = f; Label = l; To = t })
                                            |> List.filter (fun t -> t.From <> t.To)

                                        Nfa.fromTransitions
                                            ([ 0 .. stateCount - 1 ])
                                            trans
                                            Set.empty
                                            (Set.ofList startStates)
                                            (Set.ofList finalStates)))))))))
        |> MyArb.fromGen

type StressRpqGenerators =

    static member LargeRpqGraph() : Arbitrary<RPQTestData> =
        let alphabet = [ "a"; "b" ]

        MyGen.choose (30, 50)
        |> MyGen.bind (fun n ->
            MyGen.choose (n, n * 3)
            |> MyGen.bind (fun edgeCount ->
                MyGen.listOfLength edgeCount (MyGen.choose (0, n - 1))
                |> MyGen.bind (fun fromList ->
                    MyGen.listOfLength edgeCount (MyGen.choose (0, n - 1))
                    |> MyGen.bind (fun toList ->
                        MyGen.listOfLength edgeCount (MyGen.elements alphabet)
                        |> MyGen.bind (fun labelList ->
                            MyGen.choose (1, 3)
                            |> MyGen.bind (fun k ->
                                MyGen.listOfLength k (MyGen.choose (0, n - 1))
                                |> MyGen.map (fun sources ->
                                    let edges =
                                        List.zip3 fromList labelList toList
                                        |> List.map (fun (f, l, t) -> { From = f; Label = l; To = t })
                                        |> List.filter (fun t -> t.From <> t.To)

                                    { VertexCount = n
                                      Edges = edges
                                      Sources = Array.ofList sources })))))))
        |> MyArb.fromGen

type PathSemiringGenerators =

    /// Well-formed path matrix: cell [i, j] holds random simple paths from i to j
    /// (diagonal cells optionally hold the trivial path [i]).
    static member PathMatrix() : Arbitrary<Matrix<Set<int list>>> =
        MyGen.choose (2, 5)
        |> MyGen.bind (fun n ->
            let offDiagCells = n * (n - 1)
            let total = n + offDiagCells * (1 + 3 * (n - 2))

            MyGen.listOfLength total (MyGen.choose (0, 99))
            |> MyGen.map (fun bits ->
                let mutable idx = 0

                let next () =
                    let v = bits.[idx]
                    idx <- idx + 1
                    v

                let genPath (i: int) (j: int) : int list =
                    let others =
                        [ for v in 0 .. n - 1 do
                              if v <> i && v <> j then
                                  yield v ]

                    let flags =
                        [ for _ in others do
                              next () % 2 = 0 ]

                    let inter = List.zip others flags |> List.filter snd |> List.map fst
                    [ i ] @ inter @ [ j ]

                Matrix.create n n (fun i j ->
                    if i = j then
                        if next () % 2 = 0 then Set.singleton [ i ] else Set.empty
                    else
                        let k = next () % 4

                        [ for _ in 1..k do
                              yield genPath i j ]
                        |> List.toSeq
                        |> Set.ofSeq)))
        |> MyArb.fromGen

    /// Pair of well-formed path matrices with the same dimensions.
    static member PathMatrixPair() : Arbitrary<Matrix<Set<int list>> * Matrix<Set<int list>>> =
        MyGen.choose (2, 5)
        |> MyGen.bind (fun n ->
            let offDiagCells = n * (n - 1)
            let perMatrix = n + offDiagCells * (1 + 3 * (n - 2))

            MyGen.listOfLength (2 * perMatrix) (MyGen.choose (0, 99))
            |> MyGen.map (fun bits ->
                let build (offset: int) : Matrix<Set<int list>> =
                    let mutable idx = offset

                    let next () =
                        let v = bits.[idx]
                        idx <- idx + 1
                        v

                    let genPath (i: int) (j: int) : int list =
                        let others =
                            [ for v in 0 .. n - 1 do
                                  if v <> i && v <> j then
                                      yield v ]

                        let flags =
                            [ for _ in others do
                                  next () % 2 = 0 ]

                        let inter = List.zip others flags |> List.filter snd |> List.map fst
                        [ i ] @ inter @ [ j ]

                    Matrix.create n n (fun i j ->
                        if i = j then
                            if next () % 2 = 0 then Set.singleton [ i ] else Set.empty
                        else
                            let k = next () % 4

                            [ for _ in 1..k do
                                  yield genPath i j ]
                            |> List.toSeq
                            |> Set.ofSeq)

                (build 0, build perMatrix)))
        |> MyArb.fromGen

type PathEdgeMatrixGenerators =

    /// Edge matrix: cell [i, j] (i <> j) holds the single-edge path [i; j] or is empty;
    /// diagonal cells are empty.
    static member PathEdgeMatrix() : Arbitrary<Matrix<Set<int list>>> =
        MyGen.choose (2, 5)
        |> MyGen.bind (fun n ->
            MyGen.listOfLength (n * (n - 1)) (MyGen.choose (0, 99))
            |> MyGen.map (fun bits ->
                let mutable idx = 0

                Matrix.create n n (fun i j ->
                    if i = j then
                        Set.empty
                    else
                        let present = bits.[idx] % 2 = 0
                        idx <- idx + 1
                        if present then Set.singleton [ i; j ] else Set.empty)))
        |> MyArb.fromGen

type StressMatrixGenerators =

    static member LargeSquareMatrix() : Arbitrary<Matrix<int>> =
        MyGen.choose (30, 50)
        |> MyGen.bind (fun n ->
            MyGen.choose (-1000, 1000)
            |> MyGen.listOfLength (n * n)
            |> MyGen.map (fun values ->
                let arr = Array2D.init n n (fun i j -> values.[i * n + j])
                Matrix.ofArray2D arr))
        |> MyArb.fromGen

    static member LargeBoolMatrix() : Arbitrary<Matrix<bool>> =
        MyGen.choose (30, 50)
        |> MyGen.bind (fun n ->
            MyGen.choose (0, 1)
            |> MyGen.listOfLength (n * n)
            |> MyGen.map (fun values ->
                let arr = Array2D.init n n (fun i j -> values.[i * n + j] = 1)
                Matrix.ofArray2D arr))
        |> MyArb.fromGen
