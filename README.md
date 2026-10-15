# FLPQ_sharp_edu

[![CI](https://github.com/FormalLanguageConstrainedPathQuerying/FLPQ_sharp_edu/actions/workflows/ci.yml/badge.svg)](https://github.com/FormalLanguageConstrainedPathQuerying/FLPQ_sharp_edu/actions/workflows/ci.yml)

Supplementary materials for the book on formal language constrained path querying
(FLPQ): reference implementations of the book's algorithms in F# (.NET 10.0).

## Quickstart

```sh
dotnet tool restore
dotnet build
```

Run the test suite:

```sh
dotnet test
```

Format the code (check-only variant: `dotnet fantomas . --check`):

```sh
dotnet fantomas .
```

## Documentation

The documentation is the map for navigating the project. Start at
[docs/main.md](docs/main.md), or jump directly to a page:

- [Project architecture](docs/project/architecture.md)
- [Coding conventions](docs/developer/guides/coding-conventions.md)
- [Quality standards](docs/developer/guides/quality-standards.md)
- [Documentation conventions](docs/developer/guides/documentation-conventions.md)
- [CLI usage](docs/user/cli.md)
