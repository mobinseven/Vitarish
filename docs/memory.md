# Memory

Purpose:
Durable repo-specific lessons previous sessions learned the hard way.
Do not duplicate AGENTS.md, architecture.md, ADRs, runbooks, or module docs.

## Read first

- `AGENTS.md` is the operating policy.
- `docs/codebase-map.md` is the navigation map.
- `docs/architecture.md` and ADRs define boundaries.
- This file only stores practical lessons and recurring pitfalls.

## Durable decisions to remember

- UTC `DateTime` for persisted instants.
- `private-practice-owner` and `patients` replace the retired `private-practice-general` web host; the backend Clinic and SEA modules remain active.
- Internal module API endpoints are protected by default.
- Generated clients/contracts are regenerated, not manually edited.
- NSMAIL-1 FDP_RIP.2.1 does not require ending the session on browser close:
  its two-step-authentication guidance accepts any one of four options, and
  the auth cookie is issued only after the second factor succeeds. FTA_SSL.3.1
  requires an administrator-configurable idle expiry, not a particular length.

## Commands and failure modes

- Focused backend builds are inner-loop only.
- Merge-level backend confidence requires `ModuleBuildProfile=All` or full solution build.
- Run npm workspace commands from `src/Hosts/Web`.
- `fetch failed` usually means the .NET host is not running at `VITARISH_API_PROXY_TARGET`, or Node lacks `--use-system-ca` to trust its dev certificate. Local Web must call `/api` same-origin: SameSite=Lax cookies are dropped when an http web origin calls the https API directly.
- OpenAPI mutation `403` usually means CSRF/antiforgery mismatch.
- Wrong generated hooks often mean Orval points at the wrong spec.

## Recurring pitfalls

- Optional module references break focused builds.
- Do not hide disabled modules with preprocessor symbols.
- Query flows must not mutate state.
- Provider reference versions must not default to `unknown`.
- Tamin provider dates must use the provider’s expected calendar/date format.
- Knowledge normal-user QA must not reuse owner command components.
- The Users sign-in namespace is guarded by normalized-email, phone-number,
  and Identity user-name indexes plus a write-time cross-column check. The
  remaining email-versus-another-account's `NormalizedUserName` race cannot be
  enforced by those independent indexes; Users owns it and should revisit the
  schema if the race is observed.
- react-aria `usePress` injects its own runtime style element and reads the CSP
  nonce from `<meta property="csp-nonce">`; every Web app layout must render
  `ContentSecurityPolicyNonceMeta` from `@vitarish/ui/content-security-policy`
  or the production browser-evidence gate fails on a `style-src-elem` violation.

## Package/tool quirks

- Orval output is generated.
- Tailwind only generates utilities from a Web host's `@source` globs; it does not scan linked `shared/*` packages. When a host starts rendering a shared package, add that package to the host's `globals.css` `@source` list, or its utility classes silently produce no CSS. HeroUI v3 also has no `primary` color: `bg-primary`/`text-primary` render nothing, so use `accent`.
- `apiFetch` is a shared Web transport package concern; generated React clients are feature package concerns after ADR-0008 migration. Intelligence Web generation and realtime dictation are owned by `src/Hosts/Web/shared/intelligence`; do not reintroduce them in `clinic-web`. Other legacy app-bound generated output can exist until its feature package is extracted.
- Date input and localization validators enforce repo-specific React rules.
- `docs/docs.csproj` is for Visual Studio browsing only.

## Fragile files

- `src/Build/ModuleBuildProfiles.props`
- `src/Hosts/Main/Main.csproj`
- `src/Hosts/Web/package.json`
- `src/Hosts/Web/package-lock.json`
- generated OpenAPI clients/specs/hooks/query keys/mocks
- ADRs under `docs/architecture/decisions`

## E2E-only switches

### Rare audit-event triggers

- Status: active E2E proof infrastructure.
- Added date: 2026-10-07.
- Owning module/doc: E2E host (`src/Hosts/E2e`), two-host security-audit evidence.
- What: the E2E host compiles Main's `AuditCapacityAlertMonitor`,
  `AuditCapacityAlertServiceCollectionExtensions`, and `AuditCapacityAlertOptions`
  as linked source in `src/Hosts/E2e/E2e.csproj` and registers the real monitor,
  while `E2eAuditStorageSizeReader` replaces the production storage-size reader.
  The reader reports zero except during `POST /api/e2e/audit-evidence/storage-threshold/check`,
  which sets `E2eAuditCapacitySimulation` to report `CapacityWarningBytes + 1` for one
  real `CheckOnceAsync` and then clears it, so `security.audit.storage-threshold` is written
  on demand and no other screen shows the capacity warning. The other E2E-only
  `/api/e2e/audit-evidence` endpoints run the real Files integrity verification and
  tamper the E2E in-memory stored object so `security.data.integrity-verification`
  gets both a succeeded and a failed record. All of them require the platform-admin policy.
- Remove condition: remove the linked monitor, the reader replacement, and the
  trigger endpoints when the normal two-host flow can produce both events.
- Lesson for Codex: these switches exist only in the E2E host; Main has no
  threshold or integrity trigger. The monitor alerts once per exceeded period, so a
  second threshold check in the same host run within the 15-minute interval writes
  no new event.

## Temporary workarounds

Use dated entries only. Each entry must include:

- Status
- Added date
- Owning module/doc
- Remove condition
- Lesson for Codex

### Optional managed-user edit revision

- Status: active compatibility window.
- Added date: 2026-10-01.
- Owning module/doc: Users (`UpdateManagedUserCommandHandler`, `UpdateManagedUserRequest.Revision`).
- Remove condition: once no deployed client sends `PUT /api/modules/users/{userId}` without `revision` (the shared Users Web surface always sends it; the OpenAPI contract already marks the field required), reject a missing value as a concurrency conflict instead of skipping the stale-edit check.
- Lesson for Codex: a missing `revision` currently skips the stale-edit check; do not treat that as the intended steady state.

### Users contact-identifier index rollout

- Status: active rollout note.
- Added date: 2026-10-03.
- Owning module/doc: Users phone normalization and unique-identifier migrations.
- Remove condition: after all Users migrations have been applied to every
  deployed database and the rollout is closed.
- Lesson for Codex: `UsersModule.Initialize` applies migrations during host
  startup. If either duplicate guard or unique-index creation fails, that new
  host instance does not start; already-running instances continue on the old
  code until the duplicate accounts are resolved. Migration 1 reports account
  IDs only. Before applying migration 2, find duplicate normalized emails with:

  ```sql
  SELECT array_agg("Id" ORDER BY "Id") AS account_ids
  FROM "Users"."AspNetUsers"
  WHERE "NormalizedEmail" IS NOT NULL
  GROUP BY "NormalizedEmail"
  HAVING COUNT(*) > 1;
  ```

### Persian-digit change-phone E2E deferred

- Status: active debt.
- Added date: 2026-10-03.
- Owning module/doc: E2E host (`src/Hosts/E2e`), two-host Web tests.
- Remove condition: an E2E fake `ISmsProvider` outer adapter exists at E2E composition and the Persian-digit change-phone scenario passes in the two-host suite.
- Lesson for Codex: `BeginChangePhoneHandler` sends the SMS synchronously, so without a fake SMS provider every phone-change begin fails with `ProviderFailure`. The conversion itself is covered by `PhoneNumberFormatHandlerTests.Phone_change_converts_a_local_format_number_before_the_challenge_and_sms`. Do not add a production test-mode branch to unblock this.

### Production registry hosts pinned in /etc/hosts

- Status: active workaround.
- Added date: 2026-10-07.
- Owning module/doc: production server operations; `src/Hosts/deployment`.
- Remove condition: the hosting provider's resolver returns GitHub's real addresses for `ghcr.io` and `pkg-containers.githubusercontent.com` (check with `getent hosts ghcr.io` after removing the block), then delete the `# BEGIN registry-pin` … `# END registry-pin` block from the server's `/etc/hosts`.
- Lesson for Codex: the provider resolver answers both names with a proxy address (125.125.125.125). curl and openssl get through it, but Docker's TLS client fails, so image pulls and the deploy fail with TLS errors that look like registry outages. If pulls start failing again, compare `getent hosts ghcr.io` with `dig +short ghcr.io @1.1.1.1`; GitHub may have moved the pinned addresses.
