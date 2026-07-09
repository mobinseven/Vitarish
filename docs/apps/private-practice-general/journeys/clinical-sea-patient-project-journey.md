# Clinic Sea Patient as Project Journey

## 1. Context

- App: Clinic Owner
- Module: Sea usage in patient workflows
- Capability: Patient profile as Sea project
- Related modules: Sea, Files, Users, Appointments, Medical
- Journey type: Future state translation of current Sea project lifecycle with clinical gaps
- Scenario: Clinic operator finds, creates, edits, personalizes, opens, and retires patient profiles that use Sea projects as their workflow container.
- User goal: Work on the correct patient and see procedure activity without generic project-management friction.
- Business goal: Anchor all clinical procedure workflows to one patient profile while protecting clinical history.
- Scope start: Operator opens the patient list or patient profile.
- Scope end: Patient profile is selected, saved, cancelled, photo-updated, archived/deleted, or used to enter procedures.
- Primary persona: Receptionist / clinical coordinator
- Evidence sources: Platform Sea Project Lifecycle journey; Clinic Owner patient journey assessment; stakeholder design objective.
- Confidence level: Medium

## 2. Clinical Journey Stages

### Stage 1: Discover patients needing attention

- **Clinical actions:** Search patient names/identifiers; scan unread procedure activity; prioritize patients with new notes, pending follow-ups, incomplete procedures, or recent uploads.
- **Sea source:** Project discovery through project grid and stats.
- **Clinic touchpoints:** Patient list, patient search, patient overview, procedure summary cards.
- **User questions:** Is this the right patient? Which patient has new clinical work? Is urgent follow-up hidden under a generic count?
- **Emotions:** Oriented when labels say patient/procedure; anxious if the list looks like projects.
- **Pain points:** Generic project sorting by unread/name does not explain clinical urgency; patient search needs demographics and identifiers, not only project name.
- **Needs:** Patient-safe identifiers, status/flag visibility, clear new clinical activity cues, and empty/error states.
- **Opportunities:** Translate Sea stats into patient attention badges such as new evidence, pending reminders, incomplete steps, and restricted updates.
- **Success metrics:** Time to find patient; wrong-patient navigation reports; percent of opens from attention indicators.

### Stage 2: Create patient profile as clinical project

- **Clinical actions:** Enter patient identity and required demographics; link or create user account when needed; confirm duplicate candidates before save.
- **Sea source:** Project create/edit identity.
- **Clinic touchpoints:** Patient create form, duplicate review, patient metadata fields.
- **User questions:** Has this patient already been registered? Which fields are clinically required? Will procedure workflows attach to this patient?
- **Emotions:** Careful because duplicate profiles fragment clinical history.
- **Pain points:** Sea requires a project name, but clinic needs normalized patient identity, duplicate prevention, and demographic validation.
- **Needs:** One patient profile per real patient, explicit patient metadata ownership, safe cancellation, recoverable save failures.
- **Opportunities:** Keep Sea project name as display identity while clinic patient metadata supplies clinical identity details.
- **Success metrics:** Duplicate override rate; patient create completion; failed validation recovery rate.

### Stage 3: Edit patient identity and profile details

- **Clinical actions:** Update contact/demographic metadata; review linked user; keep procedure history intact.
- **Sea source:** Project edit form.
- **Clinic touchpoints:** Patient edit page, patient details panel, patient overview.
- **User questions:** Am I changing identity or only display details? Will this affect procedures, appointments, or prescriptions?
- **Emotions:** Cautious when editing identifying data.
- **Pain points:** Generic project description fields cannot safely carry all clinical patient metadata.
- **Needs:** Explicit patient fields, optimistic/concurrency feedback, audit expectations for sensitive changes.
- **Opportunities:** Separate patient identity edits from procedure workflow edits while preserving Sea project linkage.
- **Success metrics:** Identity correction rate; failed update rate; support tickets about inconsistent patient details.

### Stage 4: Personalize with patient photo/avatar

- **Clinical actions:** Upload or remove a patient-recognition image.
- **Sea source:** Project avatar through Files.
- **Clinic touchpoints:** Patient avatar/photo control, Files upload/preview.
- **User questions:** Is this a recognition photo or clinical evidence? Are upload constraints clear?
- **Emotions:** Reassured when photo helps verify the patient.
- **Pain points:** Avatar upload is file-backed but not an event item; users may confuse it with clinical media evidence.
- **Needs:** Clear label, consent/privacy guidance, type/size failure feedback from Files.
- **Opportunities:** Add copy that distinguishes patient photo from procedure attachments.
- **Success metrics:** Avatar upload success; wrong-patient selection reduction; upload failure recovery.

### Stage 5: Open patient profile and choose clinical work

- **Clinical actions:** Review patient summary; choose procedure, appointments, reminders, or profile edit.
- **Sea source:** Project details and procedure grid.
- **Clinic touchpoints:** Patient overview, procedure grid, activity feed.
- **User questions:** What is active now? What changed since last review? Which procedure needs action?
- **Emotions:** Confident when one overview answers the next-action question.
- **Pain points:** Sea project details focus on identity; clinical health is distributed across procedures, events, appointments, and reminders.
- **Needs:** Patient overview read model that composes summaries without moving source state.
- **Opportunities:** Show current procedures, next appointment, important flags, and latest event items.
- **Success metrics:** Drill-down accuracy; time to next action; overview refresh failures.

### Stage 6: Retire, archive, or delete patient profile safely

- **Clinical actions:** Close inactive patient, mark status, or delete only when truly erroneous and permitted.
- **Sea source:** Project delete strict confirmation.
- **Clinic touchpoints:** Patient settings/status action, destructive confirmation.
- **User questions:** What clinical history will be hidden or removed? Is archive safer than delete?
- **Emotions:** Highly cautious because patient history is sensitive.
- **Pain points:** Sea deletion removes project workflow context; clinic usually needs lifecycle status rather than destructive deletion.
- **Needs:** Archive/inactive status, consequence summary, authorization, audit trail, no silent removal from clinical history.
- **Opportunities:** Treat destructive delete as exceptional; prefer patient status and procedure cancellation/closure flows.
- **Success metrics:** Delete cancellation rate; accidental deletion reports; inactive patient retrieval success.

## 3. Boundary Notes

- Patient identity belongs to the Clinic patient profile boundary; Sea project identity anchors workflow containment.
- Files owns upload/stream behavior for avatar images.
- Appointments and Medical may reference the patient but do not become Sea project state.

## 4. Documentation Architecture Audit

Artifact: `clinic-sea-patient-project-journey.md`  
Type: Documentation journey  
Responsibility: Explain how Sea project lifecycle maps to Clinic patient profile workflows.

- SRP: Pass — covers only patient-as-project usage.
- Single state model: Pass — patient profile is the clinical anchor and Sea project is the workflow container.
- Low coupling: Pass — related modules are referenced through boundaries.
- Abstraction: Pass — no infrastructure dependency is introduced.
- Command-query: Pass — discovery/overview reads are separate from create/edit/delete actions.
- Failure safety: Pass — duplicate, upload, concurrency, privacy, and deletion risks are explicit.
- Open/Closed: Pass — extends Sea semantics in clinic documentation.
- Core protection: N/A — documentation only.
- KISS/YAGNI: Pass — avoids adding unrequested implementation layers.
- Single source of truth: Pass — separates patient identity, avatar Files state, and Sea workflow state.
