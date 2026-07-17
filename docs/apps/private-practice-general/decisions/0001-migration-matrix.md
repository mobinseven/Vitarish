# Migration Matrix: ADR-0008 App Experience Boundaries

Date: 2026-07-10
Status: Complete
Owner: Platform Architecture
Scope: private-practice-general feature-to-package migration

## Generated Client Module Ownership

| Generated module | Spec file | Primary feature consumer | Target package | Notes |
|---|---|---|---|---|
| `authentication` | Main_authentication.json | auth (hooks/auth/\*) | auth-web | |
| `users` | Main_users.json | auth, account, sea (role users) | auth-web | account-web/sea-web import public auth-web exports |
| `appointments` | Main_appointments.json | clinic (appointments/) | clinic-web | |
| `clinic` | Main_clinic.json | clinic (patients/, types.ts) | clinic-web | |
| `medical` | Main_medical.json | clinic | clinic-web | |
| `sea` | Main_sea.json | sea (all subdirs) | sea-web | clinic/patients/procedures also wraps SEA ops |
| `journey` | Main_journey.json | journey | journey-web | |
| `knowledge` | Main_knowledge.json | knowledge-web | knowledge-web | Already aligned |
| `parties` | Main_parties.json | none (unused) | hold | Audit consumers before assigning |
| `files` | Main_files.json | none (unused) | hold | Audit consumers before assigning |
| `search` | Main_search.json | none (unused) | hold | Audit consumers before assigning |
| `feed` | Main_feed.json | none (unused) | hold | |
| `social` | Main_social.json | none (unused) | hold | |
| `communications` | Main_communications.json | none (unused) | hold | |
| `commerce` | Main_commerce.json | none (unused) | hold | |
| `intelligence` | Main_intelligence.json | live dictation capability | intelligence-web | Extracted from clinic-web; generated client remains package-internal |
| `requests` | Main_requests.json | none (unused) | hold | |

## Feature File Ownership

| Artifact path | Current location | Target package | Phase |
|---|---|---|---|
| **apiFetch.ts** | src/api/client/apiFetch.ts | @vitarish/web-api-client | 1a |
| **apiFetch.test.ts** | src/__tests__/api/apiFetch.test.ts | @vitarish/web-api-client | 1a |
| **orval.config.ts** | src/api/client/orval.config.ts | Split per feature package | per phase |
| **localization/** | src/localization/ | @vitarish/web-localization (infra) + feature packages (catalogs) | 1b + per phase |
| **features/auth/session/** | src/features/auth/ | @vitarish/auth-web | 3 |
| **hooks/auth/\*** | src/hooks/auth/ (8 files) | @vitarish/auth-web | 3 |
| **features/account/hooks/\*** | src/features/account/hooks/ | @vitarish/account-web | 4 |
| **features/account/theme/** | src/features/account/theme/ | @vitarish/account-web | 4 |
| **features/account/passkeyRegistrationErrors.ts** | src/features/account/ | @vitarish/account-web | 4 |
| **features/journey/\*** | src/features/journey/ | @vitarish/journey-web | 5 |
| **hooks/journey/\*** | src/hooks/journey/ (3 files) | @vitarish/journey-web | 5 |
| **features/clinic/\*** | src/features/clinic/ | @vitarish/clinic-web | 6 |
| **features/clinic/patients/procedures/api/** | src/features/clinic/patients/procedures/api/ | @vitarish/sea-web (wraps SEA ops) | 7 |
| **features/sea/\*** | src/features/sea/ | @vitarish/sea-web | 7 |
| **features/home/** | src/features/home/ | app-owned (stay) | 8 |
| **features/landing/** | src/features/landing/ | app-owned (stay) | 8 |

## Cross-Feature Dependencies (Must Resolve Before Migration)

| Source feature | Target feature | File | Resolution |
|---|---|---|---|
| clinic | sea | features/clinic/patients/procedures/api/proceduresApi.ts imports features/sea/shared/models/\* and api/generated/sea/\* | Move proceduresApi.ts to sea-web; clinic-web consumes sea-web exports |
| sea | clinic | features/sea/templates/components/SeaTemplateProjectDetailPage.tsx imports ClinicDetailGrid from features/clinic/components/ | Promote ClinicDetailGrid to @vitarish/web-ui (generic presentation) |
| sea | clinic | features/sea/projects/components/SeaProjectDetailPage.tsx imports ClinicDetailGrid from features/clinic/components/ | Same as above |

## App-Level Hook Ownership

| Hook path | Target package | Phase |
|---|---|---|
| hooks/auth/usePhoneLogin.ts | auth-web | 3 |
| hooks/auth/usePasswordLogin.ts | auth-web | 3 |
| hooks/auth/usePasskeyLogin.ts | auth-web | 3 |
| hooks/auth/useEmailLogin.ts | auth-web | 3 |
| hooks/auth/useLogout.ts | auth-web | 3 |
| hooks/auth/useAuthSession.ts | auth-web | 3 |
| hooks/auth/useTenantAuthorization.ts | auth-web | 3 |
| hooks/auth/useOtpCodeCapture.ts | auth-web | 3 |
| hooks/auth/useOtpExpiry.ts | auth-web | 3 |
| hooks/auth/requestedReturnUrl.ts | auth-web | 3 |
| hooks/journey/useCurrentJourney.ts | journey-web | 5 |
| hooks/journey/resolvePostLoginNavigation.ts | journey-web | 5 |
| hooks/journey/index.ts | journey-web | 5 |

## Validation Notes

- No duplicate apiFetch implementations allowed across packages
- Generated clients remain package-internal; public exports only through feature package index.ts
- feature packages import from @vitarish/web-api-client, not from app-local apiFetch
- apps compose from package public exports, not deep-import internals
