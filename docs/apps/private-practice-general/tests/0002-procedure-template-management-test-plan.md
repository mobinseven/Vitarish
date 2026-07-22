# Procedure Template Management Test Plan

Status: Future-state implementation plan  
Owner: Private Practice General app  
Verifies journey: [0029 Procedure Template Management](../journeys/0029-procedure-template-management-journey.md)  
Decision source: [Procedure Template Management Decision Tree](../decision-trees/0002-procedure-template-management-decision-tree.md)  
Last reviewed: 2026-07-22

## Traceability

| Journey stage | Deterministic coverage | Hot scenario |
| --- | --- | --- |
| Enter through Settings | English/Persian card copy, Font Awesome icon, emitted intent, route mapping, shared-panel composition, workspace-layout assignment. | Direct URL and Settings entry under owner/Journey protection. |
| Select a template | Loading, initial failure/retry, empty collection, populated selection, project create/edit/delete. | Refetch keeps a valid project ID and deterministically replaces a missing one. |
| Manage routines | Empty state, selection, create/edit cancellation and success, destructive confirmation, success/failure. | Delete selected routine and recover to an adjacent valid routine. |
| Edit ordered steps | Empty state, create/edit, earlier/later boundary controls, reorder success/failure, confirmed delete. | Keyboard-only operation and selected-step persistence after refetch. |
| Handle partial duplication | Full success, routine-create failure, sequential step-copy failure after routine creation. | Partial result is announced, authoritative data is refetched, and the created routine remains discoverable. |
| Recover | Query/mutation failures, retry, cancellation, stale selection IDs, no remaining item. | English LTR and Persian RTL with visible focus and localized accessible names/tooltips. |

## Automated boundaries

- **Clinic Web:** Settings card emits `{ kind: "procedureTemplateManagement" }`; both catalogs resolve title and description; existing panel tests cover loading, error, empty, selection, mutations, confirmation, reorder, and partial duplication branches.
- **Owner app:** navigation intent resolves to `Routes.Clinic.Settings.ProcedureTemplates`; the route composes only `ProcedureTemplateManagementPanel`; route-layout coverage asserts placement under `(clinic-workspace)`.
- **Architecture/docs:** Web UI, app boundary, flat app-doc placement, traceability links, localization parity, and whitespace validation remain green.
- **No contract churn:** assert the change contains no backend, migration, OpenAPI, generated-client, or patient-procedure modifications.

## Manual accessibility and browser matrix

Exercise desktop and narrow mobile widths in English LTR and Persian RTL. Enter from Settings and by direct URL; verify owner-route protection, three-pane resizing/overflow, keyboard traversal and activation, visible focus, localized tooltips, icon-only responsive controls, confirmation focus return, and text announcements for loading, failures, destructive results, and partial duplication. Refresh after each project/routine/step selection and after delete/reorder to verify deterministic persistence or recovery.

## Validation commands

- `npm run typecheck --workspace @vitarish/clinic-web`
- `npm run test --workspace @vitarish/clinic-web`
- `npm run typecheck --workspace @vitarish/private-practice-owner`
- Focused private-practice-owner navigation, route-composition, and route-layout tests
- `npm run validate:web-ui-boundary`
- `npm run validate:app-boundaries`
- `npm run validate:docs:app-boundaries`
- `npm run validate:localization`
- `git diff --check`
