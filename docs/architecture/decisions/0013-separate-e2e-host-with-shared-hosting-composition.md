# ADR-0013: Separate E2E Host with Shared Hosting Composition

Status: Accepted  
Date: 2026-08-15  
Owner: Platform Architecture

## Context

Production startup and browser end-to-end tests require different persistence,
certificate, and lifecycle guarantees. Reusing production startup for E2E tests
coupled test-only authentication and initialization suppression to the
production composition path.

## Decision

`src/Libraries/Hosting` owns reusable registration and middleware composition.
`src/Hosts/Main` is the production-only PostgreSQL, Redis, certificate,
Hangfire, OpenAPI, migration, and initialization root. `src/Hosts/E2e` is a
non-publishable executable using explicit loopback origins, an ephemeral
IdentityServer certificate, and in-memory databases.

Modules and apps expose registration, endpoint mapping, and initialization as
separate lifecycle operations. E2E does not run migrations, production
initializers, background-job scheduling, or test-only authentication routes.
The Web runner starts E2e and the real Web app, supplies a generated password,
and cleans up child process trees and E2e build output.

## Consequences

- Production startup has no E2E branch, flag, route, or password shortcut.
- Shared host behavior has one composition owner while provider and operational
  concerns remain in each executable host.
- E2E uses isolated in-memory state and deterministic fixture identity/tenant
  data without touching production persistence.
- Hosting physically owns the common implementation files; Main retains only
  production-only infrastructure, preserving the public host contract.

## Alternatives rejected

- Keep a `VITARISH_E2E` branch in Main: this preserves unsafe production/test
  coupling.
- Add a test-only authentication endpoint: this bypasses real authentication
  and tenant authorization.
- Run E2E against PostgreSQL migrations: this reduces isolation and increases
  coupling to deployment persistence.

## Validation

Build Hosting, E2e, and Main with canonical OpenAPI generation; run the
architecture endpoint validator and focused host/module tests; then run the
two-host Playwright runner and inspect authentication, tenant, fixture, and
cleanup assertions.
