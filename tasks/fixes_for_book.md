# Book Fixes and Improvements

## ArroyueloRPQ.transitiveClosure Bug (Task 115)

**Problem**: The `transitiveClosure` function in `ArroyueloRPQ.fs` used a flawed repeated-squaring approach that only accumulated powers M¹, M², M⁴, M⁸, ..., skipping M³ and other odd powers. This caused incorrect results for paths of length 3 in graphs with 6+ vertices. For example, with regex `(a|b)*` on a graph where vertex 3 reached vertex 5 via path 3→0→2→5 (length 3), the algorithm incorrectly reported no reachability.

**Root cause**: The algorithm computed `result = I + M + M² + M⁴ + M⁸ + ...` (adding only powers of 2: M, M², M⁴, M⁸...) instead of the full transitive closure `I + M + M² + M³ + ... + Mⁿ⁻¹`.

**Fix**: Changed `transitiveClosure` to compute `(I+M)^n` by squaring `(I+M)` iteratively: start with `A = I+M`, then square `A` ceil(log₂(n)) times. After k iterations, `A = (I+M)^(2ᵏ)`. When 2ᵏ ≥ n, A contains all paths of length ≤ n-1.

**Impact**: All Arroyuelo RPQ results must be verified after this fix. The bug only manifests for graphs with paths of length 3+ where no alternative shorter path exists.

**Book reference**: Chapter 11, 03_Arroyuelo.tex. The book should describe the correct transitive closure algorithm: compute `(I+M)^n` by squaring `(I+M)` ceil(log₂(n)) times.

## buildLR0/buildLR1 Already Deduplicated (Task 119)

**Note**: Task 119 mentions deduplicating `buildLR0`/`buildLR1` (~60 duplicated lines) by extracting a common BFS framework. The implementation already has the `buildLR` helper function (parameterized by closure and item construction) which is a thin shared layer over the generic `buildAutomaton` function. `buildLR0` and `buildLR1` are already thin wrappers (~20 lines each) over `buildLR`. No further deduplication is needed here.