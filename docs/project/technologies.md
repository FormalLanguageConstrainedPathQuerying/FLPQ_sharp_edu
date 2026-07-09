# Third-Party Libraries and Tools

| Dependency | Role | Rationale |
|-----------|------|-----------|
| [Argu](https://fsprojects.github.io/Argu/) | CLI argument parsing | First-class F# DU-based argument declarations with built-in help generation — matches the project's type-first design philosophy |
| [FSharpPlus](https://fsprojects.github.io/FSharpPlus/) | `NonEmptyList`, `NonEmptySet` | Compile-time enforcement of non-emptiness invariants for grammars, RSMs, and automata — aligns with the "non-empty by type" convention in [coding conventions](../developer/guides/coding-conventions.md) |
| [FsCheck](https://fscheck.github.io/FsCheck/) | Property-based testing | Generates random inputs and shrinks counterexamples — provides statistical confidence in algorithm correctness without exhaustive enumeration |
| [xUnit](https://xunit.net/) | Unit testing framework | Standard .NET test framework with clean `[<Fact>]`/`[<Theory>]`/`[<Property>]` attribute model |
| [fantomas](https://fsprojects.github.io/fantomas/) | Code formatting | Deterministic F# formatting — eliminates style debates in code review and ensures diffs show only semantic changes |
| [nicematrix](https://ctan.org/pkg/nicematrix) | LaTeX matrix rendering | Renders structured matrices (CYK tables, Valiant tables, LL/LR parsing tables) with cell-level styling and highlights |
| [Graphviz](https://graphviz.org/) | Graph visualization | Renders automata and derivation trees as DOT graphs — industry-standard layout engine for directed graphs |
| [lualatex](https://www.tug.org/applications/pdftex/) | TeX compilation | Compiles generated TeX output into PDF for verification — required for graphdrawing-based Tikz layouts (LR automata) |

## Compile-time vs Run-time Dependencies

| Category | Example | When Required |
|----------|---------|---------------|
| NuGet packages | Argu, FSharpPlus, FsCheck, xUnit | Always (compile and test) |
| .NET local tools | `dotnet-fsharplint`, `dotnet-coverage`, `dotnet fantomas` | Quality gates only |
| External executables | `dot` (Graphviz), `lualatex` | Visualization tests and `--summary` CLI flag only |
