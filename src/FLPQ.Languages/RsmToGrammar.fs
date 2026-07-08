namespace FLPQ.Languages

open FSharpPlus.Data

/// Convert an RSM to an equivalent BNF grammar.
/// Each DFA block is converted to a right-linear grammar fragment,
/// following the book's construction (Chapter 5, 06_LinearGrammars.tex
/// and Chapter 6, 02_EBNF.tex, Theorem thm:ebnf_cfg).
module RsmToGrammar =

    let private ntName (blockNt: Nonterminal<string>) (stateIdx: int) (startState: int) : string =
        let (Nonterminal name) = blockNt

        if stateIdx = startState then
            name
        else
            sprintf "%s_q%d" name stateIdx

    /// Convert an RSM to a BNF grammar.
    /// For each block: DFA transitions become right-linear rules.
    /// Final states get epsilon rules.
    /// The start nonterminal is from the RSM's start block.
    let convert (rsm: RSM<string, string>) : Grammar<string, string> =
        let rules = ResizeArray<Rule<string, string>>()

        for block in RSM.blocks rsm do
            let n = Dfa.stateCount block.Dfa

            for fromIdx in 0 .. n - 1 do
                let fromNt = Nonterminal(ntName block.Nonterminal fromIdx block.Dfa.StartState)

                for sym in Dfa.alphabet block.Dfa do
                    match Dfa.move block.Dfa fromIdx sym with
                    | Some toIdx ->
                        let toNt = Nonterminal(ntName block.Nonterminal toIdx block.Dfa.StartState)

                        let rhsSymbols =
                            match sym with
                            | RsmSymbol.RTerm(Terminal t) -> [ Symbol.T(Terminal t); Symbol.N toNt ]
                            | RsmSymbol.RNonterm calledNt -> [ Symbol.N calledNt; Symbol.N toNt ]

                        rules.Add(
                            { Lhs = fromNt
                              Rhs = NonEmptyList.ofList rhsSymbols |> Symbols }
                        )
                    | None -> ()

            for finalIdx in block.Dfa.FinalStates do
                let finalNt = Nonterminal(ntName block.Nonterminal finalIdx block.Dfa.StartState)

                rules.Add { Lhs = finalNt; Rhs = EpsilonRhs }

        let startNt = (RSM.startBlock rsm).Nonterminal

        { Grammar.Rules = rules |> Seq.toList
          Start = startNt }
