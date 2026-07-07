# Clinic Sea Clinical Access and Roles Journey

## 1. Context

- App: Clinic Owner
- Module: Sea usage in patient workflows
- Capability: Clinical role and event access management
- Related modules: Sea, Users, Medical
- Journey type: Future state translation of current Sea access and roles with clinical gaps
- Scenario: Clinic administrator defines clinical access roles, assigns read/write capabilities, and restricts sensitive procedure events.
- User goal: Let the right care-team members see or update the right clinical steps.
- Business goal: Protect patient information while enabling coordinated care.
- Scope start: Administrator opens role/user/event-role UI or participant opens a restricted procedure.
- Scope end: Role vocabulary, user assignment, event access, redaction, or escalation path is clear.
- Primary persona: Clinic owner / access administrator
- Evidence sources: Platform Sea Access and Roles journey; Clinic Owner patient procedure journey.
- Confidence level: Medium

## 2. Clinical Journey Stages

### Stage 1: Define clinical role vocabulary

- **Clinical actions:** Create roles such as provider, assistant, receptionist, billing reviewer, specialist, supervisor, or restricted reviewer.
- **Sea source:** Roles grid and role edit dialog.
- **User questions:** Does this role describe clinical responsibility or just application access?
- **Pain points:** Role names alone may not communicate privacy consequences.
- **Needs:** Naming conventions, descriptions, affected-user counts, deletion consequences.
- **Opportunities:** Add clinic role descriptions and examples without replacing Users/authorization policy.
- **Success metrics:** Role correction rate; unused roles; privacy-related support tickets.

### Stage 2: Assign user read/write clinical capabilities

- **Clinical actions:** Grant read/write access to providers, assistants, receptionists, and supervisors.
- **Sea source:** User role toggles with immediate saves.
- **User questions:** Can this person read sensitive steps? Can they add clinical evidence?
- **Pain points:** Immediate per-toggle saves can partially fail and leave unclear state.
- **Needs:** Per-toggle feedback, pending/saved/error state, batch option if high-volume changes are common.
- **Opportunities:** Preview a user's effective clinical access profile.
- **Success metrics:** Assignment failure rate; time to configure care-team access.

### Stage 3: Restrict sensitive event access

- **Clinical actions:** Set reader/writer roles for sensitive steps such as private provider notes, financial review, specialist consult, or restricted media.
- **Sea source:** Event reader/writer role assignment.
- **User questions:** Which users are affected by this role? Can writers also read? Who can correct an error?
- **Pain points:** Role impact is not proven before save.
- **Needs:** Affected-user preview, clear read/write labels, safe cancellation, authorization errors.
- **Opportunities:** Clinic-specific access presets mapped to Sea roles.
- **Success metrics:** Event access corrections; restricted-event escalation count.

### Stage 4: Experience restricted clinical rows

- **Clinical actions:** Participant sees procedure sequence but restricted event details are redacted.
- **Sea source:** Redacted event rendering.
- **User questions:** Why is this hidden? Who should I ask? Can I continue with other steps?
- **Pain points:** Redaction protects privacy but can block workflow without guidance.
- **Needs:** Privacy-safe explanation, access request/contact path, no leakage in titles or counts.
- **Opportunities:** Add clinic owner/provider contact metadata for access escalation.
- **Success metrics:** Redacted row clicks; access request resolution time; privacy incident count.

## 3. Boundary Notes

- Sea roles control Sea event read/write behavior.
- Users/identity owns user accounts and organization membership.
- Medical provider credentials, employment status, and patient consent are separate concerns unless explicitly integrated.

## 4. Documentation Architecture Audit

Artifact: `clinic-sea-access-and-roles-journey.md`  
Type: Documentation journey  
Responsibility: Translate Sea roles and event access into clinical care-team access usage.

- SRP: Pass — covers only clinical role/access journeys.
- Single state model: Pass — Sea event access is distinct from identity, credentialing, and consent.
- Low coupling: Pass — Users and Medical boundaries remain separate.
- Abstraction: Pass — describes access semantics without implementation coupling.
- Command-query: Pass — previews/visibility are reads; role assignment is a command.
- Failure safety: Pass — partial save, redaction, privacy, and escalation risks are explicit.
- Open/Closed: Pass — clinic presets may extend role usage without forking Sea.
- Core protection: N/A — documentation only.
- KISS/YAGNI: Pass — avoids broad policy framework beyond current role needs.
- Single source of truth: Pass — Sea remains event-access source of truth.
