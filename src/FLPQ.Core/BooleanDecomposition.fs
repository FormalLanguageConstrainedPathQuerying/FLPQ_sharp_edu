namespace FLPQ.Core

/// Boolean decomposition of a matrix over sets into a family of Boolean matrices,
/// one per distinct element. Based on definition from the book.
module BooleanDecomposition =

    /// Decompose a matrix of sets into a map from each distinct element
    /// to a Boolean matrix of the same dimensions where cell[i,j] = true
    /// iff the element is in the original set at that position.
    let decompose (m: Matrix<Set<'a>>) : Map<'a, Matrix<bool>> =
        let allElements =
            [ for i in 0 .. m.rows - 1 do
                  for j in 0 .. m.cols - 1 do
                      yield! m.data.[i, j] ]
            |> Set.ofList

        allElements
        |> Set.toList
        |> List.map (fun elem ->
            let boolMatrix =
                Matrix.create m.rows m.cols (fun i j -> Set.contains elem m.data.[i, j])

            (elem, boolMatrix))
        |> Map.ofList

    /// Reconstruct a matrix of sets from a decomposition (map from element to Boolean matrix).
    /// All matrices must have the same dimensions.
    let recompose (decomp: Map<'a, Matrix<bool>>) : Matrix<Set<'a>> =
        if Map.isEmpty decomp then
            invalidArg (nameof decomp) "Decomposition must contain at least one element"

        let first = decomp |> Map.values |> Seq.head
        let rows = first.rows
        let cols = first.cols

        Matrix.create rows cols (fun i j ->
            decomp |> Map.filter (fun _ mat -> mat.data.[i, j]) |> Map.keys |> Set.ofSeq)
