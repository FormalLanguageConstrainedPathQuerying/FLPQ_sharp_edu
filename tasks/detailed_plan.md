# Detailed Plan: Tasks 001-002 — Infrastructure Init and Generic Matrix Type

## Task 1: Init Infrastructure

### 1.1 Solution and Project Creation
- Create solution file: `FLPQ.sln` via `dotnet new sln -n FLPQ`
- Create F# class library: `src/FLPQ.Core/FLPQ.Core.fsproj` via `dotnet new classlib -lang F# -n FLPQ.Core -o src/FLPQ.Core`
- Create F# xUnit test project: `tests/FLPQ.Core.Tests/FLPQ.Core.Tests.fsproj` via `dotnet new xunit -lang F# -n FLPQ.Core.Tests -o tests/FLPQ.Core.Tests`
- Add projects to solution:
  - `dotnet sln add src/FLPQ.Core/FLPQ.Core.fsproj`
  - `dotnet sln add tests/FLPQ.Core.Tests/FLPQ.Core.Tests.fsproj`

### 1.2 NuGet Dependencies
- Test project references: FsCheck (property-based testing), coverlet.collector (coverage), Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio
- Core project references: none initially (standard library only)

### 1.3 Tool Manifest
- Create `dotnet new tool-manifest` for local tools
- Install `dotnet fantomas` as local tool: `dotnet tool install fantomas`

### 1.4 CI Configuration
- Create `.github/workflows/ci.yml`
- Matrix strategy: ubuntu-latest, windows-latest, macos-latest
- Steps:
  1. Checkout code
  2. Setup .NET 10.0
  3. Restore dependencies
  4. Build Debug
  5. Build Release
  6. Check formatting (fantomas --check)
  7. Run tests (Debug)
  8. Run tests (Release)

### 1.5 Directory Structure
```
FLPQ_sharp_edu/
├── FLPQ.sln
├── .config/
│   └── dotnet-tools.json
├── .github/
│   └── workflows/
│       └── ci.yml
├── src/
│   └── FLPQ.Core/
│       ├── FLPQ.Core.fsproj
│       └── Library.fs          (initial file, remove default)
├── tests/
│   └── FLPQ.Core.Tests/
│       ├── FLPQ.Core.Tests.fsproj
│       └── Tests.fs            (empty placeholder)
├── tasks/...
├── docs/...
└── data/...
```

## Task 2: Generic Matrix Type

### 2.1 Type Definition
```fsharp
type Matrix<'a> = { rows: int; cols: int; data: 'a[,] }
```
Plain wrapper around a 2D array with explicit row/column counts.

### 2.2 Module Functions (in `Matrix.fs`)

#### map: ('a -> 'b) -> Matrix<'a> -> Matrix<'b>
Element-wise transformation. Creates new matrix preserving dimensions.

#### map2: ('a -> 'b -> 'c) -> Matrix<'a> -> Matrix<'b> -> Matrix<'c>
Element-wise binary operation on two matrices. Requires matching dimensions (throw exception if mismatch).

#### transpose: Matrix<'a> -> Matrix<'a>
Swap rows and columns. New matrix with cols×rows dimensions.

#### create: int -> int -> (int -> int -> 'a) -> Matrix<'a>
Create matrix of specified dimensions using a generator function `f row col -> value`.

#### init: int -> int -> 'a -> Matrix<'a>
Create matrix filled with a constant value.

#### zeroCreate: int -> int -> Matrix<'a>  (with INum or use provided zero)
Actually simpler: `init rows cols value` covers this. We'll have `create`, `init`, and helper `zeroCreate` using Unchecked.defaultof or accepting a zero parameter.

Per task spec: "Helper functions for matrices creation and initialization." — let's provide:
- `create rows cols f` — generator function
- `init rows cols value` — constant fill
- `ofArray2D arr` — from existing 2D array
- `rows m` / `cols m` — dimension accessors

### 2.3 TeX Printing
```fsharp
val toTeX: 
    ?showRowNumbers: bool -> 
    ?showColNumbers: bool -> 
    cellPrinter: ('a -> string) -> 
    matrix: Matrix<'a> -> 
    string
```
Uses nicematrix package:
```tex
\begin{pNiceMatrix}
  a_{11} & a_{12} & \cdots & a_{1n} \\
  a_{21} & a_{22} & \cdots & a_{2n} \\
  \vdots & \vdots & \ddots & \vdots \\
  a_{m1} & a_{m2} & \cdots & a_{mn}
\end{pNiceMatrix}
```
If `showRowNumbers=true`, use first-column option; if `showColNumbers=true`, use first-row option.

Actually, nicematrix supports `first-col` and `first-row` options or we can add the row/col numbers manually. Simpler approach: manually prepend row numbers as first column/virtual cells, and col numbers as first row.

Let's use nicematrix with external row/col labels (using `\Block` for row header cells and regular cells for column headers plus a corner block), since nicematrix `first-row` and `first-col` handle formatting. Or we can just add them manually to the content.

Simpler: generate additional row/column in the matrix for labels. Use `\textbf{}` or special formatting for labels.

### 2.4 Property-Based Tests

File: `tests/FLPQ.Core.Tests/MatrixTests.fs`

1. **map2 commutativity**: For commutative operation (+), `map2 (+) a b` = `map2 (+) b a`
2. **Repeated transpose is identity**: `transpose (transpose m)` = `m`
3. **Sequence of maps = single map with composition**: `map f (map g m)` = `map (f << g) m`
4. **map2 dimensions**: result has same dimensions as input matrices
5. **transpose dimensions**: rows and cols are swapped
6. **create correctness**: `create r c f` yields matrix where element at (i,j) = f i j

### 2.5 TeX Printing Test
Generate TeX for a sample matrix and verify it:
- Contains correct `\begin{pNiceMatrix}` / `\end{pNiceMatrix}`
- Contains correct cell content
- When showRowNumbers/showColNumbers enabled, includes extra row/column
- The generated TeX can be processed (optional: check structure only)

## Implementation Order
1. Create solution and projects
2. Install tools and packages  
3. Create CI config
4. Verify CI pipeline works (build, format check, tests)
5. Implement Matrix type and module
6. Write property-based tests
7. Verify all tests pass
8. Format code
9. Update documentation

## Decisions
- **Project name**: `FLPQ.Core` — core library for the book's algorithms
- **Test project**: `FLPQ.Core.Tests` — mirrors src structure
- **.NET version**: 10.0 (already installed, per AGENTS.md requirement)
- **Testing framework**: xUnit (standard for .NET) + FsCheck for property-based tests
- **Formatting**: fantomas via local tool manifest
- **Matrix type**: Struct record wrapping 2D array with explicit dimensions fields
- **TeX printing**: Use nicematrix package `pNiceMatrix` environment, optionally with `first-row`/`first-col` options for row/column numbering
