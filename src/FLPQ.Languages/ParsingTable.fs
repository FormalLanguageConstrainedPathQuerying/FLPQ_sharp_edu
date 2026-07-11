namespace FLPQ.Languages

open FLPQ.LinearAlgebra

/// Common table type for CYK and Valiant algorithms.
/// Each cell is a set of nonterminals that derive the corresponding substring.
type ParsingTable<'nt when 'nt: comparison> = Matrix<Set<Nonterminal<'nt>>>

/// Action in an LR/RNGLR parsing table, parameterized by the reduce payload type.
/// In classical LR: LRAction<int> (reduce by rule index).
/// In RNGLR: LRAction<Nonterminal<'nt>> (reduce by RSM block nonterminal).
/// Book references: sec:LR_parsing (classical LR), sec:CFPQ_RNGLR (RNGLR).
[<RequireQualifiedAccess>]
type LRAction<'a> =
    | Shift of int
    | Reduce of 'a
    | Accept
