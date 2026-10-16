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
        | BelyaninRPQ
        | AzimovCFPQ
        | KroneckerCFPQ

    /// Path-query semantics: reachability (Boolean — the classical answer, complete on cyclic
    /// graphs), simplePath (path-semiring trace; RPQ: reachable only via an accepting simple
    /// path; CFPQ: one extracted derivation per reachable pair), or allPaths (AzimovCFPQ only:
    /// the matrix records every split vertex, and enumeration yields exactly all simple paths).
    type PathQuerySemantics =
        | Reachability
        | SimplePath
        | AllPaths

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
        | BelyaninRPQ -> "Belyanin RPQ"
        | AzimovCFPQ -> "Azimov CFPQ"
        | KroneckerCFPQ -> "Kronecker CFPQ"

    type Arguments =
        | [<AltCommandLine("-a")>] Algorithm of Algorithm
        | [<AltCommandLine("-q")>] Query of string
        | [<AltCommandLine("-i")>] Input of string
        | [<AltCommandLine("-o")>] Output of string
        | [<AltCommandLine("-k")>] Lookahead of int
        | [<AltCommandLine("-s")>] Summary
        | [<AltCommandLine("-semantics")>] Semantics of PathQuerySemantics
        | [<AltCommandLine("--use-dot")>] UseDot
        | [<AltCommandLine("--no-sppf-table")>] NoSppfTable

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Algorithm _ ->
                    "Algorithm: CYK, Valiant, ValiantModified, LL, LR0, SLR1, CLR1, GLL, RNGLR, ArroyueloRPQ, BelyaninRPQ, AzimovCFPQ, or KroneckerCFPQ"
                | Semantics _ ->
                    "Path-query semantics: reachability (Boolean, classical, default), simplePath (path-semiring trace; ArroyueloRPQ/BelyaninRPQ/AzimovCFPQ), or allPaths (AzimovCFPQ only)"
                | Query _ ->
                    "Path to the query file: grammar (.bnf) for parsing algorithms, regexp (EBNF; the first rule's RHS is the query) for RPQ algorithms, grammar (.cnf) for AzimovCFPQ, or EBNF (the first rule defines the start block) for KroneckerCFPQ"
                | Input _ ->
                    "Path to the input: input string for parsing algorithms, graph file (start vertices line + 'from label to' edges) for RPQ algorithms, or graph directory (one MatrixMarket file per edge label) for AzimovCFPQ/KroneckerCFPQ"
                | Output _ -> "Output directory for step-by-step visualization"
                | Lookahead _ -> "Lookahead k for LL parser (default: 1)"
                | Summary -> "Generate merged TeX summary file"
                | UseDot -> "Use Graphviz dot for graph rendering instead of TikZ (default: TikZ)"
                | NoSppfTable ->
                    "Render table cells as sets of nonterminals without SPPF split points and production indices"
