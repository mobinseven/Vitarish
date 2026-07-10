# Clinic Sea Clinical Procedure Lifecycle Journey

## 1. Context

- App: Clinic Owner
- Module: Sea usage in patient workflows
- Capability: Clinical procedure management under a patient
- Related modules: Sea, Medical, Users, Files, Communications
- Journey type: Future state translation of current Sea procedure lifecycle with clinical gaps
- Scenario: Provider or coordinator creates, reviews, edits, templates, closes, or cancels procedures under a patient profile.
- User goal: Track each patient procedure with clear status, ownership, notes, and next steps.
- Business goal: Preserve treatment workflow history without mixing it with patient identity or appointment scheduling.
- Scope start: Operator opens a patient's procedures.
- Scope end: Procedure is opened, created, copied from template, edited, cancelled/closed, or deleted with explicit consequence.
- Primary persona: Provider / clinical coordinator
- Evidence sources: Platform Sea Procedure Lifecycle and Template Reuse journeys; Clinic Owner patient procedure journey.
- Confidence level: Medium

## 2. Clinical Journey Stages

### Stage 1: Review patient procedures

- **Clinical actions:** Scan procedure names, active/completed/cancelled indicators, assigned provider, new evidence, incomplete steps, and reminders.
- **Sea source:** Procedure list with stats.
- **User questions:** Which treatment is active? Which has new findings? Which provider owns it?
- **Pain points:** Sea stats expose activity, but not explicit clinical lifecycle status or provider assignment.
- **Needs:** Procedure status, provider assignment, latest activity, and clear empty state.
- **Opportunities:** Add clinic procedure summary fields sourced from a clinic-specific boundary while keeping Sea events as step facts.
- **Success metrics:** Time to choose procedure; missed follow-up rate; procedure reopen frequency.

### Stage 2: Create procedure manually

- **Clinical actions:** Add a patient-specific procedure; enter clinical name/description; select provider when required; avoid duplicate active procedures.
- **Sea source:** Manual procedure create and duplicate name check.
- **User questions:** Is this the right procedure for this patient? Has it already been started?
- **Pain points:** Name-only duplicate checks are insufficient for clinical duplication risks.
- **Needs:** Inline duplicate warning, patient context confirmation, save failure recovery, authorization feedback.
- **Opportunities:** Add clinic-specific procedure category/provider metadata only when required by current workflows.
- **Success metrics:** Duplicate procedure attempts; creation completion; provider assignment omissions.

### Stage 3: Create from treatment template

- **Clinical actions:** Choose a standard protocol; preview steps; create under the patient; review copied roles/attachments; adapt to the patient's situation.
- **Sea source:** Procedure template browser and copy operation.
- **User questions:** What steps and evidence will copy? Which items are instructions versus patient facts?
- **Pain points:** Template event and attachment preview/provenance is not fully visible.
- **Needs:** Pre-copy preview and post-copy checklist.
- **Opportunities:** Mark copied attachments/notes as template-origin until reviewed.
- **Success metrics:** Template use rate; immediate post-copy edits; stale template evidence incidents.

### Stage 4: Maintain procedure identity

- **Clinical actions:** Rename procedure, clarify description, correct provider/status/notes through their owning boundaries.
- **Sea source:** Procedure edit identity.
- **User questions:** Is this identity change separate from clinical status? Will event history remain intact?
- **Pain points:** Generic name/description cannot safely carry status, provider assignment, cancellation reason, or procedure-level note history.
- **Needs:** Separate explicit fields and command boundaries for lifecycle, assignment, and notes if implemented.
- **Opportunities:** Use procedure identity for naming only; add clinic extensions for status/provider/notes.
- **Success metrics:** Procedure identity correction rate; status confusion reports.

### Stage 5: Track clinical procedure status

- **Clinical actions:** Move procedure from planned to active, completed, cancelled, or closed when clinically appropriate.
- **Sea source:** Not explicit in current Sea procedure lifecycle; currently inferred from event stats.
- **User questions:** Is the procedure actually done or merely all current steps committed?
- **Pain points:** Inferred progress is not the same as clinical lifecycle state.
- **Needs:** One explicit clinic procedure status source of truth if status is required.
- **Opportunities:** Add a clinic procedure lifecycle boundary distinct from Sea event commits.
- **Success metrics:** Status transition accuracy; cancelled procedure retrieval; completed procedure review time.

### Stage 6: Add procedure-level notes without mixing levels

- **Clinical actions:** Record procedure-wide rationale, plan, summary, or cross-step note.
- **Sea source:** Event item notes exist at event level, not procedure level.
- **User questions:** Is this note about the whole procedure or a specific step?
- **Pain points:** Forcing all notes into events can obscure procedure-wide context.
- **Needs:** Dedicated procedure-note boundary when procedure-wide notes are required.
- **Opportunities:** Link procedure notes to event timeline context without duplicating event item state.
- **Success metrics:** Misfiled note corrections; note retrieval time.

### Stage 7: Cancel, close, or delete safely

- **Clinical actions:** Cancel with reason, close with outcome, or delete only erroneous procedure records.
- **Sea source:** Procedure delete strict confirmation.
- **User questions:** What clinical history disappears? Is cancellation the correct action?
- **Pain points:** Destructive delete does not express clinical cancellation semantics.
- **Needs:** Terminal status, reason, timestamp, author, audit trail, consequence summary.
- **Opportunities:** Reserve deletion for mistakes; use explicit cancellation/closure for real clinical history.
- **Success metrics:** Delete cancellation rate; cancellation reason completeness; audit review success.

## 3. Boundary Notes

- Sea procedure owns workflow containment under patient.
- Clinic extension should own clinical lifecycle status, provider assignment, and procedure-level notes when implemented.
- Event commits remain step-level facts and should not be the sole procedure status source.

## 4. Documentation Architecture Audit

Artifact: `clinic-sea-procedure-lifecycle-journey.md`  
Type: Documentation journey  
Responsibility: Translate Sea procedure lifecycle into Clinic clinical procedure usage and identify explicit clinic extension gaps.

- SRP: Pass — covers only procedure lifecycle under a patient.
- Single state model: Pass — procedure containment stays in Sea; clinical status/provider/note extensions are explicitly bounded.
- Low coupling: Pass — appointments and medical references are not folded into procedure state.
- Abstraction: Pass — describes boundaries, not infrastructure.
- Command-query: Pass — review/preview reads are separate from create/edit/status/cancel commands.
- Failure safety: Pass — duplicate, stale template, deletion, and invalid status risks are documented.
- Open/Closed: Pass — recommends extension boundaries rather than modifying generic Sea meaning.
- Core protection: N/A — documentation only.
- KISS/YAGNI: Pass — status/provider/notes are recommended only where current workflows require them.
- Single source of truth: Pass — names one owner per state category.
