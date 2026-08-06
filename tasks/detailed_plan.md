# Task 246: Refactor LanguageRegistry

## S1: Rename all grammars semantically and distribute MiscTestGrammars

**Code:** `tests/FLPQ.TestUtilities/LanguageRegistry.fs`
**Tests:** None (registry change; existing tests break — fixed in S3)
**Docs:** None (S3 covers)

**Spec:**
Rename all grammars in existing languages:

| Language | Old name | New name |
|----------|---------|----------|
| Dyck1 | `grammar1` | `ambiguousEps` |
| Dyck1 | `grammar2` | `ambiguousWithConcat` |
| Dyck1 | `grammarSaSb_eps` | `leftRecursiveEps` |
| Dyck1 | `grammar_dyck_ebnf` | `ebnfStar` |
| APlus | `grammar3` | `rightRecursive` |
| APlus | `grammar4` | `leftRecursive` |
| APlus | `grammar5` | `ambiguousBinaryTernary` |
| APlus | `grammar11` | `viaNullableA` |
| APlus | `grammar12` | `explicitVariants` |
| APlus | `grammar14` | `ambiguousWithSingle` |
| APlus | `grammar_aplus_ebnf1` | `ebnfNaStar` |
| APlus | `grammar_aplus_ebnf2` | `ebnfAStarN` |
| AStar | `grammar13` | `ambiguous` |
| AStar | `grammar_aSa_eps` | `palindromeLike` |
| AStar | `grammar_astar_ebnf` | `ebnfNStar` |
| ArithExpr | `grammar6` | `ambiguous` |
| ArithExpr | `grammar7` | `leftAssoc` |
| ArithExpr | `grammar8` | `rightAssoc` |
| TwoTrackDyck | `grammar9` | `variantA` |
| TwoTrackDyck | `grammar10` | `variantB` |
| ANB | `grammar_aS_b` | `rightRecursive` |
| ANBN | `grammar_aSb_eps` | `classic` |
| AStarBStar | `grammarRightNullable` | `rightNullable` |
| SingleA | `grammarS2a` | `singleRule` |
| SingleAB | `grammarAB` | `singleRule` |
| EpsilonOnly | `grammarEps` | `singleRule` |
| EpsilonOnly | `grammarNtoEps` | `viaIntermediate` |
| EpsilonOnly | `grammarNNtoEps` | `viaBinary` |
| EpsilonOnly | `grammarNStarEps` | `viaKleeneStar` |
| EpsilonOnly | `grammarSSeps` | `selfRecursive` |
| EpsilonOnly | `grammarChainEps` | `viaChain` |
| EpsilonOnly | `grammarAltEps` | `viaAmbiguous` |
| EpsilonOnly | `grammarCascade` | `viaCascade` |
| AltAB | `grammar_alt_ab` | `alternation` |
| LL2Test | `ll2Grammar` | `k2` |
| LL3Test | `ll3Grammar` | `k3` |
| DualDyck | `grammar_dual_dyck` | `ebnfConcat` |
| OpExpr | `grammarOpExpr` | `rightAssoc` |

Distribute MiscTestGrammars entries:
- `grammar_aB_b` → SingleAB as `twoRule`
- `grammar_A_B_a` → SingleA as `unitChain`
- `grammar_long_chain` → SingleA as `longUnitChain`
- `grammar_N_A_a` → SingleA as `viaIntermediate` (with `unitChain` renamed to `shortUnitChain`)
- `grammar_aS_eps` → AStar as `rightRecursiveWithEps`
- `grammar_aSbS_no_eps` → Dyck1 as `singleRuleNoEps`
- `grammar_EEaddT` → ArithExpr as `simplified`
- Keep `grammar_SS_a_b`, `grammar_abc`, `grammar_AaBb`, `grammar_x_aA`, `grammar_A_a__S_b`, `grammar_ebnf_aa`, `grammar_ebnf_a_eps` — will be promoted to new languages in S2.

Create `TestInfraGrammars` language for the 5 remaining infrastructure grammars:
- `grammar_AB_BC_C` → `cnfAdjacent`
- `grammar_ABCDE` → `multiNontermWithEps`
- `grammar_aBcD` → `mixedTerminalsNonterminals`
- `grammar_a__eps` → `twoRuleWithEps`
- `grammar_aT_bE_c` → `mutualRecursion`

## S2: Create new languages for promoted grammars

**Code:** `tests/FLPQ.TestUtilities/LanguageRegistry.fs`
**Tests:** None (registry change only)
**Docs:** None (S3 covers)

**Spec:**
Create new languages, each with proper AcceptStrings, RejectStrings, GenString:

1. **DoubleA** ({aa}): `grammar_ebnf_aa` → `singleRule`. Accept: `[a;a]`. Reject: `[]`, `[a]`, `[a;a;a]`, `[b]`. Gen: `constantGen "a a"`.
2. **AOrEps** ({a, ε}): `grammar_ebnf_a_eps` → `ebnfAlt`. Accept: `[]`, `[a]`. Reject: `[a;a]`, `[b]`. Gen: `MyGen.elements [""; "a"]`.
3. **ABPlus** ({a,b}⁺): `grammar_SS_a_b` → `ambiguousConcat`. Accept: `[a]`, `[b]`, `[a;a]`, `[a;b]`, `[b;a]`, `[b;b]`. Reject: `[]`, `[c]`. Gen: from `abStringGen` filtered to non-empty.
4. **FourTerm** ({abcd}): `grammar_abc` → `singleRule`. Accept: `[a;b;c;d]`. Reject: `[]`, `[a]`, `[a;b]`, `[a;b;c]`, `[a;b;c;d;e]`. Gen: `constantGen "a b c d"`.
5. **MixedPairs** ({aabb}): `grammar_AaBb` → `mixedRule`. Accept: `[a;a;b;b]`. Reject: `[]`, `[a;a]`, `[b;b]`, `[a;b;a;b]`. Gen: `constantGen "a a b b"`.
6. **AX** ({ax}): `grammar_x_aA` → `startNotFirst`. Accept: `[a;x]`. Reject: `[]`, `[x;a]`, `[a]`, `[x]`, `[a;a;x]`. Gen: `constantGen "a x"`.
7. **SingleB** ({b}): `grammar_A_a__S_b` → `startFromA`. Accept: `[b]`. Reject: `[]`, `[a]`, `[b;b]`. Gen: `constantGen "b"`.

Remove `MiscTestGrammars` from `allLanguages`. Add all new languages. Add `TestInfraGrammars`.

## S3: Improve isEbnfText using Regexp AST walk

**Code:** `tests/FLPQ.TestUtilities/LanguageRegistry.fs`
**Tests:** None (existing grammars must still parse correctly)
**Docs:** None

**Spec:**
Replace the character-based `isEbnfText` with an AST-based check:
```fsharp
let private isRegexPureConcat (r: Regexp<string, string>) : bool =
    let rec check r =
        match r with
        | RTerm _ | RNonterm _ | REps | REmpty -> true
        | RSeq(l, r) -> check l && check r
        | RAlt _ | RStar _ -> false
    check r

let private isEbnfText (text: string) : bool =
    let isDefinitelyEbnf = text.Contains('+') || text.Contains('*') || text.Contains('?')
    if not isDefinitelyEbnf then false
    else
        try
            let rules = EbnfParser.parseEbnf text
            rules |> List.exists (fun (_, regex) -> not (isRegexPureConcat regex))
        with _ -> true
```
Logic:
- If text has `+`/`*`/`?` → potentially EBNF
- Parse with EBNF parser into Regexp ASTs
- Walk each rule's AST: if ANY contains RAlt or RStar → true EBNF
- If ALL are pure concatenation → false (any `+`/`*` were terminals)
- If EBNF parsing fails → true (malformed for EBNF, will fall through to CFG path anyway)

Remove `ebnfOperators` set (no longer needed since `+`/`*`/`?`/`|`/`(`/`)` are forbidden as terminals and will be handled by the AST check).

Remove parenthesis-wrapping logic in `grammarToEbnfText` (the `Set.contains t ebnfOperators` check) — these symbols are forbidden as terminals.

## S4: Update all test call sites for renamed grammars

**Code:** All test files referencing old grammar names
**Tests:** All existing tests must pass
**Docs:** None

**Spec:**
Search for all references to old grammar names across the codebase and update them. Key patterns:
- `Dyck1.Grammars[0]` → `Dyck1.Grammars[0]` (index unchanged for grammar1→ambiguousEps)
- `findGrammar Dyck1 "grammar1"` → `findGrammar Dyck1 "ambiguousEps"`
- Any direct name references
- Update golden reference files if grammar names appear in output
- Update CLI runner code if it references grammar names

## S5: Run full test suite and verify

**Code:** None (or golden file updates)
**Tests:** All 864+ tests must pass
**Docs:** Update `docs/developer/language-registry.md` if it exists

**Spec:**
- Build all projects
- Run all tests
- Fix any failures
- Verify golden files
- Commit and merge
