namespace FLPQ.Languages

open FLPQ.LinearAlgebra

/// Common table type for CYK and Valiant algorithms.
/// Each cell is a set of nonterminals that derive the corresponding substring.
type ParsingTable<'nt when 'nt: comparison> = Matrix<Set<Nonterminal<'nt>>>

/// A single entry in an enriched parsing table that stores data for BasicSPPF construction.
/// Fields: (nonterminal, splitPoint_k, productionIndex).
/// For terminal rules, splitPoint_k is the position of the terminal character.
/// For binary rules, splitPoint_k is the index where the left child ends and right child begins.
[<Struct>]
type SppfParsingEntry<'nt when 'nt: comparison> =
    { Nt: Nonterminal<'nt>
      SplitPoint: int
      ProdIdx: int }

/// Enriched parsing table where each cell stores a set of SPPF entries.
/// Shared by CYK and Valiant algorithms for BasicSPPF construction.
type SppfParsingTable<'nt when 'nt: comparison> = Matrix<Set<SppfParsingEntry<'nt>>>

/// Action in an LR/RNGLR parsing table, parameterized by the reduce payload type.
/// In classical LR: LRAction<int> (reduce by rule index).
/// In RNGLR: LRAction<Nonterminal<'nt>> (reduce by RSM block nonterminal).
/// Book references: sec:LR_parsing (classical LR), sec:CFPQ_RNGLR (RNGLR).
[<RequireQualifiedAccess>]
type LRAction<'a> =
    | Shift of int
    | Reduce of 'a
    | Accept
