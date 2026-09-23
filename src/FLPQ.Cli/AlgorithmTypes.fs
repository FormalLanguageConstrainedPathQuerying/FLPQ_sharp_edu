namespace FLPQ.Cli

open Argu

module AlgorithmTypes =

    type Algorithm =
        | CYK
        | Valiant
        | ValiantModified
        | LL
        | LR0
        | SLR1
        | CLR1
        | GLL
        | RNGLR
        | ArroyueloRPQ

    let displayName (algo: Algorithm) : string =
        match algo with
        | CYK -> "CYK"
        | Valiant -> "Valiant"
        | ValiantModified -> "Valiant (modified)"
        | LL -> "LL"
        | LR0 -> "LR(0)"
        | SLR1 -> "SLR(1)"
        | CLR1 -> "CLR(1)"
        | GLL -> "GLL"
        | RNGLR -> "RNGLR"
        | ArroyueloRPQ -> "Arroyuelo RPQ"

    type Arguments =
        | [<AltCommandLine("-a")>] Algorithm of Algorithm
        | [<AltCommandLine("-g")>] Grammar of string
        | [<AltCommandLine("-i")>] Input of string
        | [<AltCommandLine("-r")>] Regexp of string
        | [<AltCommandLine("--graph")>] GraphFile of string
        | [<AltCommandLine("-o")>] Output of string
        | [<AltCommandLine("-k")>] Lookahead of int
        | [<AltCommandLine("-s")>] Summary
        | [<AltCommandLine("--use-dot")>] UseDot
        | [<AltCommandLine("--no-sppf-table")>] NoSppfTable

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Algorithm _ ->
                    "Algorithm: CYK, Valiant, ValiantModified, LL, LR0, SLR1, CLR1, GLL, RNGLR, or ArroyueloRPQ"
                | Grammar _ -> "Path to grammar file (.bnf format)"
                | Input _ -> "Path to input string file"
                | Regexp _ ->
                    "Path to regexp file (EBNF format; the first rule's RHS is the query) — for RPQ algorithms"
                | GraphFile _ -> "Path to graph file (start vertices line + 'from label to' edges) — for RPQ algorithms"
                | Output _ -> "Output directory for step-by-step visualization"
                | Lookahead _ -> "Lookahead k for LL parser (default: 1)"
                | Summary -> "Generate merged TeX summary file"
                | UseDot -> "Use Graphviz dot for graph rendering instead of TikZ (default: TikZ)"
                | NoSppfTable ->
                    "Render table cells as sets of nonterminals without SPPF split points and production indices"
