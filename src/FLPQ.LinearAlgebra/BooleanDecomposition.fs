namespace FLPQ.LinearAlgebra

open FSharpPlus.Data

/// Boolean decomposition of a matrix over sets into a family of Boolean matrices,
/// one per distinct element. Based on definition from the book.
module BooleanDecomposition =

    let private decomposeGeneric
        (extractElements: 'cell -> 'a seq)
        (containsElement: 'a -> 'cell -> bool)
        (matrix: Matrix<'cell>)
        : Map<'a, Matrix<bool>> =
        let allElements =
            [ for i in 0 .. Matrix.rows matrix - 1 do
                  for j in 0 .. Matrix.cols matrix - 1 do
                      yield! extractElements (Matrix.get matrix i j) ]
            |> Set.ofList

        allElements
        |> Set.toList
        |> List.map (fun elem ->
            let boolMatrix =
                Matrix.create (Matrix.rows matrix) (Matrix.cols matrix) (fun i j ->
                    containsElement elem (Matrix.get matrix i j))

            (elem, boolMatrix))
        |> Map.ofList

    /// Decompose a matrix of sets into a map from each distinct element
    /// to a Boolean matrix of the same dimensions where cell[i,j] = true
    /// iff the element is in the original set at that position.
    let decompose (matrix: Matrix<Set<'a>>) : Map<'a, Matrix<bool>> =
        decomposeGeneric Set.toSeq Set.contains matrix

    /// Decompose a matrix of option-of-non-empty-sets into a map from each
    /// distinct element to a Boolean matrix where cell[i,j] = true
    /// iff the element is in the non-empty set at that position.
    /// None cells are treated as empty sets.
    let decomposeNonEmptySet (matrix: Matrix<Option<NonEmptySet<'a>>>) : Map<'a, Matrix<bool>> =
        decomposeGeneric
            (fun cell ->
                match cell with
                | Some nes -> NonEmptySet.toSeq nes
                | None -> Seq.empty)
            (fun elem cell ->
                match cell with
                | Some nes -> NonEmptySet.contains elem nes
                | None -> false)
            matrix

    /// Reconstruct a matrix of sets from a decomposition (map from element to Boolean matrix).
    /// All matrices must have the same dimensions.
    let recompose (decomp: Map<'a, Matrix<bool>>) : Matrix<Set<'a>> =
        if Map.isEmpty decomp then
            invalidArg (nameof decomp) "Decomposition must contain at least one element"

        let first = decomp |> Map.values |> Seq.head
        let rows = Matrix.rows first
        let cols = Matrix.cols first

        Matrix.create rows cols (fun i j ->
            decomp |> Map.filter (fun _ mat -> Matrix.get mat i j) |> Map.keys |> Set.ofSeq)
