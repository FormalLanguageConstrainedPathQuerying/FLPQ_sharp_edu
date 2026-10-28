module FlpqdotKronRegressionTests

open Xunit
open FLPQ.Printers
open FlpqdotProbeHelpers

/// Regression harness for the Kronecker-CFPQ summary graphs (the task's repro).
/// Each fixture is one of the six step graphs from `viz/kcfpq/step_*/graph.tikz.tex`,
/// rewritten to exercise the label-aware path end-to-end: a
/// `\flpqdotmeasureedgelabel` pre-pass feeds each edge label's size (and text) to
/// the channel router, and the non-loop edges carry no default quote (the S4
/// edge-callback wrapper places the label at the router's computed position). The
/// loop edge in step_6 keeps its stock quote, since the router skips loops.
///
/// For every step graph this asserts the three reported problems are gone:
///   (a) it compiles with `dot layout`;
///   (b) no label overlaps another label, a node, or another edge's path;
///   (c) no edge path is degenerate (self-intersecting).
///
/// A step-graph fixture (`KronStep`) is the step's name, its measurement
/// pre-pass (one command per non-loop edge), and its graph body (node
/// definitions plus edges; non-loop edges carry no quote, the step_6 loop keeps
/// its stock quote).
type private KronStep =
    { Name: string
      PrePass: string
      Body: string }

/// The four node definitions shared by every step graph (the `as` options set
/// the displayed node label, which determines the node box size).
let private nodes =
    """v0 [as={$v_0$}];
    v1 [as={$v_1$}];
    v2 [as={$v_2$}];
    v3 [as={$v_3$}];"""

/// The six step graphs, in order. The edge set grows by one derivation step at a
/// time (step_1 is the smallest, step_6 the full summary).
let private kronSteps: KronStep list =
    [ { Name = "step_1"
        PrePass =
          """\flpqdotmeasureedgelabel{v0->v1}{a}
\flpqdotmeasureedgelabel{v1->v2}{a}
\flpqdotmeasureedgelabel{v1->v3}{S}
\flpqdotmeasureedgelabel{v2->v0}{a}
\flpqdotmeasureedgelabel{v2->v3}{b}
\flpqdotmeasureedgelabel{v3->v2}{b}"""
        Body =
          nodes
          + """
    v0 -> v1;
    v1 -> v2;
    v1 -> v3;
    v2 -> v0;
    v2 -> v3;
    v3 -> v2;""" }
      { Name = "step_2"
        PrePass =
          """\flpqdotmeasureedgelabel{v0->v1}{a}
\flpqdotmeasureedgelabel{v0->v2}{S}
\flpqdotmeasureedgelabel{v1->v2}{a}
\flpqdotmeasureedgelabel{v1->v3}{S}
\flpqdotmeasureedgelabel{v2->v0}{a}
\flpqdotmeasureedgelabel{v2->v3}{b}
\flpqdotmeasureedgelabel{v3->v2}{b}"""
        Body =
          nodes
          + """
    v0 -> v1;
    v0 -> v2;
    v1 -> v2;
    v1 -> v3;
    v2 -> v0;
    v2 -> v3;
    v3 -> v2;""" }
      { Name = "step_3"
        PrePass =
          """\flpqdotmeasureedgelabel{v0->v1}{a}
\flpqdotmeasureedgelabel{v0->v2}{S}
\flpqdotmeasureedgelabel{v1->v2}{a}
\flpqdotmeasureedgelabel{v1->v3}{S}
\flpqdotmeasureedgelabel{v2->v0}{a}
\flpqdotmeasureedgelabel{v2->v3}{b, S}
\flpqdotmeasureedgelabel{v3->v2}{b}"""
        Body =
          nodes
          + """
    v0 -> v1;
    v0 -> v2;
    v1 -> v2;
    v1 -> v3;
    v2 -> v0;
    v2 -> v3;
    v3 -> v2;""" }
      { Name = "step_4"
        PrePass =
          """\flpqdotmeasureedgelabel{v0->v1}{a}
\flpqdotmeasureedgelabel{v0->v2}{S}
\flpqdotmeasureedgelabel{v1->v2}{a, S}
\flpqdotmeasureedgelabel{v1->v3}{S}
\flpqdotmeasureedgelabel{v2->v0}{a}
\flpqdotmeasureedgelabel{v2->v3}{b, S}
\flpqdotmeasureedgelabel{v3->v2}{b}"""
        Body =
          nodes
          + """
    v0 -> v1;
    v0 -> v2;
    v1 -> v2;
    v1 -> v3;
    v2 -> v0;
    v2 -> v3;
    v3 -> v2;""" }
      { Name = "step_5"
        PrePass =
          """\flpqdotmeasureedgelabel{v0->v1}{a}
\flpqdotmeasureedgelabel{v0->v2}{S}
\flpqdotmeasureedgelabel{v0->v3}{S}
\flpqdotmeasureedgelabel{v1->v2}{a, S}
\flpqdotmeasureedgelabel{v1->v3}{S}
\flpqdotmeasureedgelabel{v2->v0}{a}
\flpqdotmeasureedgelabel{v2->v3}{b, S}
\flpqdotmeasureedgelabel{v3->v2}{b}"""
        Body =
          nodes
          + """
    v0 -> v1;
    v0 -> v2;
    v0 -> v3;
    v1 -> v2;
    v1 -> v3;
    v2 -> v0;
    v2 -> v3;
    v3 -> v2;""" }
      { Name = "step_6"
        PrePass =
          """\flpqdotmeasureedgelabel{v0->v1}{a}
\flpqdotmeasureedgelabel{v0->v2}{S}
\flpqdotmeasureedgelabel{v0->v3}{S}
\flpqdotmeasureedgelabel{v1->v2}{a, S}
\flpqdotmeasureedgelabel{v1->v3}{S}
\flpqdotmeasureedgelabel{v2->v0}{a}
\flpqdotmeasureedgelabel{v2->v3}{b, S}
\flpqdotmeasureedgelabel{v3->v2}{b}"""
        Body =
          nodes
          + """
    v0 -> v1;
    v0 -> v2;
    v0 -> v3;
    v1 -> v2;
    v1 -> v3;
    v2 -> v0;
    v2 -> v3;
    v3 -> v2;
    v2 ->["S", red, thick, loop above] v2;""" } ]

/// The number of non-loop edges in a step (the count of measurement pre-pass
/// commands, i.e. the labels the router must place).
let private nonLoopEdgeCount (step: KronStep) : int =
    step.PrePass.Split('\n')
    |> Array.filter (fun l -> l.StartsWith("\\flpqdotmeasureedgelabel"))
    |> Array.length

[<Fact>]
[<Trait("Category", "TeX")>]
let ``every Kronecker-CFPQ step graph is label-clean and curve-simple`` () =
    for step in kronSteps do
        let content = step.PrePass + "\n" + tikzGraphWithLayout "dot layout" step.Body
        let log = compileProbeLog content

        // (a) compiles (compileProbeLog already fails on a compile error).
        // (b) every non-loop edge is assigned a label position.
        let placed = probeInt "LABEL_PLACED_COUNT=" log
        let expected = nonLoopEdgeCount step
        let placedOk = placed = expected

        let placedMsg =
            sprintf "%s: expected %d placed labels, got %d" step.Name expected placed

        Assert.True(placedOk, placedMsg)

        // (b) no label overlaps another label, a node, or another edge's path.
        let labelOverlaps = probeInt "LABEL_OVERLAP_COUNT=" log
        let nodeColl = probeInt "LABEL_NODE_COLLISIONS=" log
        let edgeColl = probeInt "LABEL_EDGE_COLLISIONS=" log
        let overlapOk = labelOverlaps = 0 && nodeColl = 0 && edgeColl = 0

        let overlapMsg =
            sprintf
                "%s: label overlaps=%d node collisions=%d edge collisions=%d"
                step.Name
                labelOverlaps
                nodeColl
                edgeColl

        Assert.True(overlapOk, overlapMsg)

        // (c) no edge path is degenerate (self-intersecting).
        let selfInt = probeInt "SELF_INTERSECTION_COUNT=" log
        let selfIntOk = selfInt = 0
        let selfIntMsg = sprintf "%s: %d self-intersecting edge path(s)" step.Name selfInt
        Assert.True(selfIntOk, selfIntMsg)
