# ADR-0001: Record Architecture Decisions

Date: YYYY-MM-DD
Status: Accepted
Scope: Platform
Owner: Architecture
Supersedes:
Superseded by:
Related:

## Context

The platform needs a durable way to preserve significant architecture decisions, their context, and their consequences.

Without a decision record, architectural knowledge can drift into scattered discussions, pull request comments, issue threads, or individual memory.

## Decision

We will record significant platform architecture decisions as Markdown ADRs in `docs/architecture/decisions/`.

Each ADR will capture one decision and include context, decision, consequences, alternatives, validation, and notes.

Accepted ADRs are the source of truth for platform architecture decisions.

## Consequences

Positive consequences:

- Architecture decisions become versioned with the repository.
- Future changes can be reviewed against accepted decisions.
- New contributors can understand why the platform evolved in a certain direction.

Negative consequences:

- Significant decisions require a small amount of documentation.
- ADR quality depends on review discipline.

## Alternatives Considered

### Option 1: Markdown ADRs in the repository

Chosen because it is simple, versioned, reviewable, and close to the code.

### Option 2: External documentation system

Rejected for now because it can drift from the repository.

### Option 3: No formal decision records

Rejected because it would increase architecture drift and knowledge loss.

## Validation / Enforcement

Pull request review should ask whether a change requires a new ADR or conflicts with an accepted ADR.

The ADR index should be updated when ADRs are added or superseded.

## Notes

Use supersession instead of deleting old ADRs.
