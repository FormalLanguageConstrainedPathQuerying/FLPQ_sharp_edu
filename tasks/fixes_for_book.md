# Book Fixes and Improvements

## Regexp.derive / mkAlt Unbounded Derivative Closure (Task 264 gate unblock)

**Problem**: `Regexp.buildDfaFromRegex` never terminated for certain regular expressions, e.g. `(a*)(aa)*`. The DFA construction looped forever generating ever-larger derivative states until `Regexp.derive` stack-overflowed the test host (observed as a flaky "Test Run Aborted / Stack overflow" in FLPQ.RPQ.Tests).

**Root cause**: The `mkAlt` helper inside `derive` only deduplicated one level of alternation nesting. For expressions like `(a*)(aa)*`, each derivation step produced `RAlt(RAlt(...), newAlt)` — a strictly deeper, syntactically distinct expression (the duplicate alternative was buried below the nesting depth that dedup could see). The set of syntactic derivatives is therefore infinite for such shapes, so the state-discovery loop in `buildDfaFromRegex` never terminates. Exhaustive search over all 2,881,200 regexes of depth ≤ 3 over {a, b, eps} found 202 shapes with unbounded closures; after the fix all have finite closures (max 13 states, 45 nodes).

**Fix**: `mkAlt` now flattens nested alternations into a flat, duplicate-free list of alternatives before recombining (`flattenAlts` + `List.distinct`). Flattening makes the syntactic derivative closure finite: duplicates at any nesting depth are removed, so repeated derivation stabilizes. Semantics are unchanged (flattening and dedup are language-preserving).

**Book reference**: If the book presents the Brzozowski-derivative DFA construction with a one-level-dedup `mkAlt`, it should be updated to flatten alternations; otherwise the presented algorithm does not terminate for regexes like `(a*)(aa)*`.

## ArroyueloRPQ.transitiveClosure Bug (Task 115)

**Problem**: The `transitiveClosure` function in `ArroyueloRPQ.fs` used a flawed repeated-squaring approach that only accumulated powers M¹, M², M⁴, M⁸, ..., skipping M³ and other odd powers. This caused incorrect results for paths of length 3 in graphs with 6+ vertices. For example, with regex `(a|b)*` on a graph where vertex 3 reached vertex 5 via path 3→0→2→5 (length 3), the algorithm incorrectly reported no reachability.

**Root cause**: The algorithm computed `result = I + M + M² + M⁴ + M⁸ + ...` (adding only powers of 2: M, M², M⁴, M⁸...) instead of the full transitive closure `I + M + M² + M³ + ... + Mⁿ⁻¹`.

**Fix**: Changed `transitiveClosure` to compute `(I+M)^n` by squaring `(I+M)` iteratively: start with `A = I+M`, then square `A` ceil(log₂(n)) times. After k iterations, `A = (I+M)^(2ᵏ)`. When 2ᵏ ≥ n, A contains all paths of length ≤ n-1.

**Impact**: All Arroyuelo RPQ results must be verified after this fix. The bug only manifests for graphs with paths of length 3+ where no alternative shorter path exists.

**Book reference**: Chapter 11, 03_Arroyuelo.tex. The book should describe the correct transitive closure algorithm: compute `(I+M)^n` by squaring `(I+M)` ceil(log₂(n)) times.

## buildLR0/buildLR1 Already Deduplicated (Task 119)

**Note**: Task 119 mentions deduplicating `buildLR0`/`buildLR1` (~60 duplicated lines) by extracting a common BFS framework. The implementation already has the `buildLR` helper function (parameterized by closure and item construction) which is a thin shared layer over the generic `buildAutomaton` function. `buildLR0` and `buildLR1` are already thin wrappers (~20 lines each) over `buildLR`. No further deduplication is needed here.