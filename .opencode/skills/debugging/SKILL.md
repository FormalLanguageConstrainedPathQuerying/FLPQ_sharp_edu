---
name: debugging
description: Use when debugging hangs, timeouts, or wrong results. The full guide ([TRACE] print-trace convention, dotnet test trace visibility, debugging workflow, common pitfalls such as seq laziness) lives in docs/developer/guides/debugging.md.
---

# Debugging

Before debugging a hang, timeout, or wrong result, read
[`docs/developer/guides/debugging.md`](/docs/developer/guides/debugging.md) —
it is the single source of truth for the `[TRACE]` print-trace marker
convention, making trace output visible under `dotnet test`, the add-trace →
build → run → analyze → fix workflow, common pitfalls (test discovery timeout,
infinite seq, strict vs lazy seq), and using traces when a sub-agent times out.
