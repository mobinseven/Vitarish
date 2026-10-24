# Web browser spec authoring

Applies to Playwright specs under `src/Hosts/Web/tests`. Running them and the
two-host proof stay in `src/Hosts/Web/AGENTS.md`, whose Web test quality rules
also apply here.

## Placement

- `mock/`: app screens and flows against the mock API, captured per
  scenario, locale, theme, and viewport. A spec belongs to the owner app
  unless `mockApps` in `scripts/spec-capture-options.mjs` lists it under
  another app.
- `e2e/`: behavior that must cross the real API, authentication, tenant,
  persistence, or shared transport boundary on the E2E and Web hosts. A mock
  spec is not boundary proof.
- `layout/`: shell geometry, theme mode, and visual regression snapshots.

## Runner capture contract

`npm run specs` discovers specs from `mock/` and `e2e/` and narrows them by
title, project, and environment, so a spec it cannot narrow is captured wrongly.

- **[MUST]** A mock spec loops over `supportedLocales`, keeps the locale as a
  separate word in its test title, and reads its scenario and output folder
  through `mockScreenshotScenarioEnvironmentKey` and
  `mockScreenshotOutputEnvironmentKey`. A flow writes numbered steps to
  `<scenario>/<locale>-<project>/mock-<flow>/` and skips scenarios that cannot
  answer it.
- **[MUST]** An E2E spec that works in every locale reads
  `twoHostE2eLocaleEnvironmentKey`; otherwise it runs once in its own locale.
- **[MUST]** In `mock/` and `e2e/`, theme and viewport come from the
  Playwright projects and `spec-capture-options.mjs`. A spec pins its own only
  when that is what it proves, or when its captures have a fixed format, such
  as the evidence-document screens. `layout/` specs set them per case.

## Named values

- **[MUST]** Take routes from the app `Routes`, visible text from the owning
  localization catalog, fixture ids from `mockFixtureIds` or the E2E fixture
  seeder, and environment keys from `scripts/spec-capture-options.mjs` or
  `scripts/two-host-e2e-artifacts.mjs`. Do not inline them; exact values the
  spec asserts may stay literal.

## E2E state

- **[MUST]** The E2E suite runs serially against one host lifetime. A spec that
  changes shared state (passwords, sessions, policies, memberships) restores it
  in `finally`, or uses its own fixture account or client IP so it stays out of
  other specs' sign-in rate limits.
- **[MUST]** `e2e/authenticated-boundary.spec.ts` and its
  `authorization-matrix:` markers are referenced by
  `tools/architecture/AuthorizationMatrixCheck.cs` and
  `docs/security/authorization-matrix.md`; move or rename them only together.
