# Prescription workspaces plan

Date: 2026-11-09
Owner: Medical module, with Clinic Web
Status: Proposed, planning only. Implementation is not authorized until the owner approves the slices.
Removal condition: delete this file when every slice has shipped and `journeys/implementation-matrix.md` describes the shipped surfaces.

## Outcome

Two full-featured workspaces:

1. **Doctor workspace** (patient record, Prescriptions tab): browse the patient's full prescription history, open any prescription, start a new one from scratch, from a template or as a repeat of a past one, resume or discard unfinished drafts, edit drafts, edit and cancel submitted prescriptions under the provider's rules, see each prescription's amendment history and recovery state, and give the patient the tracking code.
2. **Owner workspace** (`/settings/prescription-templates`): browse, create, edit, duplicate, archive and restore prescription templates, with the same item editing the doctor uses.

Categories in scope: drugs (already works), laboratory and other paraclinical, visits and medical services, physiotherapy and dental.

## Non-goals

- Referral, hospitalization, nursing and referral-feedback prescriptions.
- Point-in-time snapshots of past versions beyond the amendment history; that is issue #983.
- A second provider integration. The workspaces stay provider-neutral and Tamin is the only provider proven end to end.
- Reviving the `/owner/clinic/prescriptions/...` routes described in `journeys/implementation-matrix.md`; none of that UI exists any more.

## Repository evidence

- Current surfaces: `PatientPrescriptionSubmissionSheet`, `PatientPrescriptionDashboard`, `PrescriptionEventFeedItem`, `PrescriptionTemplateWorkspace` under `src/Hosts/Web/shared/clinic/src/components`. The journey matrix names pages that were removed (`OwnerPrescriptionsPage`, `PrescriptionItemsGrid`, ...), so it is stale and is rewritten in the last slice.
- Backend operations that exist but the UI does not use: update and remove draft items, update draft, cancel, refresh provider status, escalate, template get, save, archive, add item and remove item. Missing: update a template item, unarchive, duplicate, discard a draft, history paging, and a server-computed list of allowed actions.
- Template endpoints take a `tenantId` in the body, but `PrescriptionTemplateAuthorization` compares it with the authenticated tenant and requires the owner role to manage and owner or staff to apply. They are not a trust hole; the body field is redundant.
- The Tamin catalog sync loads service type `02` only, so visits, medical services and the other paraclinical types cannot be picked at all.
- Contract and domain enums list members in a different order (#1011); status chips and history filters depend on that being fixed.
- Tamin rules (guide pp. 32-33): a prescription can be edited once, before pharmacy action, and not for visit or medical-service prescriptions; it can be cancelled until the end of the day it was registered.

## Decisions taken

| Question | Decision |
|---|---|
| Doctor layout | Prescriptions tab with a list on one side and the selected prescription on the other; list-then-detail on narrow screens. |
| Template model | Template names are provider-neutral; each item keeps the provider catalog code it came from. Applying a template to a draft of another provider skips non-matching items and reports them. The owner manages, staff apply. |
| Extras | Repeat a previous prescription; resume or discard drafts; print or send the tracking code; amendment history per prescription. |

## Design

**One owner for "what may I do".** The server computes the allowed actions for a prescription (edit, cancel, resubmit, discard, repeat), each with a localized reason when not allowed, from status, provider rules and capabilities. The Web only displays them. Provider rules (edit once, same-day cancel, no edit for visits) live in the provider adapter's capability surface, not in React.

**One items editor.** Search, per-category item form, quantity and duration, in a single component used by draft editing, editing a submitted prescription, and template editing. Per-category forms: drug (consumption picker), paraclinical (service type from the code), visit (no items), medical service, physiotherapy (illness and plan from the provider lists), dental (tooth and dentist rules). Item metadata stays the transport for provider-specific values, with its keys defined once.

**Catalog.** The sync requests every service type and stores each code's type, so category and service type come from the data instead of defaults.

**Trust.** Every new command and query resolves doctor, clinic and tenant from the authenticated scope, never from the body. New endpoints get an authorization-matrix row with a real test, an operation ID, and a route constant.

**Compatibility.** The prescription feature is new, so no persisted data or published contract needs a compatibility window. Migrations are still additive and generated with the canonical command.

## Slices

Each slice is independently valid: it builds, passes its tests, and leaves the product working.

| # | Slice | Acceptance |
|---|---|---|
| 0 | Backend foundations: allowed-actions computation, draft resume and discard, template item update, duplicate and unarchive, history paging and filters (status, provider, date, tracking code), enum parity (#1011), full service-type catalog sync | Unit tests per rule (each Tamin edit and cancel rule, scope refusal for another doctor or clinic); both architecture validators clean; spec and generated client regenerated |
| 1 | Shared items editor with all category forms and the consumption picker | Component tests per category; drug flow unchanged in the E2E spec |
| 2 | Doctor workspace: list, filters, paging, detail with items, eligibility, provider status, tracking code, amendment history, recovery states | Mock-runtime handlers; screenshots inspected at every locale, theme and viewport the specs cover |
| 3 | Draft lifecycle: new from scratch, from a template or as a repeat; resume; discard; edit items | E2E: create, edit, resume, discard |
| 4 | Edit and cancel submitted prescriptions with confirmation and reasons; the E2E Tamin stub enforces edit-once and same-day cancel | E2E: second edit refused with the reason shown; cancel accepted |
| 5 | Patient output: printable summary and tracking code message | Message goes through the Communications module under its consent rules; printable view has no internal ids |
| 6 | Owner template workspace: list, search, active and archived, editor on the shared items editor, duplicate, archive, restore, apply skips and reports non-matching items | E2E: create a template, apply it, submit |
| 7 | Rewrite `journeys/implementation-matrix.md` and the journeys to the shipped surfaces; remove this plan | Matrix names only routes and components that exist |

## Validation

- Unit: allowed-actions rules, scope refusals, template apply with mixed providers, catalog sync by type.
- Contract: both architecture validators; authorization-matrix rows with real tests; regenerated OpenAPI spec and Orval client; mock-runtime handlers.
- Web: `npm run lint:layout`, shared clinic tests, `npm run specs` with every flag and the screenshots inspected.
- E2E: extend `tamin-drug-prescription.spec.ts`; run the full two-host suite before each slice that crosses the API boundary. The four failures on main are tracked in #1013 and must not grow.
- Hot scenarios: a patient with hundreds of prescriptions (paging, not loading all), two doctors in one clinic (each sees only what scope allows), edit after the provider day boundary.

## Risks

- **Provider rules unverified live.** Edit-once and same-day cancel come from the guide; nothing has run against live Tamin. The adapter should treat a provider refusal as the authority and surface its message.
- **Entitlement response shape unverified** (#1014); a wrong mapping blocks every submission.
- **Physiotherapy and dental need provider lists and rules** (illness and plan lists, tooth codes, dentist rule checks) that are untyped in the SDK; expect pilot data to adjust the item forms.
- **Scope.** Seven slices; slices 5 and 6 can ship in either order after 4.

## Rejected alternatives

- Reviving the old owner routes: they no longer exist and would duplicate the patient record.
- Computing allowed actions in the Web: breaks the rule that the Web displays security and policy state but never decides it.
- Three separate item editors (draft, submitted edit, template): triples the category forms and keeps the template gap.
- One template per provider: forces the owner to maintain copies.

## Revision triggers

- Live Tamin contradicts an edit or cancel rule.
- The entitlement response shape differs from what the mapper reads.
- Issue #983 changes what amendment history can show.
- The service-type sync shows the catalog is too large to sync in one request.
