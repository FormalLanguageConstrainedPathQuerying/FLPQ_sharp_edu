namespace FLPQ.Core

open System
open System.IO

/// A terminal symbol, wrapping a user-defined type 't.
type Terminal<'t> = Terminal of 't

/// A nonterminal symbol, wrapping a user-defined type 'nt.
type Nonterminal<'nt> = Nonterminal of 'nt

/// A grammar symbol: either a terminal or a nonterminal.
type Symbol<'t, 'nt> =
    | T of Terminal<'t>
    | N of Nonterminal<'nt>

/// A production rule: left-hand side nonterminal produces a sequence of symbols.
type Rule<'t, 'nt> =
    { lhs: Nonterminal<'nt>
      rhs: Symbol<'t, 'nt> list }

/// A context-free grammar consisting of production rules and a designated start nonterminal.
type Grammar<'t, 'nt> =
    { rules: Rule<'t, 'nt> list
      start: Nonterminal<'nt> }

module Grammar =

    let private classifyToken (token: string) : Symbol<string, string> =
        if System.Char.IsUpper(token[0]) then
            N(Nonterminal token)
        else
            T(Terminal token)

    let private parseLine (line: string) : Rule<string, string> =
        let parts = line.Split("->", 2, StringSplitOptions.None)

        if parts.Length <> 2 then
            invalidArg (nameof line) $"Invalid rule format: {line}"

        let lhs = Nonterminal(parts[0].Trim())
        let rhsStr = parts[1].Trim()

        let rhs =
            if rhsStr = "eps" then
                []
            else
                rhsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                |> Array.toList
                |> List.map classifyToken

        { lhs = lhs; rhs = rhs }

    /// Parse a grammar from BNF text.
    /// One rule per line. Empty lines are ignored.
    /// Format: `<nonterm> -> <symbols>` or `<nonterm> -> eps`.
    /// The start nonterminal is the left-hand side of the first rule.
    let parseGrammar (text: string) : Grammar<string, string> =
        let rules =
            text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            |> Array.map (fun s -> s.Trim())
            |> Array.filter (fun s -> s.Length > 0)
            |> Array.map parseLine
            |> Array.toList

        if rules.IsEmpty then
            invalidArg (nameof text) "Grammar must contain at least one rule"

        { rules = rules
          start = rules.Head.lhs }

    /// Parse a grammar from a .bnf file.
    let parseGrammarFromFile (path: string) : Grammar<string, string> = File.ReadAllText(path) |> parseGrammar
