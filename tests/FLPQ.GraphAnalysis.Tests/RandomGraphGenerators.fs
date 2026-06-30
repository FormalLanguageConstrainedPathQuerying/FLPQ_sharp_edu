module RandomGraphGenerators

open FsCheck
open FLPQ.LinearAlgebra

module MyGen = FsCheck.FSharp.Gen
module MyArb = FsCheck.FSharp.Arb

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
                                        m.data.[f, t] <- true)

                                (m, Array.ofList sources)))))))
        |> MyArb.fromGen
