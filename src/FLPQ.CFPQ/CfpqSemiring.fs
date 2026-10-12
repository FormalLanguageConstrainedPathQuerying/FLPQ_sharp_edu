namespace FLPQ.CFPQ

open FSharpPlus.Data

module CfpqSemiring =

    /// A semiring for Azimov's matrix-based CFPQ algorithm (book Chapter 12, sec:CFPQ_MatrixBased).
    /// Matrix cells are `'elem option`, where None means "no path". The operations parameterize the
    /// fixed point: `Add` combines two candidate values for the same cell, `Mult k x y` combines a
    /// left value and a right value through the middle vertex k, and `InitPath i j` is the value of
    /// a direct (height-1) path from i to j.
    type Semiring<'elem> =
        { Add: 'elem -> 'elem -> 'elem
          Mult: int -> 'elem -> 'elem -> 'elem
          InitPath: int -> int -> 'elem }

    /// Cell-level addition: None is the additive identity.
    let addCell (s: Semiring<'elem>) (a: 'elem option) (b: 'elem option) : 'elem option =
        match a, b with
        | None, x -> x
        | x, None -> x
        | Some x, Some y -> Some(s.Add x y)

    /// Cell-level multiplication through middle vertex k: any missing side yields no path.
    let multCell (s: Semiring<'elem>) (k: int) (a: 'elem option) (b: 'elem option) : 'elem option =
        match a, b with
        | Some x, Some y -> Some(s.Mult k x y)
        | _ -> None

    /// Boolean reachability semiring: the answer is the start nonterminal's matrix itself.
    let ReachabilitySemiring: Semiring<bool> =
        { Add = (||)
          Mult = fun _k x y -> x && y
          InitPath = fun _i _j -> true }

    /// A path index for the singlePath semiring (LAGraph PR #8): `Middle` is the middle vertex of a
    /// two-part derivation and `Height` is the height of the derivation tree. A direct derivation
    /// (one terminal edge or epsilon) has height 1.
    type PathIndex = { Middle: int; Height: int }

    /// Single-path semiring (LAGraph PR #8): `Add` keeps the minimum height (ties: smaller middle),
    /// `Mult k x y` joins through k with height max+1, and `InitPath i j` is the direct path {i, 1}.
    let SinglePathSemiring: Semiring<PathIndex> =
        { Add =
            fun x y ->
                if x.Height < y.Height || (x.Height = y.Height && x.Middle <= y.Middle) then
                    x
                else
                    y
          Mult =
            fun k x y ->
                { Middle = k
                  Height = max x.Height y.Height + 1 }
          InitPath = fun i _j -> { Middle = i; Height = 1 } }

    /// An element of the allPaths semiring (LAGraph PR #9): `Base` marks a direct derivation only,
    /// and `Splits` is the sorted, deduplicated non-empty list of middle vertices; if the list
    /// contains `allPathsSentinel`, a direct derivation exists as well.
    type AllPathsElem =
        | Base
        | Splits of NonEmptyList<int>

    /// The sentinel middle vertex marking a direct derivation (never a real vertex index).
    let allPathsSentinel: int = System.Int32.MaxValue

    /// All-paths semiring (LAGraph PR #9): `Add` is the ordered union (Base absorbs into Splits as
    /// the sentinel), `Mult k _ _` records only the middle vertex, and `InitPath` is Base.
    let AllPathsSemiring: Semiring<AllPathsElem> =
        { Add =
            fun x y ->
                match x, y with
                | Base, Base -> Base
                | Base, Splits l
                | Splits l, Base ->
                    Splits(NonEmptyList.ofList (List.sort (allPathsSentinel :: NonEmptyList.toList l) |> List.distinct))
                | Splits l1, Splits l2 ->
                    Splits(
                        NonEmptyList.ofList (
                            List.sort (NonEmptyList.toList l1 @ NonEmptyList.toList l2) |> List.distinct
                        )
                    )
          Mult = fun k _x _y -> Splits(NonEmptyList.singleton k)
          InitPath = fun _i _j -> Base }
