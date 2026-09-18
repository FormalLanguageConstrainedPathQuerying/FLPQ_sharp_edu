namespace FLPQ.TestUtilities

open FLPQ.Languages
open FLPQ.LinearAlgebra

/// Fixtures for building path indices in tests.
module PathIndexFixtures =

    /// Builds an empty path index on a stateCount × vertexCount grid.
    let mk (stateCount: int) (vertexCount: int) : PathIndex<string, string> =
        let k = stateCount * vertexCount

        { Matrix = Matrix.init k k (Set.empty: Set<PathIndexEntry<string, string>>)
          StateCount = stateCount
          VertexCount = vertexCount }
