namespace FLPQ.Cli

open Argu

module AlgorithmTypes =

    type Algorithm =
        | CYK
        | Valiant
        | LL
        | LR0
        | SLR1
        | CLR1

    let displayName (algo: Algorithm) : string =
        match algo with
        | CYK -> "CYK"
        | Valiant -> "Valiant"
        | LL -> "LL"
        | LR0 -> "LR(0)"
        | SLR1 -> "SLR(1)"
        | CLR1 -> "CLR(1)"

    type Arguments =
        | [<AltCommandLine("-a")>] Algorithm of Algorithm
        | [<AltCommandLine("-g")>] Grammar of string
        | [<AltCommandLine("-i")>] Input of string
        | [<AltCommandLine("-o")>] Output of string
        | [<AltCommandLine("-k")>] Lookahead of int
        | [<AltCommandLine("-s")>] Summary
        | [<AltCommandLine("--use-dot")>] UseDot

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Algorithm _ -> "Parsing algorithm: CYK, Valiant, LL, LR0, SLR1, or CLR1"
                | Grammar _ -> "Path to grammar file (.bnf format)"
                | Input _ -> "Path to input string file"
                | Output _ -> "Output directory for step-by-step visualization"
                | Lookahead _ -> "Lookahead k for LL parser (default: 1)"
                | Summary -> "Generate merged TeX summary file"
                | UseDot -> "Use Graphviz dot for LR automaton rendering (default: Tikz)"
