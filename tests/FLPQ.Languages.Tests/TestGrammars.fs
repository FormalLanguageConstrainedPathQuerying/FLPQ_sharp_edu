module TestGrammars

open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

/// Helper: space-separated string list from tokenized string lists.
let private acceptStrToSpace (ss: (string list) list) = ss |> List.map (String.concat " ")

/// Helper: look up a grammar by name in a language.
let private grammarByName (lang: Language) (name: string) =
    lang.Grammars |> List.find (fun g -> g.Name = name)

let private dyck1 = LanguageRegistry.Dyck1
let private aplus = LanguageRegistry.APlus
let private astar = LanguageRegistry.AStar
let private expr = LanguageRegistry.ArithExpr
let private twoTrack = LanguageRegistry.TwoTrackDyck
let private anb = LanguageRegistry.ANB
let private anbn = LanguageRegistry.ANBN
let private aStarBStar = LanguageRegistry.AStarBStar
let private singleA = LanguageRegistry.SingleA
let private singleAB = LanguageRegistry.SingleAB
let private epsilonOnly = LanguageRegistry.EpsilonOnly

// ---- Dyck1 ----

let grammar1 = (grammarByName dyck1 "grammar1").Grammar
let augGrammar1 = (grammarByName dyck1 "grammar1").AugmentedGrammar

let grammar2 = (grammarByName dyck1 "grammar2").Grammar
let augGrammar2 = (grammarByName dyck1 "grammar2").AugmentedGrammar

let grammarSaSb_eps = (grammarByName dyck1 "grammarSaSb_eps").Grammar

let grammar1Accept = dyck1.AcceptStrings |> acceptStrToSpace
let grammar1Reject = dyck1.RejectStrings |> acceptStrToSpace

// ---- APlus ----

let grammar3 = (grammarByName aplus "grammar3").Grammar
let augGrammar3 = (grammarByName aplus "grammar3").AugmentedGrammar

let grammar4 = (grammarByName aplus "grammar4").Grammar
let augGrammar4 = (grammarByName aplus "grammar4").AugmentedGrammar

let grammar5 = (grammarByName aplus "grammar5").Grammar
let augGrammar5 = (grammarByName aplus "grammar5").AugmentedGrammar

let grammar11 = (grammarByName aplus "grammar11").Grammar
let grammar12 = (grammarByName aplus "grammar12").Grammar
let grammar14 = (grammarByName aplus "grammar14").Grammar

let grammar3Accept = aplus.AcceptStrings |> acceptStrToSpace
let grammar3Reject = aplus.RejectStrings |> acceptStrToSpace

// ---- AStar ----

let grammar13 = (grammarByName astar "grammar13").Grammar

// ---- ArithExpr ----

let grammar6 = (grammarByName expr "grammar6").Grammar
let augGrammar6 = (grammarByName expr "grammar6").AugmentedGrammar

let grammar7 = (grammarByName expr "grammar7").Grammar
let augGrammar7 = (grammarByName expr "grammar7").AugmentedGrammar

let grammar8 = (grammarByName expr "grammar8").Grammar
let augGrammar8 = (grammarByName expr "grammar8").AugmentedGrammar

let exprAccept = expr.AcceptStrings |> acceptStrToSpace
let exprReject = expr.RejectStrings |> acceptStrToSpace

// ---- TwoTrackDyck ----

let grammar9 = (grammarByName twoTrack "grammar9").Grammar
let augGrammar9 = (grammarByName twoTrack "grammar9").AugmentedGrammar

let grammar10 = (grammarByName twoTrack "grammar10").Grammar
let augGrammar10 = (grammarByName twoTrack "grammar10").AugmentedGrammar

let grammar9Accept = twoTrack.AcceptStrings |> acceptStrToSpace
let grammar9Reject = twoTrack.RejectStrings |> acceptStrToSpace
let grammar10Accept = grammar9Accept
let grammar10Reject = grammar9Reject

// ---- Individual grammars ----

let grammar_aS_b = (grammarByName anb "grammar_aS_b").Grammar

let grammar_aSb_eps = (grammarByName anbn "grammar_aSb_eps").Grammar

let grammarRightNullable = (grammarByName aStarBStar "grammarRightNullable").Grammar

let grammarS2a = (grammarByName singleA "grammarS2a").Grammar
let grammarAB = (grammarByName singleAB "grammarAB").Grammar

let grammarEps = (grammarByName epsilonOnly "grammarEps").Grammar
let grammarNtoEps = (grammarByName epsilonOnly "grammarNtoEps").Grammar
let grammarNNtoEps = (grammarByName epsilonOnly "grammarNNtoEps").Grammar
let grammarNStarEps = (grammarByName epsilonOnly "grammarNStarEps").Grammar
let grammarSSeps = (grammarByName epsilonOnly "grammarSSeps").Grammar
let grammarChainEps = (grammarByName epsilonOnly "grammarChainEps").Grammar
let grammarAltEps = (grammarByName epsilonOnly "grammarAltEps").Grammar
let grammarCascade = (grammarByName epsilonOnly "grammarCascade").Grammar

let grammar_aSa_eps = (grammarByName astar "grammar_aSa_eps").Grammar

// ---- LL(k) test grammars ----

let ll2Grammar = (grammarByName LanguageRegistry.LL2Test "ll2Grammar").Grammar
let ll3Grammar = (grammarByName LanguageRegistry.LL3Test "ll3Grammar").Grammar

let grammar_alt_ab = (grammarByName LanguageRegistry.AltAB "grammar_alt_ab").Grammar

// ---- EBNF grammar entries (Text is the canonical source; Grammar, AugmentedGrammar, and Rsm are derived from it) ----

let grammar_dyck_ebnf = LanguageRegistry.Dyck1.Grammars[3]
let grammar_aplus_ebnf1 = LanguageRegistry.APlus.Grammars[6]
let grammar_aplus_ebnf2 = LanguageRegistry.APlus.Grammars[7]
let grammar_astar_ebnf = LanguageRegistry.AStar.Grammars[2]
let grammar_dual_dyck = LanguageRegistry.DualDyck.Grammars[0]
