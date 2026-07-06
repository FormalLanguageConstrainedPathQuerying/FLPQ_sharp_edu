#!/usr/bin/env bash
# Run tests with code coverage collection using dotnet-coverage.
# Produces Cobertura XML report in coverage/ directory.
# Requires: dotnet SDK, dotnet-coverage local tool (dotnet tool restore)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
COVERAGE_DIR="$PROJECT_DIR/coverage"

mkdir -p "$COVERAGE_DIR"

echo "=== Running tests with coverage collection ==="
cd "$PROJECT_DIR"
dotnet dotnet-coverage collect \
  dotnet test FLPQ.slnx \
    --filter "Category!=Graphviz&Category!=TeX&Category!=Summary" \
    --no-restore \
    -v quiet \
  -o "$COVERAGE_DIR/coverage.cobertura" \
  -f cobertura \
  --nologo

echo ""
echo "=== Coverage report generated ==="
echo "Cobertura XML: $COVERAGE_DIR/coverage.cobertura"
echo ""

# Parse and display summary for FLPQ source projects
python3 - "$COVERAGE_DIR/coverage.cobertura" << 'PYEOF'
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict

cobertura_path = sys.argv[1]
tree = ET.parse(cobertura_path)
root = tree.getroot()

source_packages = [
    "FLPQ.LinearAlgebra",
    "FLPQ.GraphAnalysis",
    "FLPQ.Languages",
    "FLPQ.Printers",
    "FLPQ.RPQ",
    "FLPQ.Cli",
]

total_covered = 0
total_valid = 0

print(f"{'Package':<30} {'Covered':>8} {'Valid':>8} {'Rate':>8}")
print("-" * 58)

for pkg in root.findall(".//package"):
    name = pkg.attrib.get("name", "")
    if name not in source_packages:
        continue

    pkg_covered = 0
    pkg_valid = 0
    for cls in pkg.findall(".//class"):
        for line in cls.findall(".//lines/line"):
            hits = int(line.attrib.get("hits", 0))
            if hits > 0:
                pkg_covered += 1
            pkg_valid += 1

    rate = (pkg_covered / pkg_valid * 100) if pkg_valid > 0 else 0
    print(f"{name:<30} {pkg_covered:>8} {pkg_valid:>8} {rate:>7.1f}%")
    total_covered += pkg_covered
    total_valid += pkg_valid

print("-" * 58)
total_rate = (total_covered / total_valid * 100) if total_valid > 0 else 0
print(f"{'TOTAL':<30} {total_covered:>8} {total_valid:>8} {total_rate:>7.1f}%")

# Check threshold
threshold = 80.0
if total_rate < threshold:
    print(f"\nWARNING: Coverage {total_rate:.1f}% is below threshold {threshold}%")
    sys.exit(1)
else:
    print(f"\nPASS: Coverage {total_rate:.1f}% meets threshold {threshold}%")
PYEOF
