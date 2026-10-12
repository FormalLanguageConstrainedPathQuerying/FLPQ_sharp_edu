namespace FLPQ.CFPQ

open System
open System.IO
open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.LinearAlgebra

/// Parsing of CFPQ inputs: labeled graphs in the Boolean MatrixMarket format (one file per
/// edge label), context-free grammars in the plain .cnf template format, and reference result
/// matrices. Book: Chapter 12, 02_MatrixBased.tex — the input graph G = (V, E) and the
/// grammar G = (N, Sigma, P) of the context-free path query.
module CfpqInput =

    /// A labeled directed graph: the vertex count plus one Boolean adjacency matrix per edge
    /// label. Vertices are 0-based integers in [0, VertexCount).
    type CfpqGraph<'t> when 't: comparison =
        { VertexCount: int
          Labels: Map<'t, Matrix<bool>> }

    /// View a string-labeled CFPQ graph as an NFA (one transition per labeled edge, no start or
    /// final states) for reuse of the NFA-based graph visualizers.
    let toNfa (g: CfpqGraph<string>) : NFA<string, int> =
        let edges =
            g.Labels
            |> Map.toList
            |> List.collect (fun (label, m) ->
                [ for i in 0 .. g.VertexCount - 1 do
                      for j in 0 .. g.VertexCount - 1 do
                          if m.[i, j] then
                              { From = i; Label = label; To = j } ])

        Nfa.fromTransitions [ 0 .. g.VertexCount - 1 ] edges Set.empty Set.empty Set.empty

    /// Split text into trimmed non-empty lines (CRLF-tolerant).
    let private splitLines (text: string) : string array =
        text.Split('\n')
        |> Array.map (fun l -> l.TrimEnd('\r').Trim())
        |> Array.filter (fun l -> l <> "")

    /// Parse a Boolean coordinate pattern MatrixMarket text (the flpq-data format): the first
    /// line is `%%MatrixMarket matrix coordinate pattern general`, followed by optional `%%`
    /// comment lines, then `rows cols nnz`, then exactly nnz 0-based `i j` entry lines.
    /// Returns the dense Boolean matrix (duplicate entries collapse).
    let parseMatrixMarketText (text: string) : Matrix<bool> =
        let lines = splitLines text

        if Array.isEmpty lines || not (lines.[0].StartsWith("%%MatrixMarket")) then
            invalidArg "text" "MatrixMarket text must start with a '%%MatrixMarket' header line"

        let header = lines.[0]

        if not (header.Contains("matrix coordinate pattern")) then
            invalidArg "text" $"Unsupported MatrixMarket header: {header}"

        let mutable i = 1

        while i < lines.Length && lines.[i].StartsWith("%%") do
            i <- i + 1

        if i >= lines.Length then
            invalidArg "text" "MatrixMarket text is missing the 'rows cols nnz' line"

        let dims = lines.[i].Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries)

        if dims.Length <> 3 then
            invalidArg "text" $"Invalid MatrixMarket dimensions line: {lines.[i]}"

        let rows = Int32.Parse dims.[0]
        let cols = Int32.Parse dims.[1]
        let nnz = Int32.Parse dims.[2]

        if rows < 0 || cols < 0 || nnz < 0 then
            invalidArg "text" "MatrixMarket dimensions must be non-negative"

        let dataLines = lines.[i + 1 ..]

        if dataLines.Length <> nnz then
            invalidArg "text" $"MatrixMarket declares {nnz} entries but has {dataLines.Length} data lines"

        let pairs =
            dataLines
            |> Array.map (fun l ->
                let p = l.Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries)

                if p.Length <> 2 then
                    invalidArg "text" $"Invalid MatrixMarket entry line: {l}"

                let r = Int32.Parse p.[0]
                let c = Int32.Parse p.[1]

                if r < 0 || r >= rows || c < 0 || c >= cols then
                    invalidArg "text" $"MatrixMarket entry ({r},{c}) is out of bounds for a {rows}x{cols} matrix"

                (r, c))
            |> Set.ofArray

        Matrix.create rows cols (fun i j -> Set.contains (i, j) pairs)

    /// Parse a Boolean coordinate pattern MatrixMarket file.
    let parseMatrixMarketFile (path: string) : Matrix<bool> =
        File.ReadAllText path |> parseMatrixMarketText

    /// Parse a graph directory: every `*.mtx` file becomes the adjacency matrix of its label
    /// (the file name without extension). All files must agree on dimensions.
    let parseGraphDir (dir: string) : CfpqGraph<string> =
        if not (Directory.Exists dir) then
            invalidArg "dir" $"Graph directory not found: {dir}"

        let files = Directory.GetFiles(dir, "*.mtx") |> Array.sort

        if Array.isEmpty files then
            invalidArg "dir" $"No .mtx files in graph directory: {dir}"

        let mutable dims: (int * int) option = None

        let labels =
            files
            |> Array.map (fun f ->
                let label = Path.GetFileNameWithoutExtension f
                let m = parseMatrixMarketFile f
                let rows, cols = Matrix.rows m, Matrix.cols m

                match dims with
                | Some(r, c) when (rows, cols) <> (r, c) ->
                    invalidArg "dir" $"Matrix dimensions disagree in {dir}: {label} is {rows}x{cols}, expected {r}x{c}"
                | _ -> ()

                dims <- Some(rows, cols)
                (label, m))
            |> Map.ofArray

        let (rows, _) = dims.Value
        { VertexCount = rows; Labels = labels }

    /// Parse a plain .cnf grammar text: one production per line (symbols separated by tabs or
    /// whitespace, the left-hand side first; a bare left-hand side is an epsilon rule), blank
    /// lines ignored, and the last two non-blank lines are `Count:` and the start symbol. A
    /// symbol is a nonterminal iff it is the left-hand side of some production; all other
    /// symbols are terminals. Indexed placeholders (a `_i` suffix) are rejected — materialize
    /// the template over a concrete graph with tools/flpq_data/export.py first.
    let parseCnfText (text: string) : Grammar<string, string> =
        let lines = splitLines text

        if Array.length lines < 2 then
            invalidArg "text" ".cnf grammar must contain productions and the 'Count:' trailer"

        let countIdx = Array.tryFindIndex (fun l -> l = "Count:") lines

        match countIdx with
        | None -> invalidArg "text" ".cnf grammar is missing the 'Count:' trailer line"
        | Some idx when idx + 1 >= Array.length lines ->
            invalidArg "text" ".cnf grammar is missing the start symbol after 'Count:'"
        | Some idx ->
            let prodLines = lines.[0 .. idx - 1]

            if Array.isEmpty prodLines then
                invalidArg "text" ".cnf grammar must contain at least one production"

            let startSymbol = lines.[idx + 1]

            let checkIndexed (sym: string) (where: string) =
                if sym.EndsWith("_i") then
                    invalidArg
                        "text"
                        $"Indexed placeholder '{sym}' in {where} — materialize the grammar over a graph with tools/flpq_data/export.py first"

            for l in prodLines do
                for sym in l.Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries) do
                    checkIndexed sym "a production"

            checkIndexed startSymbol "the start symbol"

            let productions =
                prodLines
                |> Array.map (fun l ->
                    let parts = l.Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries)
                    (parts.[0], parts.[1..]))

            let lhsSet = Set.ofArray (productions |> Array.map fst)

            if not (Set.contains startSymbol lhsSet) then
                invalidArg "text" $"Start symbol '{startSymbol}' is not a nonterminal"

            let rules =
                productions
                |> Array.map (fun (lhs, rhs) ->
                    let symbols =
                        rhs
                        |> Array.toList
                        |> List.map (fun s ->
                            if Set.contains s lhsSet then
                                Symbol.N(Nonterminal s)
                            else
                                Symbol.T(Terminal s))

                    let rhs' =
                        if List.isEmpty symbols then
                            Rhs.EpsilonRhs
                        else
                            Rhs.Symbols(NonEmptyList.ofList symbols)

                    { Lhs = Nonterminal lhs; Rhs = rhs' })
                |> Array.toList

            { Rules = rules
              Start = Nonterminal startSymbol }

    /// Parse a plain .cnf grammar file.
    let parseCnfFile (path: string) : Grammar<string, string> = File.ReadAllText path |> parseCnfText

    /// Parse a reference result matrix (results.mtx): a Boolean MatrixMarket file.
    let parseResultsFile (path: string) : Matrix<bool> = parseMatrixMarketFile path
