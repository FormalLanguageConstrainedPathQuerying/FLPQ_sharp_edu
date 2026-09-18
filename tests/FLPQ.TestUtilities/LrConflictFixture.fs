namespace FLPQ.TestUtilities

open FLPQ.Languages

/// Grammar `S -> A; A -> S`: a unit production completes in the accept state, so the
/// accept state is not a singleton and the LR builders record a ReduceReduce conflict
/// (see AcceptStateConflicts in FLPQ.Languages.Tests).
module LrConflictFixture =

    /// The conflict grammar itself.
    let grammar = Grammar.parseGrammar "S -> A\nA -> S"

    /// The grammar augmented with a fresh start symbol `S'`.
    let augmented = LRAutomaton.augmentGrammar (Nonterminal "S'") grammar

    /// The LR(0) table of the augmented grammar (carries the ReduceReduce conflict).
    let lr0Table = LRParser.buildLR0Table augmented Symbol.Epsilon
