# Documentation agent routing

Documentation does not automatically activate the skill for every boundary it
mentions. Load a boundary skill only when the document creates or changes a
decision owned by that boundary. Load `platform-artifact-architecture` for
architecture, placement, dependency, or ADR decisions, and
`platform-skill-governance` for skill or agent-routing documentation.

For prose-only corrections, navigation updates, or faithful documentation of an
already-authoritative implementation, use no boundary skill. Keep behavior in
its owning code or decision record rather than making documentation a duplicate
source of truth.
