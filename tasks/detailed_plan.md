# Task 207: Deduplicate `regexToDfa` / `buildBlockDfa`

## Analysis

Both functions implement the Brzozowski derivatives DFA construction algorithm:
- `RsmBuilder.buildBlockDfa` (EbnfParser.fs:268-311): builds `DFA<RsmSymbol<string,string>, int>` over RSM symbols
- `regexToDfa` (RPQTests.fs:237-290): builds `DFA<string, int>` over plain string terminals

The core loop (state exploration via derivatives, transition collection, final state detection) is identical. Differences are only in alphabet extraction and symbol type.

## Subtasks

- [ ] S1 - Extract generic `buildDfaFromRegex` function into the `Regexp` module in `EbnfParser.fs`. Signature: `alphabet:'sym list -> deriveFn:(Regexp<'t,'nt> -> 'sym -> Regexp<'t,'nt>) -> regexp:Regexp<'t,'nt> -> DFA<'sym, int>`. Contains the shared Brzozowski construction loop.
- [ ] S2 - Refactor `RsmBuilder.buildBlockDfa` to delegate to `Regexp.buildDfaFromRegex` with `RsmSymbol` alphabet and `Regexp.derive` function.
- [ ] S3 - Replace `regexToDfa` in `RPQTests.fs` with a call to `Regexp.buildDfaFromRegex` using terminal-only alphabet and wrapped derivative function.
- [ ] S4 - Build and run all tests. Verify zero failures.
