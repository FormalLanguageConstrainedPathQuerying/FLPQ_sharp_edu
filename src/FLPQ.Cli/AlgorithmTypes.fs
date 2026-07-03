namespace FLPQ.Cli

open Argu

module AlgorithmTypes =

    type Algorithm =
        | CYK
        | Valiant
        | LL
        | LR

    type Arguments =
        | [<AltCommandLine("-a")>] Algorithm of Algorithm
        | [<AltCommandLine("-g")>] Grammar of string
        | [<AltCommandLine("-i")>] Input of string
        | [<AltCommandLine("-o")>] Output of string
        | [<AltCommandLine("-k")>] Lookahead of int
        | [<AltCommandLine("-s")>] Summary

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Algorithm _ -> "Parsing algorithm: CYK, Valiant, LL, or LR"
                | Grammar _ -> "Path to grammar file (.bnf format)"
                | Input _ -> "Path to input string file"
                | Output _ -> "Output directory for step-by-step visualization"
                | Lookahead _ -> "Lookahead k for LL parser (default: 1)"
                | Summary -> "Generate merged TeX summary file"
