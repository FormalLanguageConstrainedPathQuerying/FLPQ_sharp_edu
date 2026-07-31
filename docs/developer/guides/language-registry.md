# Language Registry

**Tags:** testing, grammar, language-registry, guide
**Kind:** guide

> **Abstract:** Single source of truth for all test languages, grammars, accept/reject strings, and string generators used in parsing algorithm tests. Grammars are grouped strictly by formal language equivalence — only grammars generating the same formal language are grouped together. Each grammar is annotated with manually-verified properties (left-recursion, ambiguity, epsilon, CNF). No algorithm compatibility bindings — the developer uses grammar properties combined with knowledge of the algorithm's restrictions to decide which grammars are suitable for testing. The corresponding typed F# module is `FLPQ.TestUtilities.LanguageRegistry`.

## Contents

- [How to Use](#how-to-use)
- [Language Entries](#language-entries)
  - [Dyck1 (balanced a/b)](#dyck1-balanced-ab)
  - [APlus (a^+)](#aplus-a)
  - [AStar (a*)](#astar-a)
  - [ArithExpr (arithmetic expressions)](#arithexpr-arithmetic-expressions)
  - [TwoTrackDyck (ab↔c, ax↔y)](#twotrackdyck-abc-axy)
  - [ANB (a^n b)](#anb-an-b)
  - [ANBN (a^n b^n)](#anbn-an-bn)
  - [ASTAR_BSTAR (a^m b^n)](#astar_bstar-am-bn)
  - [SingleA ({a})](#singlea-a)
  - [SingleAB ({ab})](#singleab-ab)
  - [EpsilonOnly ({ε})](#epsilononly-)
- [Grammar Properties Reference](#grammar-properties-reference)
- [Generator Reference](#generator-reference)
- [See Also](#see-also)

## How to Use

1. **Find a language** whose grammars are compatible with the algorithm you are testing.
2. **Check grammar properties** against the algorithm's known restrictions.
   - **LL(k)**: requires `HasDirectLeftRecursion = false`; may require `IsAmbiguous = false`
   - **LR(k)**: requires no shift-reduce/reduce-reduce conflicts; `IsAmbiguous = false` for deterministic
   - **CYK**: requires grammar in CNF, or a CNF conversion step
   - **Valiant**: requires CNF (internally converted)
   - **GLL/RNGLR**: no restrictions
3. **Use the registry in test code**:
   ```fsharp
   open FLPQ.TestUtilities

   let lang = LanguageRegistry.Dyck1
   for g in lang.Grammars do
       // CFG-based parsers (CYK, Valiant, LL, LR): use g.Grammar, g.AugmentedGrammar
       // RSM-based parsers (GLL, RNGLR): use g.Rsm
       // test with lang.AcceptStrings, lang.RejectStrings
   ```
4. **For property-based tests**, use `lang.GenString` to generate random strings.
5. **For cross-algorithm equivalence**, pick a language with multiple grammars (e.g., Dyck1 has 3, APlus has 6).

## Language Entries

### Dyck1 (balanced a/b)

**Formal language**: L = {w ∈ {a,b}* | every prefix of w has at least as many a's as b's, and |w|_a = |w|_b}

**F# access**: `LanguageRegistry.Dyck1`

**Grammars**:

| Name | Definition | LR | DLR | Amb | ε | CNF | Notes |
|------|-----------|-----|-----|-----|---|-----|-------|
| `grammar1` | S → a S b S \| ε | no | no | yes | yes | no | ambiguous Dyck-1; LL(1)-compatible |
| `grammar2` | S → a S b \| S S \| ε | yes | yes | yes | yes | no | S→SS creates direct left-recursion; not LL, not LR |
| `grammarSaSb_eps` | S → S a S b \| ε | yes | yes | yes | yes | no | left-recursive Dyck-1; S→S a S b has direct left-recursion |

**Accept strings**:
| String |
|--------|
| `a b a b` |
| `a b` |
| (empty) |
| `a a b b` |
| `a a b a b b` |

**Reject strings**:
| String |
|--------|
| `a a` |
| `b b` |
| `a b b` |
| `a b b a` |
| `b` |
| `a` |
| `a b a b a` |

**Generator**: `abStringGen` (produces random a/b-sequences as space-separated strings)

---

### APlus (a^+)

**Formal language**: L = {a^n | n ≥ 1}

**F# access**: `LanguageRegistry.APlus`

**Grammars**:

| Name | Definition | LR | DLR | Amb | ε | CNF | Notes |
|------|-----------|-----|-----|-----|---|-----|-------|
| `grammar3` | S → a S \| a | no | no | no | no | no | right-recursive; LL(1), SLR(1)-compatible |
| `grammar4` | S → S a \| a | yes | yes | no | no | no | left-recursive |
| `grammar5` | S → S S \| S S S \| a | yes | yes | yes | no | no | ambiguous left-recursive |
| `grammar11` | S → a a A \| a A; A → a A \| ε | no | no | yes | no | no | ambiguous via nullable A |
| `grammar12` | S → a \| a a \| a a A \| a a a A; A → a A \| ε | no | no | yes | no | no | ambiguous with explicit short-string productions |
| `grammar14` | S → a \| S S \| S S S | yes | yes | yes | no | no | ambiguous left-recursive |

**Accept strings**: `a`, `a a`, `a a a a`, `a a a a a`

**Reject strings**: (empty), `b`

**Generator**: `aStringGen`

---

### AStar (a*)

**Formal language**: L = {a^n | n ≥ 0}

**F# access**: `LanguageRegistry.AStar`

**Grammars**:

| Name | Definition | LR | DLR | Amb | ε | CNF | Notes |
|------|-----------|-----|-----|-----|---|-----|-------|
| `grammar13` | S → ε \| a a S \| a S | no | no | yes | yes | no | ambiguous a*; multiple derivations for a^n |

**Accept strings**: (empty), `a`, `a a`, `a a a`, `a a a a`

**Reject strings**: `b`, `a b`, `a a b`, `a a a b`, `a b a a`

**Generator**: `aStringGen`

---

### ArithExpr (arithmetic expressions)

**Formal language**: Arithmetic expressions over terminals {x, +, *, (, )}

**F# access**: `LanguageRegistry.ArithExpr`

**Grammars**:

| Name | Definition | LR | DLR | Amb | ε | CNF | Notes |
|------|-----------|-----|-----|-----|---|-----|-------|
| `grammar6` | S → x \| S + S \| S * S \| (S) | yes | yes | yes | no | no | ambiguous; no precedence; not LL, not LR |
| `grammar7` | E → E + T \| T; T → T * F \| F; F → (E) \| x | yes | yes | no | no | no | unambiguous; left-assoc; SLR(1)-compatible |
| `grammar8` | E → T + E \| T; T → F * T \| F; F → (E) \| x | no | no | no | no | no | unambiguous; right-assoc |

**Accept strings**: `x`, `( x )`, `( x ) * x`, `x + x`, `x + x * x`, `x * ( x + x )`, `( x * ( x + x ) )`

**Reject strings**: (empty), `( )`, `+ x`, `x +`, `x + ( )`

**Generator**: `exprStringGen` (recursive expression generator)

---

### TwoTrackDyck (ab↔c, ax↔y)

**Formal language**: Two independent Dyck-style tracks; one matches `(ab)^n c^n`, the other matches `(ax)^n y^n`, arbitrarily interleaved.

**F# access**: `LanguageRegistry.TwoTrackDyck`

**Grammars**:

| Name | Definition | LR | DLR | Amb | ε | CNF | Notes |
|------|-----------|-----|-----|-----|---|-----|-------|
| `grammar9` | S → S1 \| S2; S1 → a b S c \| ε; S2 → a x S y \| ε | no | no | yes | yes | no | ambiguous (empty via S1 or S2) |
| `grammar10` | S → S1 \| S2; S1 → a b S c; S → ε; S2 → a x S y | no | no | yes | yes | no | same language; different epsilon handling |

**Accept strings**: (empty), `a b c`, `a x y`, `a b a b c c`, `a x a x y y`, `a x a b c y`, `a b a x y c`

**Reject strings**: `a`, `x`, `y`, `c`, `a x c`, `a b y`, `a x a b`, `a b a x y`, `a x a b c`, `a x a b y`

**Generator**: `abcdxyStringGen`

---

### ANB (a^n b)

**Formal language**: L = {a^n b | n ≥ 0}

**F# access**: `LanguageRegistry.ANB`

**Grammars**:

| Name | Definition | LR | DLR | Amb | ε | CNF | Notes |
|------|-----------|-----|-----|-----|---|-----|-------|
| `grammar_aS_b` | S → a S \| b | no | no | no | no | no | right-recursive; unambiguous |

**Accept strings**: `b`, `a b`, `a a b`, `a a a b`

**Reject strings**: (empty), `a`, `a a`, `b a`

**Generator**: `abStringGen`

---

### ANBN (a^n b^n)

**Formal language**: L = {a^n b^n | n ≥ 0}

**F# access**: `LanguageRegistry.ANBN`

**Grammars**:

| Name | Definition | LR | DLR | Amb | ε | CNF | Notes |
|------|-----------|-----|-----|-----|---|-----|-------|
| `grammar_aSb_eps` | S → a S b \| ε | no | no | no | yes | no | classic; unambiguous; LL(1)-compatible |

**Accept strings**: (empty), `a b`, `a a b b`, `a a a b b b`

**Reject strings**: `a`, `b`, `a a b`, `a b b`, `a b a b`

**Generator**: `abStringGen`

---

### ASTAR_BSTAR (a^m b^n)

**Formal language**: L = {a^m b^n | m,n ≥ 0}

**F# access**: `LanguageRegistry.ASTAR_BSTAR`

**Grammars**:

| Name | Definition | LR | DLR | Amb | ε | CNF | Notes |
|------|-----------|-----|-----|-----|---|-----|-------|
| `grammarRightNullable` | S → A B; A → a A \| ε; B → b B \| ε | no | no | no | yes | no | a^m b^n; right-nullable A and B; unambiguous |

**Accept strings**: (empty), `a`, `b`, `a b`, `a a b`, `a b b`

**Reject strings**: `b a`, `a b a`

**Generator**: `abStringGen`

---

### SingleA ({a})

**Formal language**: L = {a}

**F# access**: `LanguageRegistry.SingleA`

**Grammars**:

| Name | Definition | LR | DLR | Amb | ε | CNF | Notes |
|------|-----------|-----|-----|-----|---|-----|-------|
| `grammarS2a` | S → a | no | no | no | no | no | trivial single-terminal grammar |

**Accept strings**: `a`

**Reject strings**: (empty), `b`, `a a`, `a b`

**Generator**: `constantGen "a"`

---

### SingleAB ({ab})

**Formal language**: L = {ab}

**F# access**: `LanguageRegistry.SingleAB`

**Grammars**:

| Name | Definition | LR | DLR | Amb | ε | CNF | Notes |
|------|-----------|-----|-----|-----|---|-----|-------|
| `grammarAB` | S → a b | no | no | no | no | no | trivial two-terminal grammar |

**Accept strings**: `a b`

**Reject strings**: (empty), `a`, `b`, `b a`, `a a b`

**Generator**: `constantGen "a b"`

---

### EpsilonOnly ({ε})

**Formal language**: L = {ε}. The empty-string-only language.

**F# access**: `LanguageRegistry.EpsilonOnly`

**Grammars**:

| Name | Definition | LR | DLR | Amb | ε | CNF | Notes |
|------|-----------|-----|-----|-----|---|-----|-------|
| `grammarEps` | S → ε | no | no | no | yes | yes | simplest epsilon grammar |
| `grammarNtoEps` | S → N; N → ε | no | no | no | yes | no | epsilon via intermediate nonterminal |
| `grammarNNtoEps` | S → N N; N → ε | no | no | no | yes | yes | epsilon via nullable binary; CNF-compatible |
| `grammarNStarEps` | S → N*; N → ε | no | no | no | yes | no | epsilon via Kleene star of nullable; uses EBNF |
| `grammarSSeps` | S → S S \| ε | yes | yes | yes | yes | yes | epsilon via self-recursive binary; ambiguous; CNF-compatible |
| `grammarChainEps` | S → A B; A → C D; B → D C; D → ε; C → ε | no | no | no | yes | no | epsilon via chain of nullable nonterminals |
| `grammarAltEps` | S → A \| B; A → C D; B → D C; D → ε; C → ε | no | no | yes | yes | no | ambiguously epsilon via alternative paths |
| `grammarCascade` | S → A; A → B; B → ε | no | no | no | yes | no | epsilon via cascade of unit productions |

**Accept strings**: (empty)

**Reject strings**: `a`, `b`, `a b`, `a a`, `b b`

**Generator**: `constantGen ""`

## Grammar Properties Reference

| Column | Meaning |
|--------|---------|
| **LR** (HasLeftRecursion) | ∃A ⇒⁺ Aβ — some nonterminal can derive itself as the leftmost symbol in a derivation. Incompatible with LL parsers. |
| **DLR** (HasDirectLeftRecursion) | ∃A → Aα — a nonterminal appears as the leftmost symbol of its own production RHS. Stricter than LR; incompatible with LL. |
| **Amb** (IsAmbiguous) | ∃w with ≥2 distinct parse trees in this grammar. Incompatible with deterministic LL/LR parsers. |
| **ε** (HasEpsilon) | ε ∈ L(G) — the empty string is in the language. |
| **CNF** (IsInCnf) | Every production is A → B C or A → a (plus S → ε allowed if start). Required by CYK; internally converted for Valiant. |
| **RSM** (IsRsmDerived) | Grammar was derived from an RSM (via EBNF → RSM → RsmToGrammar.convert round-trip). The CFG may not be suitable for CYK testing — skip these entries when iterating over `Grammars` in CFG-based parser tests. |

**Important**: These properties are manually verified by the developer. They are not automatically detected. Undecidable properties (like ambiguity in the general case) are stated based on analysis of the specific grammar.

## Generator Reference

| Generator name | Language | What it produces | FsCheck type |
|---------------|----------|-----------------|-------------|
| `abStringGen` | Dyck1, ANB, ANBN, ASTAR_BSTAR | Random sequences of `a` and `b` | `Gen<string>` |
| `aStringGen` | APlus, AStar | Sequences of `a` tokens, space-separated | `Gen<string>` |
| `exprStringGen` | ArithExpr | Recursively-generated arithmetic expressions | `Gen<string>` |
| `abcdxyStringGen` | TwoTrackDyck | Random sequences from {a,b,c,d,x,y} | `Gen<string>` |
| `constantGen "a"` | SingleA | Always produces `"a"` | `Gen<string>` |
| `constantGen "a b"` | SingleAB | Always produces `"a b"` | `Gen<string>` |
| `constantGen ""` | EpsilonOnly | Always produces `""` | `Gen<string>` |

All generators produce space-separated strings. Use `TestHelpers.stringToTerminals` to tokenize them.

The FsCheck `Arbitrary` wrapper types (e.g., `AbStringGenerators`) in `Generators.fs` mirror these generators for `[<Property>]` attribute-based tests.

## See Also

- [Tests-Writer Skill](../../.opencode/skills/tests-writer/SKILL.md) — how to use the registry in tests
- [Test Categories](test-categories.md) — xUnit trait categories
- [F# Language Registry module](../../tests/FLPQ.TestUtilities/LanguageRegistry.fs) — typed implementation
- [Coding Conventions](coding-conventions.md)
