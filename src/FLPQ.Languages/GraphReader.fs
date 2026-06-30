namespace FLPQ.Languages

open System.IO
open FLPQ.LinearAlgebra

/// Graph file reading.
/// Input format:
/// - First line (optional): space-separated list of start vertex indices (0-based).
///   If absent, all vertices are considered start vertices.
/// - Following lines: triples `fromVertex label toVertex`
/// Builds per-label boolean adjacency matrices.
module GraphReader =

    /// Parsed graph data: per-label adjacency matrices and start vertices.
    type LabeledGraph<'t when 't: comparison> =
        { vertexCount: int
          labels: Set<'t>
          adjacency: Map<'t, Matrix<bool>>
          startVertices: int[] }

    let private parseLine (line: string) : (int * string * int) option =
        let trimmed = line.Trim()

        if trimmed.Length = 0 then
            None
        else
            let parts = trimmed.Split(' ', System.StringSplitOptions.RemoveEmptyEntries)

            if parts.Length <> 3 then
                failwithf "Invalid graph line: '%s'" trimmed

            let fromV = System.Int32.Parse parts.[0]
            let label = parts.[1]
            let toV = System.Int32.Parse parts.[2]
            Some(fromV, label, toV)

    /// Parse a graph from text.
    /// Returns a Map from label to boolean adjacency matrix and the set of start vertices.
    let parseGraph (text: string) : LabeledGraph<string> =
        let lines =
            text.Split('\n', System.StringSplitOptions.RemoveEmptyEntries)
            |> Array.map (fun s -> s.Trim())
            |> Array.filter (fun s -> s.Length > 0)
            |> Array.toList

        let startVertices, edgeLines =
            match lines with
            | [] -> [||], []
            | first :: rest ->
                let allAreNumbers =
                    first.Split(' ', System.StringSplitOptions.RemoveEmptyEntries)
                    |> Array.forall (fun s -> System.Int32.TryParse(s) |> fst)

                if allAreNumbers then
                    let sv =
                        first.Split(' ', System.StringSplitOptions.RemoveEmptyEntries)
                        |> Array.map System.Int32.Parse

                    sv, rest
                else
                    [||], lines

        let edges = edgeLines |> List.choose parseLine

        let labels =
            edges |> List.map (fun (_, label, _) -> label) |> List.distinct |> Set.ofList

        let maxVertex =
            if List.isEmpty edges then
                -1
            else
                let mutable m = -1

                for (fromV, _, toV) in edges do
                    m <- max m (max fromV toV)

                m

        let vertexCount = maxVertex + 1

        let adjacency =
            labels
            |> Set.toList
            |> List.map (fun label ->
                let matrix = Matrix.init vertexCount vertexCount false

                for (fromV, l, toV) in edges do
                    if l = label then
                        matrix.data.[fromV, toV] <- true

                (label, matrix))
            |> Map.ofList

        let startVerticesArr =
            if startVertices.Length = 0 && vertexCount > 0 then
                [| 0 .. vertexCount - 1 |]
            else
                startVertices

        { vertexCount = vertexCount
          labels = labels
          adjacency = adjacency
          startVertices = startVerticesArr }

    /// Parse a graph from a file.
    let parseGraphFile (path: string) : LabeledGraph<string> = File.ReadAllText path |> parseGraph
