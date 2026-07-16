# Skill: debugging

## Debugging with Print Trace

When code hangs, times out, or produces wrong results, add trace markers to isolate the stuck step.

### Trace Marker Convention

Use `[TRACE] <location>: <message>` as the prefix:

```fsharp
// F#
System.Console.WriteLine "[TRACE] myFunc: start, depth={0}" depth
System.Console.WriteLine "[TRACE] myFunc: found result, yielding"
```

```python
# Python
print("[TRACE] detect_changes.py: found 3 modified files", file=sys.stderr)
```

**Rules:**
- Every trace line starts with `[TRACE]` — enables reliable grep filtering
- Remove all trace lines before committing
- Prefer unique location names for unambiguous filtering

### Test-Specific Trace

`System.Console.WriteLine` output is invisible in `dotnet test` by default. Use the `-l "console;verbosity=detailed"` logger flag to make it visible:

```bash
dotnet test <testproj> -l "console;verbosity=detailed" --filter "<filter>" > tmp/test-trace.txt 2>&1
```

Then analyze:

```bash
grep TRACE tmp/test-trace.txt
```

### Debugging Workflow

1. **Add trace markers** at suspect points in the code (function boundaries, loop iterations, conditional branches)
2. **Build**:
   ```bash
   dotnet build <proj> > tmp/build-output.txt 2>&1
   ```
3. **Run test with trace**:
   ```bash
   dotnet test <testproj> -l "console;verbosity=detailed" --filter "<filter>" > tmp/test-trace.txt 2>&1
   ```
4. **Analyze**:
   ```bash
   grep TRACE tmp/test-trace.txt
   ```
   Identify the last marker before the hang — the problem is in the code between that marker and the next expected marker.
5. **Fix** the issue, **remove traces**, re-test

### Common Pitfalls

| Symptom | Likely Cause | Debug Approach |
|---------|-------------|----------------|
| Test never starts ("Starting test execution" hangs) | Test discovery timeout | Check `dotnet build` first; use `--no-build` only after successful separate build |
| `Seq.head` never returns | Infinite seq or slow depth enumeration | Add trace inside seq generator and `childrenByDepth` |
| `for` loop appears to process forever | Seq is strict (forced), not lazy | Check for `List.collect`/`@` inside `seq { }` — replace with `Seq.collect`/`Seq.append` |

### Integration with Sub-Agents

When a sub-agent times out, check its captured output. If no trace markers appear, the issue is before the trace lines — add markers earlier in the call chain. If trace markers appear but stop at a specific point, the issue is at that point — narrow the markers.

Base directory for this skill: /home/gsv/Projects/FormalLanguageConstrainedReachability-LectureNotes/FLPQ_sharp_edu/.opencode/skills/debugging
Relative paths in this skill (e.g., scripts/, reference/) are relative to this base directory.
