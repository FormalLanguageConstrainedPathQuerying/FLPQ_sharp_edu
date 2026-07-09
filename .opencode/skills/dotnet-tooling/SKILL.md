---
name: dotnet-tooling
description: Use when working with dotnet CLI: building, testing, formatting, linting, restoring, cleaning, creating projects/solutions, or collecting code coverage. Covers all common commands with flags and project-specific patterns.
---

# dotnet CLI

Root of dotnet CLI documentation: https://learn.microsoft.com/en-us/dotnet/core/tools/

## Solution and project management

- Manipulate `.slnx` file: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln
- Create new projects or solutions: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new

### .NET 10.0 `.slnx` Format

`dotnet new sln -n <name>` creates `<name>.slnx` by default in .NET 10.0, not the legacy `.sln` format. All `dotnet sln` commands work transparently with `.slnx` files.

### Test Project Setup

```bash
dotnet new xunit -lang F# -n <Name> -o <Path>
```

Default packages: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`.

Add FsCheck:
```bash
dotnet add package FsCheck.Xunit
```

This brings in `FsCheck` transitively.

## Dependencies

```bash
dotnet restore
```

Install NuGet packages: https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-dotnet-cli

## Build

```bash
dotnet build FLPQ.slnx -c Release   # Release mode
dotnet build FLPQ.slnx -c Debug     # Debug mode
```

## Clean

```bash
dotnet clean
```

## Format

```bash
dotnet fantomas .           # Format all F# sources
dotnet fantomas . --check   # Check formatting without modifying files
```

## Lint

```bash
# Per-project (use for subtask commits):
DOTNET_ROOT=/usr/lib/dotnet dotnet-fsharplint lint <project-path>

# Full solution (use before merge to dev):
DOTNET_ROOT=/usr/lib/dotnet dotnet-fsharplint lint FLPQ.slnx
```

Lint configuration: see the `quality-gates` skill.

Install linter locally:
```bash
dotnet tool install -g dotnet-fsharplint
```

## Tests

```bash
dotnet test                                 # Run all tests
dotnet test --filter <test identifier>      # Run specific test
```

- Use `[<Trait("Category", <category_name>)>]` and respective filters to create and run groups of tests
- For visualization: create tests that verify generated output compiles correctly (e.g., dot file via graphviz, TeX via lualatex)

## Code Coverage

### Installation

```bash
dotnet tool install dotnet-coverage
```

Invoked as `dotnet dotnet-coverage`.

### Collecting Coverage

Always collect coverage when running tests. Use together with `dotnet test`:

```bash
dotnet dotnet-coverage collect dotnet test FLPQ.slnx -o tmp/coverage.cobertura -f cobertura --nologo > tmp/coverage-output.txt 2>&1
```

Key points:
- Use `cobertura` format for XML output
- Coverage instruments ALL assemblies loaded during test execution (FSharp.Core, FsCheck, xunit, Microsoft internals, FLPQ.*)

See the `quality-gates` skill for coverage verification (> 80% gate).

## Prototyping

Use F# scripts and F# interactive:
```bash
dotnet fsi Script.fsx
```

## Output capture

All dotnet commands that produce significant output (build, test, format check, lint, coverage) MUST capture output to `tmp/`. See the `quality-gates` skill for the full output capture protocol.
