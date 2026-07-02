# Detailed Plan: Task 85 — Add input string generation for CYK and Valiant

## Goal

Generate an input string visualization file (`input.tex`) for CYK and Valiant algorithms (currently only LL/LR generate input.tex). Also update `run_viz.py` to include the input string in the merged TeX document.

## Changes

### 1. `src/FLPQ.Cli/Program.fs`

In `runCyk` (after line 63, before step loop):
```fsharp
let inputTex = TeXRenderer.inputRow string (inputTokens |> List.map (fun t -> T t)) -1
writeOutputFile (Path.Combine(outputDir, "input.tex")) inputTex
```

In `runValiant` (after line 89, before step loop):
```fsharp
let inputTex = TeXRenderer.inputRow string (inputTokens |> List.map (fun t -> T t)) -1
writeOutputFile (Path.Combine(outputDir, "input.tex")) inputTex
```

Note: `inputTokens` is already available as `Token list = Terminal<string> list`. We convert to `Symbol<string, string> list` by mapping each `Terminal t` to `T(Terminal t)`. Position `-1` means no cell is underlined (just shows the full input string).

### 2. `run_viz.py`

In `process_algorithm`, after the grammar sections (~line 172), add input string section:
```python
input_tex = viz_path / "input.tex"
if input_tex.exists():
    tex_source.append(r"\subsection*{Input String}")
    tex_source.append(r"\begin{center}")
    tex_source.append(r"\[")
    tex_source.append(input_tex.read_text(encoding="utf-8").strip())
    tex_source.append(r"\]")
    tex_source.append(r"\end{center}")
    tex_source.append("")
```

## Verification

- Run CYK and Valiant via CLI, verify `input.tex` is generated in output dir
- Run `run_viz.py --example`, verify input string appears in merged TeX
- Check that `input.tex` contains a one-row `pNiceMatrix` with all input tokens
