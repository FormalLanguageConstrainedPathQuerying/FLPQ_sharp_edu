namespace FLPQ.Languages

/// Common tokenizer for all parsing algorithms.
/// Input strings use space-separated terminals (supports multi-character terminals).
module Tokenizer =

    /// Tokenize input into individual terminal strings.
    /// Terminals are separated by spaces in the input.
    let tokenizeStrings (input: string) : string list =
        if System.String.IsNullOrWhiteSpace input then
            []
        else
            input.Split(' ', System.StringSplitOptions.RemoveEmptyEntries) |> Array.toList

    /// Tokenize input into a list of Grammar symbols (terminals).
    let tokenize (input: string) : Symbol<string, string> list =
        tokenizeStrings input |> List.map (fun t -> T(Terminal t))

    /// Tokenize input into a list of Terminal values.
    let tokenizeTerminals (input: string) : Terminal<string> list =
        tokenizeStrings input |> List.map Terminal
