# Detailed Plan: Task 118 — Large-Input Stress Tests

## Goal
Add large-input stress tests across all algorithm families. Tests must verify termination within reasonable time and correctness against small-input reference. FsCheck generators for stress tests should use higher bounds (50–200).

## Approach

### Cross-validation strategy
For correctness verification without a trusted oracle:
- **CYK vs Modified Valiant**: both should agree on acceptance for same grammar+input
- **NFA→DFA**: DFA output equivalence — verify deterministic behavior
- **RPQ**: Belyanin ≡ Arroyuelo for regex-derived DFAs
- **Matrix**: verify algebraic properties (associativity, distributivity)
- **LR**: verify table structure (state count, action/goto tables non-empty)

### Test organization
- New files per test project (no modification to existing test files to avoid merge conflicts)
  - `tests/FLPQ.Languages.Tests/StressTests.fs`
  - `tests/FLPQ.RPQ.Tests/StressTests.fs`
  - `tests/FLPQ.LinearAlgebra.Tests/StressTests.fs`
- All stress tests tagged with `[<Trait("Category", "Stress")>]`
- Mix of `[<Fact>]` (deterministic) and `[<Property>]` (FsCheck) tests

## Steps

### 1. Add stress generators to Generators.fs

**StressStringGenerator**: strings of length 50-200 for the grammar `S -> a S b | eps`
  - Generate `a^n b^n` pattern with n in 25-100 (total length 50-200)

**StressNfaGenerator**: NFAs with 30-100 states
  - Generate linear NFA chain with random transitions

**StressRpqGenerator**: RPQ test data with 50-200 vertices, simple regex

**StressMatrixGenerator**: large square matrices 100-200×100-200

### 2. Create StressTests.fs in each test project

#### `tests/FLPQ.Languages.Tests/StressTests.fs`
- **[Fact] CYK large input**: `S -> a S b | eps` with `a^50 b^50` (100 tokens). Verify CYK accepts.
- **[Fact] Valiant large input**: Same grammar+input, verify Valiant accepts.
- **[Property] CYK vs Valiant equivalence**: For random CNF-compatible strings of length 10-30, CYK and Valiant must agree.
- **[Fact] NFA→DFA large**: Build NFA with 50 states in a chain, convert to DFA, verify DFA size and determinism.
- **[Property] toDfa correctness**: For random NFA with up to 20 states, verify DFA correctly classifies random strings.
- **[Fact] LR large automaton**: Use expression grammar with many precedence levels (10+), verify automaton has 100+ states.

#### `tests/FLPQ.RPQ.Tests/StressTests.fs`
- **[Fact] RPQ large graph**: Build graph with 100 vertices (chain + random edges), run all three RPQ algorithms, verify agreement.
- **[Property] RPQ large equivalence**: For random graphs with 20-50 vertices, Belyanin ≡ Arroyuelo for regex DFA.

#### `tests/FLPQ.LinearAlgebra.Tests/StressTests.fs`
- **[Fact] mxm large matrices**: Multiply 200×200 matrices, verify result has correct dimensions.
- **[Fact] kron large matrices**: Kronecker product of 50×50 matrices produces 2500×2500 result.
- **[Fact] map2 large matrices**: Element-wise operation on 200×200 matrices.

### 3. Update fsproj files
Add `StressTests.fs` Compile items to:
- `tests/FLPQ.Languages.Tests/FLPQ.Languages.Tests.fsproj`
- `tests/FLPQ.RPQ.Tests/FLPQ.RPQ.Tests.fsproj`
- `tests/FLPQ.LinearAlgebra.Tests/FLPQ.LinearAlgebra.Tests.fsproj`

### 4. Verify
- `dotnet build`
- `dotnet test --filter "Category=Stress"` — stress tests pass within reasonable time
- `dotnet fantomas .`

## Design Decisions

### Grammar for CYK/Valiant stress
Use the well-known grammar `S -> a S b | eps` (balanced a's and b's). CNF form:
```
S  -> A B
S  -> A S1
S1 -> S B
A  -> a
B  -> b
S  -> eps
```
This grammar accepts strings of the form `a^n b^n`. For stress testing, generate input `a^50 b^50` (100 tokens). Both CYK and Valiant should accept.

### Grammar for LR stress
Use expression grammar with many precedence levels:
```
E1 -> E1 + E2 | E2
E2 -> E2 * E3 | E3
E3 -> E3 ^ E4 | E4
... (repeat for 10+ levels)
E10 -> ( E1 ) | x
```
This produces 100+ LR(0) states due to shift/reduce chains.

### NFA for toDfa stress
Build a deterministic linear chain NFA: states 0→1→2→...→99 with transitions on "a". This converts to DFA with 100 states (no blowup since it's already deterministic). But we can make it non-deterministic by adding epsilon transitions and branching.

Actually, for stress testing the subset construction, we want the DFA to potentially have many states. A linear chain NFA is already deterministic and produces same-size DFA. To truly stress it, we'd need an NFA with ambiguity. But performance-wise, the chain is a good test.

Better approach: NFA with 50 states where each state has 2 epsilon transitions to random states. This can produce DFA blowup.

### Time limits
- Individual stress tests should complete within 30 seconds
- Property-based tests with MaxTest=5-10 to keep total time reasonable

### FsCheck bounds
- Stress generators: 50-200 for dimensions/lengths
- Property-based stress tests: 20-50 for dimensions (still larger than normal 5-15)
