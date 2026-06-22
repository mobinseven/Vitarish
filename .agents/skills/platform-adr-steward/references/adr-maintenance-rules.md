# ADR Maintenance Rules

## Good ADRs

A good ADR is:

- about one significant architecture decision
- short enough to read quickly
- explicit about context
- explicit about the chosen decision
- honest about tradeoffs
- clear about consequences
- connected to related ADRs
- practical enough to guide implementation

## Avoid

Avoid:

- multiple unrelated decisions in one ADR
- generic best-practice text without project context
- long essays
- implementation-only details
- duplicated decisions
- undocumented reversals of accepted decisions

## Status Handling

Use `Superseded` instead of deleting history.

Use `Rejected` when an option was considered and intentionally not chosen.

Use `Deprecated` when a decision remains historically valid but should not guide new work.
