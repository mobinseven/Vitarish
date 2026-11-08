# Clinic navigation implementation

Status: planning only; implementation not yet authorized.
Target: [navigation map](../apps/private-practice-general/navigation-map.md).
Removal condition: delete this plan when every slice below is merged and the
map's status is Active.

## Outcome and non-goals

Outcome: the private-practice-owner app implements the navigation map — seven
destinations, page types, patient-record tabs, settings grouping — with
navigation generated from one route registry and enforced by checks.

Non-goals: account, sign-in, platform home and onboarding hosts; the
clinic-user (patient-facing) pages; backend API changes; visual redesign
beyond page-type headers; new features behind stub pages.

## Repository evidence

Captured 2026-11-08 from a crawl of the compiled app under the `ready` mock
scenario (54 routes, fa and en, desktop and mobile).

- Navigation has three independent owners:
  `components/shell/dashboardShellNavigation.ts` (sidebar, its item list
  repeated in four groups), `components/shell/CommandPalette/commandRegistry.ts`
  and `components/clinic-navigation.tsx` (intent → route). They disagree on
  which pages are reachable.
- Sidebar items use `exactMatch: true`, so no item is active on child pages
  (`/appointments`, `/appointments/shifts`, `/contents`, record pages).
- Four detail pages crash with client exceptions: reservation, service
  provider, shift, shift occurrence.
- The mock runtime has no handlers for prescriptions dashboard, prescription
  template, reminders query, surgery settings, file upload policy, contents and
  several account endpoints; affected pages render error states.
- `/appointments` duplicates `/appointments/reservations`; shifts, services,
  service types and providers are reachable only by URL or settings cards.
- Unknown routes render the framework's English 404 outside the app layout.
- Headers vary per page (dark hero, plain title, bare text); several pages
  have no `h1` or two.

## Route reconciliation

| Current route | Decision | Target |
| --- | --- | --- |
| `/home` | keep | Today |
| `/check-in` | keep | Check-in destination; page-type header |
| `/patients`, `/patients/new`, `/patients/{id}/edit` | keep | Patients list / forms |
| `/patients/{id}` | rebuild | Record with tabs: overview, visits, surgeries, procedures, prescriptions, reminders |
| `/patients/{id}/procedures/{pid}` and `/events/{eid}` | keep | Workspace under the record |
| `/appointments` | rebuild | Schedule list with day/week/month views |
| `/appointments/reservations` | merge | Redirect to `/appointments` |
| `/appointments/reservations/{id}` | rebuild | Visit record (fix crash) |
| `/appointments/shifts`, `/new`, `/{id}`, `/{id}/edit`, `/{id}/occurrence/{date}` | move | `/settings/shifts/...` (fix crashes); old URLs redirect |
| `/appointments/services`, `/service-types`, `/service-providers` and details | merge | `/settings/services` with tabs; old URLs redirect |
| `/appointments/settings` | move | `/settings/appointments`; old URL redirects |
| `/surgeries`, `/new`, `/{id}`, `/{id}/edit` | keep | Surgeries destination |
| `/reminders` | keep | Reminders destination (add mock data) |
| `/settings` and existing settings children | keep | Settings index and pages |
| `/contents` | delete | Removed from this app |
| `/knowledge`, `/knowledge/qa` | delete | Removed from this app |
| `/viewport-reveal-test`, `/layout-fixture/*`, `/color-legend` | keep | Test fixtures; excluded from navigation and the registry's reachability rule |
| unknown route | rebuild | Localized not-found inside the app layout |

Redirects keep existing bookmarks and links working. Each redirect is
recorded in the registry with removal condition "no inbound references for one
release".

## Design

- **Owner:** `apps/private-practice-owner` owns a route registry next to
  `app/routes.ts`. Each entry: route builder, parent, destination, page type,
  label key, icon, required permission. `Routes` stays the URL owner; the
  registry references it and adds no new URL literals.
- Sidebar groups, active highlight (by destination, not exact URL),
  breadcrumbs/back links and command-menu navigation entries are derived from
  the registry. `clinic-navigation.tsx` keeps intent handling but resolves
  targets through `Routes`.
- Page-type headers reuse the existing shell/header components; one component
  per page type, no new design-system wrapper.
- Dependency direction unchanged: UI only. No API, contract, persistence,
  configuration or generated-client changes. Mock handlers are test runtime.

## Slices

Each slice is independently mergeable and leaves the app working.

1. **Stabilize** — fix the four detail-page crashes at their root; add missing
   mock handlers and fixtures; localized in-layout not-found and error pages.
   Accept: crawl shows zero page errors and zero 4xx API calls under `ready`.
2. **Route registry** — introduce the registry, derive sidebar and active state
   from it, collapse the four duplicated nav groups. Accept: exactly one active
   destination on every registered route; unit test enumerates the registry.
3. **Reconcile routes** — moves, merges, redirects and removals from the table;
   update command palette and intents. Accept: no route reachable only by URL;
   old URLs redirect.
4. **Page types** — apply the six page-type headers and back behavior; one
   `h1` per page; labelled icon controls. Accept: layout lint and the crawl
   checks pass on fa/en, desktop/mobile.
5. **Patient record tabs** — rebuild the record as tabs; surgery planning from
   the record pre-fills the patient. Accept: map flows 2, 4, 6 within their
   click budgets.
6. **Schedule views** — merge reservations into Schedule with day/week/month.
   Accept: flows 3 and 8.
7. **Enforce** — add a mock-scenario spec that visits every registry route and
   fails on page errors, failed API calls, missing/extra `h1`, no active
   destination or horizontal overflow; wire it into `npm run test` or the
   screenshots pipeline. Set the map's status to Active and delete this plan.

## Progress

- Slices 1 to 3 are merged on `feat/clinic-navigation`.
- Slice 4 is done for the clinic pages outside the patient record: a plain header
  (`PageHeader`) on lists, forms and settings, the anchor band only on record
  pages, one `h1` per page, and a back trail (breadcrumb on desktop, a single
  "Back to X" link on mobile). The patient record, edit and procedure pages keep
  their current headers and the top-bar up button until slice 5. The account
  sub-pages (name, email, phone, passkeys, language) still use `SettingsSection`
  headers; unify them when the account area is next touched.
- `components/user-appointments/*` in the shared clinic package has no consumer in
  any app; remove it or give it one.

## Validation

- Per slice: `npm run typecheck`, `npm run test`, `npm run lint:layout`,
  `npm run validate:localization`, `npm run validate:web-ui-boundary`, the
  route-crawl spec, and a manual walk of affected map flows.
- Two-host E2E is not required: no backend, auth, tenant or persistence
  boundary changes.

## Risks and revision triggers

- Crashes may come from real API shape drift rather than mock data; if so the
  fix belongs in the shared clinic client, not the mock.
- Moving shifts/services changes URLs used in SMS or emails; check outbound
  links before slice 3.
- Permissions per role are not yet modelled in the registry's consumers;
  revise if role-based hiding needs backend data.
- Revise the plan if the map changes or if tree testing with clinic staff
  contradicts the grouping.

## Rejected alternatives

- Patching each page's highlight and header individually: repeats the current
  drift between three navigation owners.
- Generating routes from the file system: page type, destination and
  permission are not derivable from paths.
