namespace FLPQ.Languages

open FLPQ.LinearAlgebra

/// Common table type for CYK and Valiant algorithms.
/// Each cell is a set of nonterminals that derive the corresponding substring.
type ParsingTable<'nt when 'nt: comparison> = Matrix<Set<Nonterminal<'nt>>>
