# FLPQ_sharp_edu

Supplementary materials for the book on formal language constrained path querying (FLPQ).

Reference implementations of algorithms in F# (.NET 10.0).

## Building

```sh
dotnet tool restore
dotnet build
```

## Running Tests

```sh
dotnet test
```

## Code Formatting

```sh
dotnet fantomas .
```

To check formatting without modifying files:

```sh
dotnet fantomas . --check
```
