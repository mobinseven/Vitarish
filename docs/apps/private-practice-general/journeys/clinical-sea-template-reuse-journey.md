# Clinic Sea Clinical Template Reuse Journey

## 1. Context

- App: Clinic Owner
- Module: Sea usage in patient workflows
- Capability: Reusable clinical procedure templates
- Related modules: Sea, Files, Communications, Users, Medical
- Journey type: Future state translation of current Sea template reuse with clinical gaps
- Scenario: Clinic owner maintains standard procedure templates and applies them to patient-specific procedures.
- User goal: Start from a proven clinical protocol while clearly adapting it to the current patient.
- Business goal: Reduce setup time and variation without mistaking template content for patient evidence.
- Scope start: User marks a project as template or creates a patient procedure from template.
- Scope end: Template is saved, selected, copied, reviewed, adapted, or rejected.
- Primary persona: Lead provider / clinical coordinator
- Evidence sources: Platform Sea Template Reuse journey; Clinic Owner patient procedure journey.
- Confidence level: Medium

## 2. Clinical Journey Stages

### Stage 1: Maintain clinical protocol templates

- **Clinical actions:** Create or edit template patient/project containing reusable procedure structures.
- **Sea source:** Template project flag and procedure content.
- **User questions:** Is this active patient work or a reusable protocol? Who approves changes?
- **Pain points:** Template flag meaning is not self-explanatory.
- **Needs:** Clear template labeling, ownership, last-reviewed date, and separation from active patients.
- **Opportunities:** Add protocol governance metadata only if clinics need review workflows now.
- **Success metrics:** Template creation; accidental template toggles; protocol review completion.

### Stage 2: Browse templates during procedure creation

- **Clinical actions:** Compare standard protocols and choose one appropriate for the patient.
- **Sea source:** ProcedureTemplatesBrowser.
- **User questions:** Which steps will be created? Are roles, attachments, and reminders copied? Is this clinically current?
- **Pain points:** Current browser lacks full pre-copy preview.
- **Needs:** Event list preview, copied/not-copied categories, attachment provenance warning.
- **Opportunities:** Preview estimated duration, provider role expectations, and required evidence when available.
- **Success metrics:** Template selection rate; post-copy correction rate.

### Stage 3: Copy template into patient procedure

- **Clinical actions:** Confirm name, create under patient, review copied steps/roles/attachments.
- **Sea source:** Template procedure copy command.
- **User questions:** Did the copy finish? What came from the template? What must be personalized?
- **Pain points:** Copied attachments can appear like patient-specific evidence.
- **Needs:** Post-copy summary and review checklist.
- **Opportunities:** Mark copied notes/files as template-origin until confirmed or removed.
- **Success metrics:** Copy failures; missing-item reports; unreviewed copied content.

### Stage 4: Adapt copied procedure to patient

- **Clinical actions:** Rename, reorder, delete irrelevant steps, add patient-specific notes/files/reminders, adjust access.
- **Sea source:** Event/procedure edit, event items, roles.
- **User questions:** What must change for this patient? Which template items are instructions rather than facts?
- **Pain points:** Without provenance, stale instructions may be misread as current clinical findings.
- **Needs:** Review status, provenance labels, safe deletion/correction guidance.
- **Opportunities:** Add template-origin filter in event timeline.
- **Success metrics:** Adaptation completion; stale template incident reports; copied item deletion/edit rate.

## 3. Documentation Architecture Audit

Artifact: `clinic-sea-template-reuse-journey.md`  
Type: Documentation journey  
Responsibility: Translate Sea template reuse into clinical protocol setup and patient-specific adaptation.

- SRP: Pass — covers only template/protocol reuse.
- Single state model: Pass — template content is separate from patient-specific procedure evidence until copied and reviewed.
- Low coupling: Pass — related modules remain source owners for attachments/reminders/users.
- Abstraction: Pass — no implementation dependency is introduced.
- Command-query: Pass — browsing/preview are reads; copy/adaptation are commands.
- Failure safety: Pass — stale evidence, provenance, and copy failure risks are explicit.
- Open/Closed: Pass — proposes clinic-specific labels/review state without changing Sea template mechanics.
- Core protection: N/A — documentation only.
- KISS/YAGNI: Pass — protocol governance is deferred unless needed now.
- Single source of truth: Pass — Sea template procedure remains reusable structure source.
