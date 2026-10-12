"""Materialize CFPQ reference data from the pinned flpq-data package into data/cfpq/.

For each pinned graph this tool materializes the effective query graph — the
stored edges plus the reverse edges with the ``_r`` label suffix that the
package's CFPQ queries run over (flpq_data.add_reverse_edges) — as edge-label
MatrixMarket files, materializes every cfpq grammar template over the graph
into a plain .cnf (indexed symbols expanded to the graph's concrete edge
labels), copies the reference results.mtx, and records the expected
reachable-pair counts from the package's reachable_pairs.csv. Output layout:

    data/cfpq/manifest.json                   {version, graphs: {name: {vertex_count, grammars}}}
    data/cfpq/<graph>/graph/*.mtx             edge-label matrices (0-based pattern), _r labels included
    data/cfpq/<graph>/queries/<q>/<q>.cnf     materialized plain grammar
    data/cfpq/<graph>/queries/<q>/results.mtx reference reachability matrix
    data/cfpq/<graph>/reference.json          {grammar: num_reachable_pairs}

The tool is idempotent: re-running regenerates the same files. It requires
Python >= 3.11 and flpq-data==EXPECTED_VERSION:

    pip install --extra-index-url https://test.pypi.org/simple/ "flpq-data==6.0.0.dev15"
    python3 tools/flpq_data/export.py

Unlike the quality tools, this is a one-shot data materializer: it prints its
summary to stdout instead of writing tmp/<tool>.txt.
"""

import csv
import json
import pathlib
import shutil
import sys

EXPECTED_VERSION = "6.0.0.dev15"
PINNED_GRAPHS = ["generations", "travel", "skos", "sampleproject", "univ"]

REPO_ROOT = pathlib.Path(__file__).resolve().parents[2]
OUTPUT_DIR = REPO_ROOT / "data" / "cfpq"


def load_cfpq_counts() -> dict:
    """Read the package's reachable_pairs.csv into {(graph, grammar): count}.

    Only rows with query_class == cfpq and a non-empty count are kept; the
    grammar column carries a .cnf suffix that is stripped here.
    """
    import flpq_data

    counts = {}
    with flpq_data.REACHABLE_PAIRS_CSV.open() as f:
        for row in csv.DictReader(f):
            if row["query_class"] == "cfpq" and row["num_reachable_pairs"]:
                grammar = row["grammar"].removesuffix(".cnf")
                counts[(row["graph"], grammar)] = int(row["num_reachable_pairs"])
    return counts


def export_graph(name: str, cfpq_counts: dict) -> dict:
    """Materialize one graph into data/cfpq/<name>/ and return its manifest entry."""
    import flpq_data

    graph_dir = flpq_data.graph_dir(name)
    graph = flpq_data.graph_from_mtx_dir(graph_dir / "graph")

    dest = OUTPUT_DIR / name
    if dest.exists():
        shutil.rmtree(dest)
    (dest / "graph").mkdir(parents=True)

    # The package's CFPQ queries run over the stored edges plus the reverse
    # edges with the _r label suffix; materialize that effective graph.
    flpq_data.graph_to_mtx_dir(flpq_data.add_reverse_edges(graph), dest / "graph")

    reference = {}
    for query_dir in sorted((graph_dir / "queries" / "cfpq").iterdir()):
        if not query_dir.is_dir():
            continue
        q = query_dir.name
        cfg = flpq_data.materialize_grammar(query_dir / f"{q}.cnf", graph)
        out_dir = dest / "queries" / q
        out_dir.mkdir(parents=True)
        (out_dir / f"{q}.cnf").write_text(flpq_data.cnf_template_to_text(cfg))
        shutil.copyfile(query_dir / "results.mtx", out_dir / "results.mtx")
        count = cfpq_counts.get((name, q))
        if count is None:
            print(
                f"WARNING: {name}/{q}: no reachable_pairs.csv row with a count; "
                "grammar exported but not recorded in reference.json"
            )
        else:
            reference[q] = count

    (dest / "reference.json").write_text(json.dumps(reference, indent=2) + "\n")
    return {"vertex_count": graph.number_of_nodes(), "grammars": reference}


def main() -> None:
    import flpq_data

    if flpq_data.__version__ != EXPECTED_VERSION:
        sys.exit(
            f"flpq-data {flpq_data.__version__} is installed, but this tool pins "
            f"{EXPECTED_VERSION}; install it with: pip install --extra-index-url "
            "https://test.pypi.org/simple/ " + f'"flpq-data=={EXPECTED_VERSION}"'
        )

    cfpq_counts = load_cfpq_counts()
    manifest = {"version": EXPECTED_VERSION, "graphs": {}}
    for name in PINNED_GRAPHS:
        entry = export_graph(name, cfpq_counts)
        manifest["graphs"][name] = entry
        total_pairs = sum(entry["grammars"].values())
        print(
            f"{name}: {entry['vertex_count']} vertices, "
            f"{len(entry['grammars'])} grammars, {total_pairs} reachable pairs"
        )

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    (OUTPUT_DIR / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    total_cases = sum(len(g["grammars"]) for g in manifest["graphs"].values())
    print(f"Exported {total_cases} graph x grammar cases to {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
