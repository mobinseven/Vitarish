#!/usr/bin/env python3
"""Validate the canonical Web layout skill and its isolated routing cases."""

from pathlib import Path
import re
import sys


ROOT = Path(__file__).resolve().parents[2]
SKILL = ROOT / ".agents/skills/platform-web-layout-policy/SKILL.md"
WEB_AGENTS = ROOT / "src/Hosts/Web/AGENTS.md"
BRIDGE = ROOT / ".claude/skills/platform-web-layout-policy/SKILL.md"


def fail(message: str) -> None:
    print(f"web-layout-skill: {message}", file=sys.stderr)
    raise SystemExit(1)


def read(path: Path) -> str:
    if not path.is_file():
        fail(f"missing {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


skill = read(SKILL)
web_agents = read(WEB_AGENTS)
bridge = read(BRIDGE)

frontmatter = re.match(r"\A---\n(.*?)\n---\n", skill, re.DOTALL)
if frontmatter is None:
    fail("canonical skill has invalid frontmatter")

metadata = frontmatter.group(1)
if "name: platform-web-layout-policy" not in metadata:
    fail("canonical skill name does not match its directory")

description_match = re.search(r"^description:\s*(.+)$", metadata, re.MULTILINE)
if description_match is None:
    fail("canonical skill is missing its trigger description")

description = description_match.group(1).lower()
required_trigger_terms = (
    "route shells",
    "main/scroll ownership",
    "viewport sizing",
    "public chrome",
    "centered states",
    "immersive workspaces",
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

if "`platform-web-layout-policy`" not in web_agents:
    fail("Web AGENTS.md does not route layout work to the skill")

if ".agents/skills/platform-web-layout-policy/SKILL.md" not in bridge:
    fail("Claude bridge does not point to the canonical skill")

print("web-layout-skill: canonical skill and Web-scoped routing are valid")
