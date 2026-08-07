# Deterministic mock previews

Use the mock command for rapid Web UI review when Main, IdentityServer, or external providers are not part of the change:

```powershell
npm run dev:mock:private-practice-owner
npm run dev:mock:patients
npm run dev:mock:platform-admin
npm run dev:mock:knowledge-qa-demo
npm run dev:mock:vitarish-landing
```

Pass a fixed route-state scenario after `--`:

```powershell
npm run dev:mock:private-practice-owner -- --scenario loading
```

Supported scenarios are `ready` (default), `empty`, `loading`, and `error`.
The scenario is fixed for the dev-server process so refreshes stay reproducible.

Mock mode starts an MSW browser worker only when
`NEXT_PUBLIC_VITARISH_MOCK_API=1`. It intercepts browser HTTP requests below the
generated clients and `@vitarish/web-api-client`; the app still sends its normal
tenant header, credentials, and antiforgery bootstrap request. Unhandled
requests are errors by design and must never fall through to live services.

The worker is generated into each browser app's `public/mockServiceWorker.js`.
Regenerate it with `npx msw init apps/<app>/public --save`; do not edit it by
hand.

The initial shared handlers cover authentication session, antiforgery, and
journey startup so app roots can boot deterministically. Add a feature fixture
beside the owning feature's client/API boundary when a route needs more data.
Use contract-shaped, non-sensitive fixed data and add the handler in the same
change as the UI request. Do not mock generated client functions or add UI
business rules to fixtures.

`MockApiBootstrap` also accepts stable MSW request handlers. An app mock
composition should import feature-owned handlers once and pass that module-level
array to its provider; do not allocate handlers inside render. This keeps mock
mode at the HTTP boundary while letting each feature own its fixture data.

Normal `npm run dev:<app>` commands retain their live-backend behavior. The
existing Playwright layout fixture server remains independent of mock preview
mode.

Currently covered representative traffic:

| Surface | Requests covered |
| --- | --- |
| Authentication and app entry | session, antiforgery, current journey |
| Knowledge | workspace and ask |
| Clinic | patient list and patient count |
| Appointments | reservations, shifts, services, service types, providers, command outcomes |
| Platform administration | owners, tenants, owner/tenant detail, owner tenants, tenant indicators, user search/memberships, command outcomes |
| Intelligence | live transcription capabilities, responder/transcription/rewrite results, agent conversation start |
| Public landing | conversation submit and reset |

Other endpoint families remain intentionally unhandled until their owning
feature adds contract-shaped fixtures. Mock mode reports those requests as
errors so a route cannot silently use a live backend.
