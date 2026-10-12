module CfpqReferenceTests

open System
open System.IO
open System.Text.Json
open Xunit
open FLPQ.CFPQ
open FLPQ.Languages
open FLPQ.LinearAlgebra

// data/cfpq is materialized by tools/flpq_data/export.py from the pinned flpq-data package
// (see tools/README.md); it is gitignored and regenerated on every CI run.
let private cfpqDataDir () : string =
    let candidates =
        [ Path.Combine("data", "cfpq")
          Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "cfpq") ]

    match candidates |> List.tryFind Directory.Exists with
    | Some p -> p
    | None ->
        failwith
            "data/cfpq not found. Materialize it first: pip install --extra-index-url https://test.pypi.org/simple/ \"flpq-data==6.0.0.dev15\" && python3 tools/flpq_data/export.py"

type private ManifestGraph =
    { VertexCount: int
      Grammars: Map<string, int> }

type private Manifest = { Graphs: Map<string, ManifestGraph> }

let private loadManifest (dir: string) : Manifest =
    let doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "manifest.json")))

    let graphs =
        doc.RootElement.GetProperty("graphs").EnumerateObject()
        |> Seq.map (fun g ->
            let entry = g.Value

            (g.Name,
             { VertexCount = entry.GetProperty("vertex_count").GetInt32()
               Grammars =
                 entry.GetProperty("grammars").EnumerateObject()
                 |> Seq.map (fun q -> (q.Name, q.Value.GetInt32()))
                 |> Map.ofSeq }))
        |> Map.ofSeq

    { Graphs = graphs }

let private firstMismatch (a: Matrix<bool>) (b: Matrix<bool>) : string option =
    if Matrix.rows a <> Matrix.rows b || Matrix.cols a <> Matrix.cols b then
        Some($"dimensions {Matrix.rows a}x{Matrix.cols a} vs {Matrix.rows b}x{Matrix.cols b}")
    else
        [ for i in 0 .. Matrix.rows a - 1 do
              for j in 0 .. Matrix.cols a - 1 do
                  if a.[i, j] <> b.[i, j] then
                      Some($"cell ({i},{j})") ]
        |> List.tryPick id

[<Fact>]
let ``Azimov reachability matches the flpq-data reference results for every pinned graph and grammar`` () =
    let dir = cfpqDataDir ()
    let manifest = loadManifest dir

    for graphName, entry in Map.toList manifest.Graphs do
        let graph = CfpqInput.parseGraphDir (Path.Combine(dir, graphName, "graph"))
        Assert.Equal(entry.VertexCount, graph.VertexCount)

        for grammarName, expectedPairs in Map.toList entry.Grammars do
            let queryDir = Path.Combine(dir, graphName, "queries", grammarName)
            let grammar = CfpqInput.parseCnfFile (Path.Combine(queryDir, $"{grammarName}.cnf"))
            let expected = CfpqInput.parseResultsFile (Path.Combine(queryDir, "results.mtx"))

            // The same CNF conversion evaluate applies internally, so the answer is queried at the CNF start.
            let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar

            let result =
                AzimovCfpq.evaluate CfpqSemiring.ReachabilitySemiring Grammar.freshStringNonterminal grammar graph

            let actual = AzimovCfpq.answer result cnf.Start

            match firstMismatch actual expected with
            | None -> ()
            | Some why -> Assert.True(false, $"{graphName}/{grammarName}: {why}")

            let count = CfpqTestHelpers.trueCellCount actual
            Assert.Equal(expectedPairs, count)
