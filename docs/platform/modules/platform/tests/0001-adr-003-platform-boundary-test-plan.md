# ADR-003 Platform boundary test plan

Date: 2026-06-25
Status: Suggested
App: Shared platform
Module: Platform
Capability: Interface/backend isolation for theme defaults
Related ADRs: ADR-003
Confidence: Medium

## Objective
Verify that the Platform Interface uses frontend-safe theme defaults from Contracts and does not reference Platform Domain for static fallback colors.

## Required checks
- Architecture boundary check: Platform must remain `FailFast` with no Interface references to `Vitarish.Platform.Domain`.
- Contract/defaults check: `PlatformThemeDefaults` fallback values remain serialization/presentation-safe constants only.

## Suggested tests
- Contract test comparing known fallback defaults used by `PlatformThemeForm` against expected hex values.

## Hot tests
No hot test is required for this slice because no Platform HTTP workflow or runtime dependency was introduced. Platform Client/API are intentionally deferred until a current UI workflow needs HTTP access.
