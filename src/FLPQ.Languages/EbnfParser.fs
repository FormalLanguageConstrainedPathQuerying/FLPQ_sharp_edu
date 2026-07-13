namespace FLPQ.Languages

open System
open System.IO
open FLPQ.LinearAlgebra

/// Regular expression AST for EBNF right-hand sides.
type Regexp<'t, 'nt when 't: comparison and 'nt: comparison> =
    | REps
    | REmpty
    | RTerm of Terminal<'t>
    | RNonterm of Nonterminal<'nt>
    | RSeq of Regexp<'t, 'nt> * Regexp<'t, 'nt>
    | RAlt of Regexp<'t, 'nt> * Regexp<'t, 'nt>
    | RStar of Regexp<'t, 'nt>

module Regexp =

    let rec nullable (r: Regexp<'t, 'nt>) : bool =
        match r with
        | REps
        | RStar _ -> true
        | REmpty
        | RTerm _
        | RNonterm _ -> false
        | RAlt(l, r) -> nullable l || nullable r
        | RSeq(l, r) -> nullable l && nullable r

    let rec derive (r: Regexp<'t, 'nt>) (sym: RsmSymbol<'t, 'nt>) : Regexp<'t, 'nt> =
        let mkAlt l r =
            match l, r with
            | REmpty, _ -> r
            | _, REmpty -> l
            | RAlt(a, b), x
            | x, RAlt(a, b) when a = x || b = x -> RAlt(a, b)
            | l, r when l = r -> l
            | l, r -> RAlt(l, r)

        match r with
        | REmpty -> REmpty
        | REps -> REmpty
        | RTerm t ->
            match sym with
            | RsmSymbol.RTerm t' when t = t' -> REps
            | _ -> REmpty
        | RNonterm nt ->
            match sym with
            | RsmSymbol.RNonterm nt' when nt = nt' -> REps
            | _ -> REmpty
        | RSeq(hd, tl) ->
            let newHead = derive hd sym
            let headNullable = nullable hd

            match newHead, headNullable with
            | REmpty, false -> REmpty
            | REps, false -> tl
            | REmpty, true -> derive tl sym
            | REps, true -> mkAlt tl (derive tl sym)
            | _, false -> RSeq(newHead, tl)
            | _, true -> mkAlt (RSeq(newHead, tl)) (derive tl sym)
        | RAlt(l, r) -> mkAlt (derive l sym) (derive r sym)
        | RStar rp ->
            let dr = derive rp sym

            match dr with
            | REps -> RStar rp
            | REmpty -> REmpty
            | _ -> RSeq(dr, RStar rp)

    let rec symbols (r: Regexp<'t, 'nt>) : RsmSymbol<'t, 'nt> list =
        match r with
        | REps
        | REmpty -> []
        | RTerm t -> [ RsmSymbol.RTerm t ]
        | RNonterm nt -> [ RsmSymbol.RNonterm nt ]
        | RStar rp -> symbols rp
        | RAlt(l, r)
        | RSeq(l, r) -> symbols l @ symbols r

    let rec toString
        (termPrinter: Terminal<'t> -> string)
        (nontermPrinter: Nonterminal<'nt> -> string)
        (r: Regexp<'t, 'nt>)
        : string =
        match r with
        | REps -> "eps"
        | REmpty -> "∅"
        | RTerm t -> termPrinter t
        | RNonterm nt -> nontermPrinter nt
        | RSeq(l, r) ->
            "("
            + toString termPrinter nontermPrinter l
            + " "
            + toString termPrinter nontermPrinter r
            + ")"
        | RAlt(l, r) ->
            toString termPrinter nontermPrinter l
            + " | "
            + toString termPrinter nontermPrinter r
        | RStar rp -> "(" + toString termPrinter nontermPrinter rp + ")*"


/// EBNF token type for tokenizer-based parsing.
[<RequireQualifiedAccess>]
type private EbnfToken =
    | Ident of string
    | Bar
    | Star
    | Plus
    | Quest
    | LParen
    | RParen
    | Arrow
    | Eps
    | EOL


module EbnfParser =

    let private tokenize (text: string) : EbnfToken list list =
        let lines = text.Split('\n', StringSplitOptions.None)

        lines
        |> Array.choose (fun line ->
            let trimmed = line.Trim()

            if trimmed.Length = 0 then
                None
            else
                let tokens = ResizeArray<EbnfToken>()

                let mutable i = 0

                while i < trimmed.Length do
                    if trimmed.[i] = ' ' || trimmed.[i] = '\t' then
                        i <- i + 1
                    elif trimmed.[i] = '|' then
                        tokens.Add EbnfToken.Bar
                        i <- i + 1
                    elif trimmed.[i] = '*' then
                        tokens.Add EbnfToken.Star
                        i <- i + 1
                    elif trimmed.[i] = '+' then
                        tokens.Add EbnfToken.Plus
                        i <- i + 1
                    elif trimmed.[i] = '?' then
                        tokens.Add EbnfToken.Quest
                        i <- i + 1
                    elif trimmed.[i] = '(' then
                        tokens.Add EbnfToken.LParen
                        i <- i + 1
                    elif trimmed.[i] = ')' then
                        tokens.Add EbnfToken.RParen
                        i <- i + 1
                    elif trimmed.[i] = '-' && i + 1 < trimmed.Length && trimmed.[i + 1] = '>' then
                        tokens.Add EbnfToken.Arrow
                        i <- i + 2
                    elif Char.IsLetter trimmed.[i] then
                        let mutable j = i

                        while j < trimmed.Length
                              && (Char.IsLetter trimmed.[j] || Char.IsDigit trimmed.[j] || trimmed.[j] = '_') do
                            j <- j + 1

                        let ident = trimmed.Substring(i, j - i)

                        if ident = "eps" then
                            tokens.Add EbnfToken.Eps
                        else
                            tokens.Add(EbnfToken.Ident ident)

                        i <- j
                    else
                        failwithf "Unexpected character '%c' at position %d" trimmed.[i] i

                Some(tokens |> Seq.toList))
        |> Array.toList

    let private parseTokens (lines: EbnfToken list list) : (Nonterminal<string> * Regexp<string, string>) list =
        let sym token =
            match token with
            | EbnfToken.Ident s ->
                if Char.IsUpper s.[0] then
                    RNonterm(Nonterminal s)
                else
                    RTerm(Terminal s)
            | EbnfToken.Eps -> REps
            | _ -> failwithf "Expected symbol, got %A" token

        let rec parsePostfix (tokens: EbnfToken list) : Regexp<string, string> * EbnfToken list =
            let atom, rest = parseAtom tokens

            match rest with
            | EbnfToken.Star :: r -> RStar atom, r
            | EbnfToken.Plus :: r -> RSeq(atom, RStar atom), r
            | EbnfToken.Quest :: r -> RAlt(atom, REps), r
            | _ -> atom, rest

        and parseAtom (tokens: EbnfToken list) : Regexp<string, string> * EbnfToken list =
            match tokens with
            | EbnfToken.LParen :: rest ->
                let inner, rest2 = parseAlt rest

                match rest2 with
                | EbnfToken.RParen :: rest3 -> inner, rest3
                | _ -> failwith "Expected ')' after '('"
            | EbnfToken.Eps :: rest -> REps, rest
            | EbnfToken.Ident s :: rest ->
                let r = sym (EbnfToken.Ident s)
                r, rest
            | _ -> failwithf "Unexpected token %A in atom" (if tokens.IsEmpty then "EOF" else string tokens.Head)

        and parseSeq (tokens: EbnfToken list) : Regexp<string, string> * EbnfToken list =
            let first, rest = parsePostfix tokens

            match rest with
            | (EbnfToken.Ident _ | EbnfToken.LParen | EbnfToken.Eps) :: _ ->
                let rest2, rem = parseSeq rest
                RSeq(first, rest2), rem
            | _ -> first, rest

        and parseAlt (tokens: EbnfToken list) : Regexp<string, string> * EbnfToken list =
            let first, rest = parseSeq tokens

            match rest with
            | EbnfToken.Bar :: rest2 ->
                let restAlt, rem = parseAlt rest2
                RAlt(first, restAlt), rem
            | _ -> first, rest

        let rec parseRules (lines: EbnfToken list list) : (Nonterminal<string> * Regexp<string, string>) list =
            match lines with
            | [] -> []
            | line :: restLines ->
                match line with
                | EbnfToken.Ident lhs :: EbnfToken.Arrow :: rhsTokens when Char.IsUpper lhs.[0] ->
                    let rhs, remaining = parseAlt rhsTokens

                    match remaining with
                    | [] -> (Nonterminal lhs, rhs) :: parseRules restLines
                    | tok :: _ -> failwithf "Unexpected token %A after rule" tok
                | _ -> failwithf "Invalid rule format: %A" line

        parseRules lines

    /// Parse EBNF text into a list of (nonterminal, regexp) pairs.
    let parseEbnf (text: string) : (Nonterminal<string> * Regexp<string, string>) list = text |> tokenize |> parseTokens

    /// Parse an EBNF file.
    let parseEbnfFile (path: string) : (Nonterminal<string> * Regexp<string, string>) list =
        File.ReadAllText path |> parseEbnf

    /// Group rules by nonterminal, joining right-hand sides with alternative.
    let groupRules
        (rules: (Nonterminal<string> * Regexp<string, string>) list)
        : Map<Nonterminal<string>, Regexp<string, string>> =
        rules
        |> List.groupBy fst
        |> List.map (fun (nt, rhsList) ->
            let combined = rhsList |> List.map snd |> List.reduce (fun a b -> RAlt(a, b))

            (nt, combined))
        |> Map.ofList


module RsmBuilder =

    let private buildBlockDfa (nt: Nonterminal<string>) (regexp: Regexp<string, string>) : RsmBlock<string, string> =
        let alphabet = Regexp.symbols regexp |> List.distinct |> Set.ofList
        let stateMap = System.Collections.Generic.Dictionary<Regexp<string, string>, int>()
        let mutable transitions: (int * RsmSymbol<string, string> * int) list = []
        let mutable stateList: Regexp<string, string> list = []

        let getStateId (r: Regexp<string, string>) =
            match stateMap.TryGetValue r with
            | true, id -> id
            | false, _ ->
                let id = stateList.Length
                stateList <- r :: stateList
                stateMap.[r] <- id
                id

        let startId = getStateId regexp
        let stack = System.Collections.Generic.Stack<Regexp<string, string>>()
        stack.Push regexp

        while stack.Count > 0 do
            let state = stack.Pop()

            for sym in alphabet do
                let deriv = Regexp.derive state sym

                match deriv with
                | REmpty -> ()
                | _ ->
                    if not (stateMap.ContainsKey deriv) then
                        stack.Push deriv

                    let fromId = stateMap.[state]
                    let toId = getStateId deriv
                    transitions <- (fromId, sym, toId) :: transitions

        let finalStates =
            stateMap
            |> Seq.choose (fun kvp -> if Regexp.nullable kvp.Key then Some kvp.Value else None)
            |> Set.ofSeq

        let dfa =
            Dfa.fromTransitions [ 0 .. stateList.Length - 1 ] transitions startId finalStates

        { Nonterminal = nt; Dfa = dfa }

    let buildRSMWithStart (grouped: Map<Nonterminal<string>, Regexp<string, string>>) (startNt: Nonterminal<string>) : RSM<string, string> =
        if Map.isEmpty grouped then
            invalidArg (nameof grouped) "Grammar must contain at least one rule"

        let firstNt = startNt

        let blocks =
            grouped |> Map.toList |> List.map (fun (nt, regexp) -> buildBlockDfa nt regexp)

        let totalStates = blocks |> List.sumBy (fun b -> Dfa.stateCount b.Dfa)

        let transitions = Matrix.init totalStates totalStates None
        let stateInfo = Array.zeroCreate<RsmStateInfo<string>> totalStates
        let blockStart = System.Collections.Generic.Dictionary<Nonterminal<string>, int>()
        let mutable finalStates = Set.empty<int>
        let mutable offset = 0

        for block in blocks do
            let dfa = block.Dfa
            let localSize = Dfa.stateCount dfa
            blockStart.[block.Nonterminal] <- offset + dfa.StartState

            for localState in 0 .. localSize - 1 do
                let globalState = offset + localState
                let isFinal = Set.contains localState dfa.FinalStates

                stateInfo.[globalState] <-
                    { BlockNonterminal = block.Nonterminal
                      LocalState = localState
                      IsFinal = isFinal }

                if isFinal then
                    finalStates <- Set.add globalState finalStates

                for localTarget in 0 .. localSize - 1 do
                    match Matrix.get dfa.Transitions localState localTarget with
                    | Some labels -> Matrix.set transitions (offset + localState) (offset + localTarget) (Some labels)
                    | None -> ()

            offset <- offset + localSize

        { Transitions = transitions
          StateCount = totalStates
          StateInfo = stateInfo
          BlockStart = blockStart
          FinalStates = finalStates
          StartBlock = firstNt }

    let buildRSM (grouped: Map<Nonterminal<string>, Regexp<string, string>>) : RSM<string, string> =
        buildRSMWithStart grouped (grouped |> Map.keys |> Seq.head)

    let buildRSMFromText (text: string) : RSM<string, string> =
        let rules = EbnfParser.parseEbnf text
        let grouped = EbnfParser.groupRules rules

        let startNt =
            match rules with
            | (nt, _) :: _ -> nt
            | [] -> invalidArg (nameof text) "Grammar must contain at least one rule"

        buildRSMWithStart grouped startNt

    let buildRSMFromFile (path: string) : RSM<string, string> =
        File.ReadAllText path |> buildRSMFromText
