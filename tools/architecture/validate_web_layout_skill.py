#!/usr/bin/env python3
"""Validate the canonical Web layout skill and its isolated routing cases."""

from pathlib import Path
import re
import sys


ROOT = Path(__file__).resolve().parents[2]
SKILL = ROOT / ".agents/skills/apply-web-layout-policy/SKILL.md"
INDEX = ROOT / ".agents/skills/README.md"
AGENTS = ROOT / "AGENTS.md"
BRIDGE = ROOT / ".claude/skills/apply-web-layout-policy/SKILL.md"


def fail(message: str) -> None:
    print(f"web-layout-skill: {message}", file=sys.stderr)
    raise SystemExit(1)


def read(path: Path) -> str:
    if not path.is_file():
        fail(f"missing {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


skill = read(SKILL)
index = read(INDEX)
agents = read(AGENTS)
bridge = read(BRIDGE)

frontmatter = re.match(r"\A---\n(.*?)\n---\n", skill, re.DOTALL)
if frontmatter is None:
    fail("canonical skill has invalid frontmatter")

metadata = frontmatter.group(1)
if "name: apply-web-layout-policy" not in metadata:
    fail("canonical skill name does not match its directory")

description_match = re.search(r"^description:\s*(.+)$", metadata, re.MULTILINE)
if description_match is None:
    fail("canonical skill is missing its trigger description")

description = description_match.group(1).lower()
required_trigger_terms = (
    "planning",
    "implementation",
    "diagnosis",
    "review",
    "dashboardframe",
    "applayout",
    "main landmarks",
    "scrolling",
    "viewport sizing",
    "gutters",
    "public chrome",
    "sidebar",
    "aside",
    "centered states",
    "immersive workspaces",
    "do not use",
    "feature-internal grids",
)
missing_terms = [term for term in required_trigger_terms if term not in description]
if missing_terms:
    fail(f"trigger description is missing: {', '.join(missing_terms)}")

for step in range(1, 7):
    if not re.search(rf"^{step}\. ", skill, re.MULTILINE):
        fail(f"canonical skill is missing workflow step {step}")

for source in (
    "src/Hosts/Web/shared/web-ui/README.md",
    "docs/codebase-map.md",
):
    if source not in skill:
        fail(f"canonical skill does not reference {source}")

if "`apply-web-layout-policy`" not in agents:
    fail("root AGENTS.md does not route Web layout work to the skill")

expected_cases = {
    "Positive": ("`apply-web-layout-policy`",),
    "Negative": ("`platform-react-feature`", "not `apply-web-layout-policy`"),
    "Planning": ("`platform-change-planning`", "`apply-web-layout-policy`", "`platform-test-validation`"),
    "Implementation": (
        "`platform-delivery-loop`",
        "`apply-web-layout-policy`",
        "`platform-react-feature`",
        "`platform-test-validation`",
    ),
    "Diagnosis": ("`apply-web-layout-policy`", "`platform-test-validation`"),
    "Review": ("`apply-web-layout-policy`", "`platform-test-validation`"),
}

case_rows = {}
for line in index.splitlines():
    columns = [column.strip() for column in line.strip().strip("|").split("|")]
    if len(columns) == 3 and columns[0] in expected_cases:
        case_rows[columns[0]] = columns[2]

for case, expected_routes in expected_cases.items():
    route = case_rows.get(case)
    if route is None:
        fail(f"skill index is missing the isolated {case.lower()} case")
    missing_routes = [expected for expected in expected_routes if expected not in route]
    if missing_routes:
        fail(f"{case.lower()} case is missing routes: {', '.join(missing_routes)}")

if ".agents/skills/apply-web-layout-policy/SKILL.md" not in bridge:
    fail("Claude bridge does not point to the canonical skill")

print("web-layout-skill: canonical skill and 6 isolated routing cases are valid")
