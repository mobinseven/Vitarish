# Patient Procedure Clinical Workspace Test Plan

Status: Future-state implementation plan  
Owner: Private Practice General app  
Verifies journey: [0028 Patient Procedure Clinical Workspace](../journeys/0028-patient-procedure-workspace-journey.md)  
Decision source: [Patient Procedure Clinical Workspace Decision Tree](../decision-trees/0001-patient-procedure-workspace-decision-tree.md)  
Last reviewed: 2026-07-13

## Traceability

| Journey stage | Deterministic coverage | Hot scenario |
| --- | --- | --- |
| Orient | Loading, access failure, not-found, identity, completion summary, patient return navigation. | Desktop/mobile layout; Persian RTL and English LTR. |
| Scan steps | Empty state, ordered statuses, deterministic default, deep-link selection, localized Earlier/Later, read-only actions. | Keyboard/focus flow and browser history. |
| Review step | Desktop selected detail, mobile Sheet close navigation, invalid event recovery. | Mobile viewport; back/refresh on event URL. |
| Review activity | Selected-only query, newest-first order, type/time/MIME/size, filename and legacy fallback. | Filename survives upload and reload. |
| Add evidence | Note validation/success/failure with draft preservation; single-file success/failure/retry/remove. | Network loss without draft/file loss. |
| Add/manage step | Create, edit, complete/reopen, reorder boundaries/failure, delete confirmation/recovery. | Read-only versus write permission against a running host. |

## Boundary coverage

- **Documentation:** validate flat app placement and decision-tree -> journey -> test links; keep journeys 0022/0023 as current-state evidence.
- **SEA domain/persistence:** filename mapping, nullable legacy values, bounded metadata, control/path separator rejection, persistence round trip, and migration compatibility.
- **Contract/OpenAPI:** nullable `fileName` serialization on attachment write/read DTOs and unchanged routes/operation IDs.
- **Generated Web boundary:** regenerate Orval and verify generated DTO, SEA wrapper, browser-file conversion, read mapper, and Clinic presentation mapping.
- **Localization:** English/Persian catalog parity plus locale-aware file-size formatting.
- **React:** component tests for all deterministic journey branches, accessible names, semantic time, note draft retention, and failed upload retry.

## Validation commands

- Focused SEA tests and build.
- SEA, Clinic, and Web localization workspace tests and typechecks.
- OpenAPI document refresh and package-owned Orval generation.
- App-document, localization, API/OpenAPI, Web import-boundary, and architecture validators.
- All-modules host build using `ModuleBuildProfile=All`.

## Hot browser matrix

Validate desktop and mobile in Persian RTL and English LTR. Exercise keyboard selection/focus, event-URL back and refresh, writable and read-only users, filename persistence after reload, and network failure/retry without losing local drafts. Browser checks require a running backend and representative authorization data; mocked component tests do not substitute for them.

## Deferred tests

Resumable/batch uploads, real transfer progress, large-media policy, Files-module migration, actor presentation, analytics, notifications, and creation of reminder/audit/plugin items are deferred with their owning product scope.
