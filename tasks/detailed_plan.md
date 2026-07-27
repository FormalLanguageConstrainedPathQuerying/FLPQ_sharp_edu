# Detailed Plan: Task 203 — TreatWarningsAsErrors

## Task Description

Add solution level `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` to turn all warnings to errors. Compile all projects, fix all problems.

## Warning Inventory (21 unique warnings)

| File | Line | Code | Count | Description |
|------|------|------|-------|-------------|
| LRParser.fs | 299, 349 | FS0064 | 4 | `'t` and `'nt` constrained to `string` via `Grammar.eoiSymbol` |
| PathIndex.fs | 255, 322 | FS0064 | 2 | `'nt` constrained to `string` via `sprintf` with `Nonterminal<'nt>` |
| Sppf.fs | 381, 645 | FS0064 | 2 | `'nt` constrained to `string` via `sprintf` / literal string |
| RnglrStepVisualizer.fs | 107, 189, 219 | FS0064 | 3 | `'nt` constrained to `string` via pattern match on `Nonterminal<'nt>` |
| GllTypes.fs | 123 | FS0020 | 1 | Implicitly ignored `(int * GssEdgeInfo) list` |
| GllTests.fs | 8 lines | FS0020 | 8 | Implicitly ignored `bool` from `accepts` calls |
| TokenizerTests.fs | 108 | FS0686 | 1 | Explicit type arg on non-generic function |

## Subtasks

### S1: Create Directory.Build.props with TreatWarningsAsErrors

**Code:** New file `Directory.Build.props` at project root
**Tests:** None
**Docs:** None

Create `Directory.Build.props`:
```xml
<Project>
  <PropertyGroup>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

### S2: Fix FS0064 in LRParser.fs (lines 299, 349)

**Root cause:** `Grammar.eoiSymbol` has type `Symbol<string, string>`. Used inside generic functions `buildLR0Table` and `buildSLR1Table`, constraining `'t` and `'nt` to `string`.

**Fix:** Change function signatures from `Grammar<'t, 'nt>` to `Grammar<string, string>`. All callers already use `Grammar<string, string>`.

### S3: Fix FS0064 in PathIndex.fs (lines 255, 322)

**Root cause:** `sprintf` with `Nonterminal<'nt>` value directly.

**Fix:** Pattern-match to extract inner value before sprintf:
- Line 255: `(Nonterminal nt)` already bound above, use `nt` in sprintf (it's already the inner value)
- Line 322: `(Nonterminal ntName)` already bound, use `ntName` (already the inner value)

Wait - looking at the code again, line 255 uses `nt` which is from `PathIndexEntry.PEpsilonNonterminal nt` where `nt : Nonterminal<'nt>`. Need to destructure.
Line 322 uses `ntName` which is already destructured from `(Nonterminal ntName) = originalStart`.

Actually the warning says line 255 col 37 - that's the `nt` in the sprintf format argument position. The variable `nt` has type `Nonterminal<'nt>` and sprintf treats it as a string. Need to destructure.

For line 322, `ntName` is already the inner value (string), but the warning points to col 27 which is `(vertexCount - 1)`. Wait, that doesn't make sense. Let me re-read... The warning says "The type variable 'nt has been constrained to be type 'string" at line 322 col 27. Col 27 on line 322 is `ntName`. But `ntName` was extracted from `(Nonterminal ntName) = originalStart` where `originalStart : Nonterminal<'nt>`. So `ntName : 'nt`. When passed to sprintf, it constrains `'nt` to `string`.

Fix: Both need explicit string conversion. Since these are validation functions used only with string grammars, add `[<EntryPoint>]`-style constraint or use `box/unbox` pattern. Actually, the simplest fix is to add a type annotation `'nt = string` to these validation functions since they're only called with string grammars.

Better approach: Add `when 'nt : equality` constraint isn't enough. The real fix is to either:
a) Constrain these specific functions to `'nt = string`
b) Use a printer function parameter

Since these are test/validation functions, option (a) is appropriate.

### S4: Fix FS0064 in Sppf.fs (lines 381, 645)

**Line 381:** `sprintf "...%s" i ntName` where `ntName : 'nt`. Same pattern - validation function.
**Line 645:** `Node(Nonterminal "$root", childList)` - literal string `"$root"` constrains `'nt` to `string`.

Fix for 381: Constrain validation function to `'nt = string` or destructure `Nonterminal<'nt>`.
Fix for 645: This is in `enumerateTrees` which creates a root node. The `"$root"` is a sentinel name. Since this function is only called with string-based SPPFs, constrain appropriately.

### S5: Fix FS0064 in RnglrStepVisualizer.fs (lines 107, 189, 219)

**Line 107:** `let (Nonterminal ntName) = item.BlockNonterminal` then `sprintf "%s / %d" ntName item.RsmState` - `ntName : 'nt` passed to sprintf.
**Lines 189, 219:** These are at function call sites where type inference flows back.

Fix: The `lrAutomatonToDot` function uses `sprintf` with the extracted `'nt` value. Since this is a printer/visualizer, it already takes printer functions (`terminals`, `nonterminals`). Use the `nonterminals` printer instead of sprintf directly.

### S6: Fix FS0020 in GllTypes.fs (line 123)

**Issue:** List expression result implicitly ignored in `pop` function.
**Fix:** The first list comprehension at line 123 is dead code (followed by a second identical one at line 132). Remove the duplicate.

### S7: Fix FS0020 in GllTests.fs (8 lines)

**Issue:** `accepts` returns `bool` that's implicitly ignored in property tests.
**Fix:** Change `accepts rsm input` to `accepts rsm input |> ignore` or use as the property result directly.

Looking at the pattern:
```fsharp
try
    accepts (TestHelpers.grammarToRsm grammarG1) input
    true
with _ ->
    false
```
The `accepts` call result is ignored, then `true` is returned. Since `accepts` already validates tree leaves internally and returns `bool`, the fix is to use `accepts ... || true` or just `accepts ...` (since if it returns true, the property passes; if false, we still return true because the string might legitimately be rejected).

Actually, looking more carefully: these are property tests where the contract is "if accepted, tree yield matches input". The `accepts` function already validates this internally. So the property should just be `accepts rsm input` - it returns true if accepted (and tree validated), false if rejected. Either outcome is valid for the property.

Fix: Replace `accepts ... ; true` with `accepts ... || true` to use the bool result.

Wait, that's not right either. The intent is: "for any string, either it's accepted (with correct tree) or rejected - both are fine". So the property should always return true unless an exception occurs. The `accepts` call is the validation. Fix: `let _ = accepts ... ; true` or `accepts ... |> ignore ; true`.

Simplest fix: `accepts rsm input |> ignore`

### S8: Fix FS0686 in TokenizerTests.fs (line 108)

**Issue:** `Tokenizer.terminalsToSymbols<int, string> []` - explicit type args on a function that doesn't declare them.
**Fix:** Remove explicit type args and use a let binding for inference:
```fsharp
let result : Symbol<int, string> list = Tokenizer.terminalsToSymbols []
Assert.Empty(result)
```

### S9: Verify clean build, run tests, merge

Build with TreatWarningsAsErrors, verify 0 warnings. Run all tests. Code review. Merge to dev.
