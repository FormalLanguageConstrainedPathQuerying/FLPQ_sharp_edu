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
      Edges: (int * string * int) list
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
                                        |> List.filter (fun (f, _, t) -> f <> t)

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
                                        |> List.filter (fun (f, _, t) -> f <> t)

                                    { VertexCount = n
                                      Edges = edges
                                      Sources = Array.ofList sources })))))))
        |> MyArb.fromGen

type AbStringGenerators =

    static member AbString() : Arbitrary<string> =
        MyGen.choose (0, 12)
        |> MyGen.bind (fun len ->
            MyGen.choose (0, 1)
            |> MyGen.listOfLength len
            |> MyGen.map (fun bits -> bits |> List.map (fun b -> if b = 0 then "a" else "b") |> String.concat " "))
        |> MyArb.fromGen

type AbcxdStringGenerators =

    static member AbcxdString() : Arbitrary<string> =
        let chars = [ "a"; "b"; "c"; "x"; "d" ]

        MyGen.choose (0, 8)
        |> MyGen.bind (fun len -> MyGen.listOfLength len (MyGen.elements chars) |> MyGen.map (String.concat " "))
        |> MyArb.fromGen

type TokenStringGenerators =

    static member TokenString() : Arbitrary<string> =
        MyGen.choose (0, 5)
        |> MyGen.bind (fun n -> MyGen.listOfLength n (MyGen.elements [ "a"; "b"; "c" ]))
        |> MyGen.map (String.concat " ")
        |> MyArb.fromGen

type AbcdxyStringGenerators =

    static member AbcdxyString() : Arbitrary<string> =
        let chars = [ "a"; "b"; "c"; "d"; "x"; "y" ]

        MyGen.choose (0, 8)
        |> MyGen.bind (fun len -> MyGen.listOfLength len (MyGen.elements chars) |> MyGen.map (String.concat " "))
        |> MyArb.fromGen

type AStringGenerators =

    static member AString() : Arbitrary<string> =
        MyGen.choose (0, 15)
        |> MyGen.map (fun len ->
            if len = 0 then
                ""
            else
                System.String.Concat(Array.replicate len "a ").Trim())
        |> MyArb.fromGen

type ExprStringGenerators =

    static member ExprString() : Arbitrary<string> =
        let terminals = [| "x" |]
        let operators = [| "+"; "*" |]

        let rec genExpr depth =
            if depth <= 0 then
                MyGen.elements terminals
            else
                MyGen.choose (0, 2)
                |> MyGen.bind (fun choice ->
                    match choice with
                    | 0 -> MyGen.elements terminals
                    | 1 -> genExpr (depth - 1) |> MyGen.map (fun inner -> "( " + inner + " )")
                    | _ ->
                        genExpr (depth - 1)
                        |> MyGen.bind (fun left ->
                            genExpr (depth - 1)
                            |> MyGen.bind (fun right ->
                                MyGen.elements operators |> MyGen.map (fun op -> left + " " + op + " " + right))))

        MyGen.choose (0, 4) |> MyGen.bind genExpr |> MyArb.fromGen

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
                            |> MyGen.map (fun toIdx -> (fromIdx, label, toIdx))))
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
      Edges: (int * string * int) list
      Sources: int[] }

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
                                    let genRegex =
                                        let alphabet = [ Terminal "a"; Terminal "b" ]

                                        let rec genExpr depth =
                                            if depth <= 0 then
                                                MyGen.frequency
                                                    [ (3, MyGen.map RTerm (MyGen.elements alphabet))
                                                      (1, MyGen.constant REps) ]
                                            else
                                                MyGen.choose (0, 3)
                                                |> MyGen.bind (fun choice ->
                                                    match choice with
                                                    | 0 -> MyGen.map RTerm (MyGen.elements alphabet)
                                                    | 1 ->
                                                        MyGen.map2
                                                            (fun l r -> RSeq(l, r))
                                                            (genExpr (depth - 1))
                                                            (genExpr (depth - 1))
                                                    | 2 ->
                                                        MyGen.map2
                                                            (fun l r -> RAlt(l, r))
                                                            (genExpr (depth - 1))
                                                            (genExpr (depth - 1))
                                                    | _ -> MyGen.map RStar (genExpr (depth - 1)))

                                        MyGen.choose (0, 3) |> MyGen.bind genExpr

                                    genRegex
                                    |> MyGen.map (fun regex ->
                                        let edges =
                                            List.zip3 fromList labelList toList
                                            |> List.filter (fun (f, _, t) -> f <> t)

                                        { Regex = regex
                                          VertexCount = n
                                          Edges = edges
                                          Sources = Array.ofList sources }))))))))
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
                                            |> List.filter (fun (f, _, t) -> f <> t)

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
                                        |> List.filter (fun (f, _, t) -> f <> t)

                                    { VertexCount = n
                                      Edges = edges
                                      Sources = Array.ofList sources })))))))
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
