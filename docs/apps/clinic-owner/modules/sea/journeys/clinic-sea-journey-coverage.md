# Clinic Owner Sea Journey Coverage

## 1. Context

- App: Clinic Owner
- Module: Sea usage in patient workflows
- Capability: Clinical translation of Sea project/procedure/event journeys
- Related apps: Clinic Owner
- Related modules: Sea, Medical, Appointments, Communications, Files, Users
- Product / service: Clinic patient procedure management
- Journey type: Future state translation of current Sea journeys with clinical gaps
- Scenario: Clinic operators use Sea's project hierarchy as patient-centered clinical workflow management: patients are projects, procedures sit under each patient, events are ordered clinical steps, and event items hold supporting evidence and follow-ups.
- User goal: Understand and complete clinical work under the right patient without learning generic project-management language.
- Business goal: Reuse Sea's workflow engine for Clinic while preserving clinical safety, privacy, accountability, and module boundaries.
- Scope start: A clinic operator opens a patient, patient procedure, clinical event, role, template, triage, or event-item surface backed by Sea.
- Scope end: Clinical workflow state is reviewed, created, updated, secured, committed, followed up, or intentionally deferred.
- Primary persona: Clinic owner / clinical coordinator
- Evidence sources: Platform Sea journey maps; Clinic Owner patient journey assessment; design objective from stakeholder request.
- Confidence level: Medium

## 2. Clinical Translation Matrix

| Sea concept | Clinic usage | Clinical source of truth | Do not treat as |
|---|---|---|---|
| Project | Patient profile container for procedure workflows | Clinic patient profile backed by Sea project identity and patient metadata | A generic project, appointment, prescription, or provider record |
| Project avatar | Patient photo or patient-recognition image where clinically appropriate | Files-backed avatar reference on patient/project | Clinical document evidence |
| Procedure | Procedure, treatment plan, care workflow, or clinical case under one patient | Sea procedure plus clinic-specific procedure metadata when required | Appointment booking, provider shift, or whole patient status |
| Template procedure | Reusable treatment protocol or standard procedure checklist | Template Sea project/procedure | Patient-specific clinical evidence until reviewed/adapted |
| Event | Ordered procedure step, visit milestone, consent step, measurement step, review, intervention, or follow-up checkpoint | Sea event chain under procedure | Free-floating note or appointment state |
| Committed event | Step marked clinically complete/accepted at the workflow level | Sea event committed state plus audit item | Legal signature or full medical chart attestation unless explicitly designed |
| Event item text | Clinical note, observation, measurement narrative, handoff note, or audit text for one event | Sea event attachment metadata / timeline item | Procedure-level note unless surfaced as such by a separate boundary |
| Event item file/media | Consent form, scan, image, lab attachment, wound photo, pre/post media, or supporting document | Files item linked through Sea event item metadata | Patient avatar or global patient document store |
| Reminder | Event-linked follow-up action or clinical/admin reminder | Communications reminder linked to Sea event source id | Procedure status or appointment reservation |
| Role | Clinical responsibility/access grouping such as provider, assistant, receptionist, billing reviewer, specialist, or supervisor | Sea role and user-role assignment | Employment role, provider credential, or medical license record |
| Event role | Sensitive-step read/write access rule | Sea event reader/writer roles | Patient-level consent model unless explicitly integrated |
| Plugin | Clinic-specific presentation or action extension | Sea plugin contract plus clinic module adapters | A duplicate clinical state store |

## 3. Exhaustive Clinic Journey Inventory

| Clinic journey | Primary persona | Platform Sea source | Clinic interpretation | Dedicated clinic doc |
|---|---|---|---|---|
| Patient discovery and selection | Receptionist / coordinator | Project discovery | Find the patient profile that needs procedure attention, unread items, or follow-up | `clinic-sea-patient-project-journey.md` |
| Patient profile create/edit/delete | Clinic owner / coordinator | Project create/edit/delete | Maintain patient identity container for procedures with clinical deletion/archival caution | `clinic-sea-patient-project-journey.md` |
| Patient photo/avatar | Coordinator / provider | Project avatar | Recognize patient safely; separate recognition image from evidence attachments | `clinic-sea-patient-project-journey.md` |
| Procedure list and selection | Provider / coordinator | Procedure list | Review patient treatment/care workflows and pick the active procedure | `clinic-sea-procedure-lifecycle-journey.md` |
| Manual procedure creation | Provider / coordinator | Manual procedure create | Create a patient-specific clinical procedure with duplicate and naming guardrails | `clinic-sea-procedure-lifecycle-journey.md` |
| Procedure template creation | Clinic owner / lead provider | Template project/procedure reuse | Maintain reusable protocols without converting template evidence into patient facts | `clinic-sea-template-reuse-journey.md` |
| Procedure template copy | Provider / coordinator | Create from template | Seed a patient's procedure from a standard protocol, then review/adapt copied steps | `clinic-sea-template-reuse-journey.md` |
| Procedure identity edit | Provider / coordinator | Procedure edit | Rename/describe clinical workflow while keeping status/provider/notes as explicit future boundaries | `clinic-sea-procedure-lifecycle-journey.md` |
| Procedure deletion/cancellation | Clinic owner / provider | Procedure delete | Prefer cancellation/closure semantics for clinical history; destructive delete needs consequence clarity | `clinic-sea-procedure-lifecycle-journey.md` |
| Ordered event review | Provider / assistant | Event list | Review clinical step sequence, committed state, redacted sensitive rows, and next action | `clinic-sea-event-lifecycle-journey.md` |
| Event creation | Provider / assistant | Event create | Add a missing clinical step/milestone at the correct location in the procedure | `clinic-sea-event-lifecycle-journey.md` |
| Event reorder | Provider / coordinator | Event chain move | Correct clinical step order while preserving integrity and audit expectations | `clinic-sea-event-lifecycle-journey.md` |
| Event access edit | Access admin / provider | Event roles | Restrict sensitive steps such as private notes, financial reviews, or specialist-only details | `clinic-sea-access-and-roles-journey.md` |
| Event commit/uncommit | Provider / coordinator | Event commit/uncommit | Mark a clinical step as complete/accepted or reopen it with visible audit context | `clinic-sea-event-lifecycle-journey.md` |
| Event deletion | Clinic owner / provider | Event delete | Remove erroneous step only with clear impact on notes, files, reminders, and audit trail | `clinic-sea-event-lifecycle-journey.md` |
| Clinical note entry | Provider / assistant | Text attachment | Add step-level observations and handoff notes to the event timeline | `clinic-sea-event-items-journey.md` |
| Clinical file/media upload | Provider / assistant | File/media attachment | Attach consent forms, scans, photos, videos, and procedure evidence to the correct event | `clinic-sea-event-items-journey.md` |
| Reminder creation | Coordinator / assistant | Event reminder | Schedule event-linked follow-up without changing appointment reservation state | `clinic-sea-event-items-journey.md` |
| Timeline review | Provider / coordinator | Event items timeline | Read clinical notes, files, reminders, plugin items, and audit notes in chronological context | `clinic-sea-event-items-journey.md` |
| Role vocabulary | Clinic owner / access admin | Role management | Define clinical access/responsibility groups with patient privacy in mind | `clinic-sea-access-and-roles-journey.md` |
| User role assignment | Clinic owner / access admin | User role assignment | Grant read/write scope for providers, assistants, receptionists, and supervisors | `clinic-sea-access-and-roles-journey.md` |
| Sensitive event redaction | Provider / participant | Access-controlled event rendering | Preserve procedure sequence while hiding restricted clinical details | `clinic-sea-access-and-roles-journey.md` |
| Patient/procedure stats | Coordinator / owner | Stats/progress | Triage patients and procedures by unread evidence, incomplete steps, reminders, and progress | `clinic-sea-observability-and-triage-journey.md` |
| Clinic project plugins | Clinic module owner | Project plugins | Replace/augment generic project UI with patient-oriented summaries and actions | `clinic-sea-plugin-extension-journey.md` |
| Clinic event item plugins | Clinic module owner | Event item plugins | Render medical-specific timeline cards without bypassing Sea event item ownership | `clinic-sea-plugin-extension-journey.md` |

## 4. Clinical Command / Query Split

| Clinical action | Query/read behavior | Command/write behavior |
|---|---|---|
| Find patient/procedure | Search, filter, and attention indicators read patient/project and procedure summaries | No state change |
| Create patient profile | Duplicate candidate lookup remains read-only | Create patient/project only after operator confirmation |
| Create procedure | Template preview and duplicate name checks are read-only | Create procedure manually or from template |
| Review event timeline | Load ordered events and event items | No state change until note/file/reminder/commit/edit action |
| Add clinical note/file | Display existing event items | Add event item metadata and Files content through the event item command path |
| Commit step | Display current event state and audit history | Commit/uncommit event and write audit note |
| Assign access | Preview roles and affected users | Save role or event-role assignments |
| Triage unread work | Display counts/progress/reminders | Marking, committing, adding reminders, or editing happen through the owning flows only |

## 5. Cross-Journey Clinical Risks and Opportunities

### Risks

- Generic Sea labels can make clinical users think in project terminology instead of patient care terminology.
- Procedure progress can be inferred from event stats, but clinical status such as planned, active, completed, or cancelled needs an explicit clinic procedure boundary when required.
- Committed event state is useful for workflow readiness, but should not be described as a regulated clinical attestation without a dedicated decision.
- Copied template attachments may be mistaken for patient-specific evidence unless provenance and review state are clear.
- Event-level roles can protect sensitive steps, but they do not replace patient-level consent, provider credentialing, or organization policy.
- Appointment reminders and event reminders can be confused unless the UI labels reminder source and consequence clearly.

### Opportunities

- Use clinic-specific labels: patient, procedure, step, note, evidence, follow-up, care team, and restricted step.
- Add patient/procedure overview summaries that translate Sea stats into clinical attention signals.
- Add template preview and post-copy review checklists for standard protocols.
- Add provenance labels for copied event items and uploaded clinical media.
- Add role impact previews for sensitive clinical event access.
- Add explicit procedure status, provider assignment, procedure-level notes, and cancellation boundaries only when implemented as clinic-specific extensions rather than hidden inside generic Sea fields.

## 6. Documentation Architecture Audit

Artifact: `clinic-sea-journey-coverage.md`  
Type: Documentation journey coverage index  
Responsibility: Translate every platform Sea journey area into its Clinic Owner patient-centered clinical usage.

- SRP: Pass — covers only clinic translation coverage for Sea journeys.
- Single state model: Pass — keeps Sea hierarchy authoritative for workflow state and names related-module boundaries.
- Low coupling: Pass — describes Medical, Appointments, Communications, Files, and Users as related owners rather than embedded Sea responsibilities.
- Abstraction: Pass — maps concepts without requiring implementation details.
- Command-query: Pass — separates read-only review/preview from write actions.
- Failure safety: Pass — identifies clinical risks around deletion, access, copied evidence, and commit semantics.
- Open/Closed: Pass — extends platform journey documentation through app-scoped docs.
- Core protection: N/A — documentation only.
- KISS/YAGNI: Pass — no speculative production architecture is introduced.
- Single source of truth: Pass — states platform Sea docs remain the behavior source while this file owns clinic usage translation.
