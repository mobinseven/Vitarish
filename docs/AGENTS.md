# Documentation agent routing

Documentation does not automatically activate a skill for every boundary it
mentions. Repository and scoped `AGENTS.md` instructions own the boundary
guidance; skill or agent-routing documentation follows `.agents/AGENTS.md`.

For prose-only corrections, navigation updates, or faithful documentation of an
already-authoritative implementation, use no boundary skill. Keep behavior in
its owning code or decision record rather than making documentation a duplicate
source of truth.
