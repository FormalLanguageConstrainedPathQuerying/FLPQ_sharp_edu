namespace FLPQ.RPQ

open System.IO
open FLPQ.LinearAlgebra
open FLPQ.Languages

/// Graph file reading.
/// Input format:
/// - First line (optional): space-separated list of start vertex indices (0-based).
///   If absent, all vertices are considered start vertices.
/// - Following lines: triples `fromVertex label toVertex`
/// Returns the graph as an NFA where states are vertices and transitions are edges.
module GraphReader =

    let private parseLine (line: string) : Trans<string> option =
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

            Some
                { From = fromV
                  Label = label
                  To = toV }

    /// Parse a graph from text and return it as an NFA.
    /// Vertices become states, edges become transitions, start vertices become start states.
    let parseGraph (text: string) : NFA<string, int> =
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

        let maxVertex =
            if List.isEmpty edges then
                -1
            else
                let mutable m = -1

                for { From = fromV; To = toV } in edges do
                    m <- max m (max fromV toV)

                m

        let vertexCount = maxVertex + 1

        let states = [ 0 .. vertexCount - 1 ]

        let transitions = edges

        let startStatesSet =
            if startVertices.Length = 0 && vertexCount > 0 then
                Set.ofList [ 0 .. vertexCount - 1 ]
            else
                Set.ofArray startVertices

        Nfa.fromTransitions states transitions Set.empty startStatesSet Set.empty

    /// Parse a graph from a file.
    let parseGraphFile (path: string) : NFA<string, int> = File.ReadAllText path |> parseGraph
