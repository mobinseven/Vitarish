# Clinic Sea Clinical Procedure Event Journey

## 1. Context

- App: Clinic Owner
- Module: Sea usage in patient workflows
- Capability: Ordered clinical event / procedure step management
- Related modules: Sea, Users, Files, Communications
- Journey type: Future state translation of current Sea event lifecycle with clinical gaps
- Scenario: Clinical staff create, order, edit, secure, commit, reopen, and remove procedure events under a patient procedure.
- User goal: Keep clinical steps in the right order with clear completion, accountability, and evidence context.
- Business goal: Make procedure progress traceable without confusing step completion with patient or procedure lifecycle state.
- Scope start: User opens a patient procedure event list.
- Scope end: Event is selected, created, reordered, edited, access-controlled, committed/uncommitted, or deleted.
- Primary persona: Provider / assistant
- Evidence sources: Platform Sea Event Lifecycle journey; Clinic Owner patient procedure event journey.
- Confidence level: Medium

## 2. Clinical Journey Stages

### Stage 1: Review ordered clinical steps

- **Clinical actions:** Scan event sequence; distinguish completed, pending, restricted, and evidence-heavy steps; open the next step.
- **Sea source:** Ordered event list with committed state and redacted access rows.
- **User questions:** What happened? What remains? What am I allowed to see? Which step needs my action?
- **Pain points:** Generic event names and redacted rows may not explain clinical next action.
- **Needs:** Clinical step labels, completion cues, restricted-step explanation, and escalation route.
- **Opportunities:** Translate committed/uncommitted into step-ready language while preserving Sea state naming internally.
- **Success metrics:** Step selection time; restricted-step support requests; missed step rate.

### Stage 2: Create a clinical step

- **Clinical actions:** Add a missing step such as intake, consent, imaging, provider review, intervention, discharge instruction, or follow-up checkpoint.
- **Sea source:** Event create at end or after selection.
- **User questions:** Where will this step appear? Is it patient-specific or template-derived?
- **Pain points:** Default event naming is not clinically meaningful.
- **Needs:** Immediate clinical title/description prompt and patient/procedure context confirmation.
- **Opportunities:** Offer common step labels based on procedure template without adding a generic rules engine.
- **Success metrics:** Rename-after-create rate; misplaced-step reorder rate.

### Stage 3: Reorder steps safely

- **Clinical actions:** Move steps to reflect actual or intended clinical order.
- **Sea source:** Event chain move commands.
- **User questions:** Did I preserve the correct order? Does moving a completed step change meaning?
- **Pain points:** Chain integrity feedback and clinical consequence copy are not explicit.
- **Needs:** Refresh after move, warning for completed/restricted steps, clear failure recovery.
- **Opportunities:** Show chain integrity status when reorder fails or stale data is detected.
- **Success metrics:** Reorder success; reorder reversals; stale order incidents.

### Stage 4: Edit identity and access

- **Clinical actions:** Rename step, clarify description, restrict visibility/writability for sensitive content.
- **Sea source:** Event edit and event roles.
- **User questions:** Who can read this step? Who can add evidence? Does the title reveal sensitive content?
- **Pain points:** Role names may not explain affected clinical users.
- **Needs:** Role impact preview, privacy-safe labels, authorization error handling.
- **Opportunities:** Provide clinic role presets such as provider-only, care-team, reception-visible, or billing-review when supported by role configuration.
- **Success metrics:** Access correction rate; unauthorized attempt count; sensitive-title corrections.

### Stage 5: Commit or reopen a clinical step

- **Clinical actions:** Mark step complete/accepted; reopen when correction is needed; review automatic audit note.
- **Sea source:** Event commit/uncommit plus audit text item.
- **User questions:** Is this final? Does reopening leave a trace? Is this a legal signature?
- **Pain points:** Commit audit appears as text attachment; clinical meaning can be overstated.
- **Needs:** Clear copy that commit means workflow completion, not regulated attestation unless explicitly designed.
- **Opportunities:** Dedicated state history display if clinical governance needs stronger audit traceability.
- **Success metrics:** Commit completion; uncommit frequency; audit note discoverability.

### Stage 6: Delete erroneous step

- **Clinical actions:** Remove a mistaken event only when clinically safe and authorized.
- **Sea source:** Event delete command.
- **User questions:** Which notes, files, reminders, and audit records will be affected?
- **Pain points:** Deleting a step can remove associated clinical evidence context.
- **Needs:** Consequence summary, strict confirmation, authorization, and recoverability guidance.
- **Opportunities:** Prefer cancelled/voided semantics for clinically meaningful steps if deletion is unsafe.
- **Success metrics:** Delete abandonment; accidental deletion reports; evidence orphan incidents.

## 3. Documentation Architecture Audit

Artifact: `clinic-sea-event-lifecycle-journey.md`  
Type: Documentation journey  
Responsibility: Translate Sea event lifecycle into ordered clinical procedure step usage.

- SRP: Pass — covers only clinical step lifecycle.
- Single state model: Pass — event state remains Sea-owned and procedure status remains separate.
- Low coupling: Pass — references role/files/reminder effects without owning them.
- Abstraction: Pass — avoids infrastructure specifics.
- Command-query: Pass — review is read-only; create/reorder/edit/commit/delete are commands.
- Failure safety: Pass — redaction, stale ordering, audit, and deletion impacts are explicit.
- Open/Closed: Pass — clinic labels extend Sea events without redefining Sea.
- Core protection: N/A — documentation only.
- KISS/YAGNI: Pass — avoids proposing a new event engine.
- Single source of truth: Pass — Sea event chain owns step order and committed state.
