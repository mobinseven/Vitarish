# Web host routing

- Route topology, feature entry points, navigation, and intent contracts: update every applicable route tree, registry, mapper, journey default, localization key, test, and generated transport source; never edit generated API output manually.
- Route/page shells, main landmark, viewport geometry, outer scrolling, shared gutters, sidebar, and aside: classify the surface and reuse the matching `@vitarish/ui` capability; feature-internal grids and intentional workspace-pane scrolling remain local.

## Named values

- **[MUST]** Reuse the authoritative owner for route and intent values, API
  operation IDs, localization keys, query/cache keys, feature flags, and action
  IDs. Keep these values in the existing route, contract, localization, query
  key, or feature-configuration mechanism; do not repeat magic strings across
  route trees, mappers, hooks, components, tests, or app registration. Inline
  framework syntax, CSS utility classes, and exact-value tests remain allowed
  when they are not application contracts or business rules.

## Localization convention

- **[MUST]** All localization keys used in components must be defined.

## Icon conventions

- **[MUST]** Render interface icons using Font Awesome CSS classes. The icon name must be recognizable directly from the JSX, for example `fa-light fa-users`.
- **[MUST]** Do not embed `<svg>`, `<path>`, copied path data, or SVG-based icon components for interface icons.
- Use `FaIcon` from `@vitarish/ui` when its shared behavior is useful; otherwise, a plain semantic `<i>` with Font Awesome classes is acceptable.
- SVG remains allowed for non-icon artwork such as logos, illustrations, diagrams, and data visualizations.
- Decorative icons must be hidden from assistive technology. Icon-only controls must have an accessible label.

## Button conventions

- **[MUST]** All buttons must have localized tooltips and icons. so in mobile view they can be shrunk to icon and tooltips to avoid confusing users.

## Web test quality

- **[MUST]** Every Web test must verify a meaningful, purposeful behavior,
  contract, invariant, accessibility outcome, state transition, or user-visible
  result that is worth protecting. A bare presence assertion such as
  `getByRole(...).toBeInTheDocument()` is not a sufficient test unless the
  existence itself is the contract being tested.
- **[MUST]** Do not create or modify tests merely to make mock designs pass, to
  mirror implementation markup, or to count rendered controls. For mock or
  presentation-only work, leave unrelated tests unchanged and report stale or
  irrelevant failures instead of shaping the UI around them.

## Date conventions

- **[MUST]** All dates must be displayed in localized formats. 
- **[MUST]** All date conversions must be used by central conventions.

## Real-host E2E feedback

- **[MUST]** For changes that cross the API, authentication, authorization,
  tenant, persistence, or shared transport boundary, run the real two-host
  proof from `src/Hosts/Web`:

  ```powershell
  $proof = Join-Path (Resolve-Path ".") "artifacts/two-host-e2e/authenticated-dashboard-proof.png"
  Remove-Item -LiteralPath $proof -Force -ErrorAction SilentlyContinue
  $env:VITARISH_E2E_PROOF_SCREENSHOT = $proof
  npm run test:e2e:two-host
  if (-not (Test-Path -LiteralPath $proof)) { throw "E2E proof screenshot was not produced." }
  ```

- **[MUST]** Inspect the fresh authenticated screenshot as part of the
  implementation flow. Use it to identify and correct layout, loading, data,
  authentication, tenant, or authorization-flow defects, then rerun the proof.
  Do not report completion from a passing test or screenshot that was not
  inspected.
- Visual-only changes that do not cross a backend boundary may use the existing
  visual-test workflow instead.

## Security-facing Web behavior

- **[MUST]** Web displays security state; it never decides it. Do not calculate
  effective authorization, tenant/resource ownership, session validity,
  failed-attempt policy, file integrity, or audit scope in React code.
- **[MUST]** Login history and parallel-session/integrity/security notices must
  remain visible until the server-owned acknowledgement succeeds, or remain in
  a stable account/security surface when the owning contract specifies that
  model. A local dismissal, cache mutation, or optimistic UI state is not
  acknowledgement.
- **[MUST]** Use the shared authenticated API transport for credentials,
  antiforgery, tenant headers, cancellation, and generated contracts. Handle
  unauthenticated, forbidden, conflict/session-revoked, dependency-unavailable,
  and integrity-blocked outcomes explicitly; do not turn them into empty or
  successful UI states.
- **[MUST]** Authentication/session UI changes require focused interaction
  coverage plus the real two-host proof for known/unknown denial parity where
  visible, sign-in, new-session notice, maximum-session behavior, idle expiry,
  explicit sign-out, security-attribute revocation, and history acknowledgement
  as applicable to the changed slice.
