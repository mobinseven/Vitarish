# Clinic Owner Sea Module — Clinical Journey Maps

## Overview

This folder translates the platform Sea module journey maps into the Clinic Owner app's clinical usage direction. In Clinic, Sea is used as a patient-centered clinical workflow container:

```txt
Sea Project → Clinic patient profile
Sea Procedure → Clinical procedure / treatment plan under that patient
Sea Event → Ordered clinical step, visit milestone, procedure stage, or care activity
Sea Event items → Clinical notes, documents, media, reminders, audit notes, and plugin-rendered clinical timeline entries
```

The platform Sea source of truth remains `docs/platform/modules/sea/journeys/`. These clinic journeys do not redefine Sea behavior; they explain how the Clinic Owner app should interpret and surface Sea concepts for clinical operators.

## Journey Map Files

- [Clinical Sea Journey Coverage](./clinic-sea-journey-coverage.md) — exhaustive translation index from platform Sea journeys to clinic usage.
- [Patient as Sea Project Journey](./clinic-sea-patient-project-journey.md) — patient discovery, identity, profile details, avatar/photo, and deletion/archival clinical consequences.
- [Clinical Procedure Lifecycle Journey](./clinic-sea-procedure-lifecycle-journey.md) — procedure creation, template-based treatment setup, provider/status needs, notes, and cancellation semantics.
- [Clinical Procedure Event Journey](./clinic-sea-event-lifecycle-journey.md) — ordered clinical steps, access, commit/uncommit, audit history, and safe deletion.
- [Clinical Event Items Journey](./clinic-sea-event-items-journey.md) — clinical notes, attachments, media, reminders, provenance, and timeline review.
- [Clinical Access and Roles Journey](./clinic-sea-access-and-roles-journey.md) — clinical team roles, sensitive event redaction, provider responsibility, and participant access.
- [Clinical Template Reuse Journey](./clinic-sea-template-reuse-journey.md) — reusable treatment protocols and patient-specific adaptation after copying.
- [Clinical Observability and Triage Journey](./clinic-sea-observability-and-triage-journey.md) — patient/procedure attention signals, unread work, progress, and escalation.
- [Clinical Plugin Extension Journey](./clinic-sea-plugin-extension-journey.md) — clinic-specific patient/project, event-item, and timeline plugin usage without creating a second state model.

## Scope

- **App:** Clinic Owner
- **Module:** Sea usage within patient workflows
- **Capability:** Patient-centered clinical procedure and event tracking
- **Related apps:** Clinic Owner
- **Related modules:** Sea, Medical, Appointments, Communications, Files, Users
- **Primary users:** Clinic owner, clinical coordinator, provider, assistant, receptionist/scheduler, access administrator
- **Primary hierarchy:** Patient profile → Clinical procedure → Clinical event/step → Clinical event items

## Clinical Translation Principles

1. **Patient remains the clinical anchor.** The Clinic app treats each relevant Sea project as one patient profile; patient identity and patient-level metadata must remain the source of truth for patient context.
2. **Procedures belong under one patient.** Procedures organize treatment work for a patient and must not become standalone patient records or appointment records.
3. **Events are step-level clinical facts.** Events describe ordered stages or activities inside a procedure, not the full patient lifecycle.
4. **Event items carry supporting clinical evidence.** Notes, files, media, reminders, and audit text support a specific event and should not be confused with patient identity or procedure lifecycle state.
5. **Clinical extensions must not fork Sea state.** Clinic plugins can adapt labels, summaries, and clinical affordances, but Sea project/procedure/event/event-item state remains authoritative for the hierarchy.
6. **Appointment and medical records stay bounded.** Appointment scheduling and medical reference/prescription capabilities may link to patient/procedure context, but their state remains owned by their modules.

## Coverage Checklist

| Clinic usage area | Sea source journey | Clinic journey |
|---|---|---|
| Patient list, profile identity, avatar/photo, profile detail | Sea Project Lifecycle | Patient as Sea Project Journey |
| Procedure list, create/edit/delete, treatment status gaps | Sea Procedure Lifecycle | Clinical Procedure Lifecycle Journey |
| Template procedures and repeated treatment protocols | Sea Template Reuse | Clinical Template Reuse Journey |
| Ordered clinical steps and commit/uncommit readiness | Sea Event Lifecycle | Clinical Procedure Event Journey |
| Clinical notes, files, media, reminders, timeline items | Sea Event Items | Clinical Event Items Journey |
| Provider/team roles and sensitive event redaction | Sea Access and Roles | Clinical Access and Roles Journey |
| Patient/procedure attention indicators and unread triage | Sea Observability and Triage | Clinical Observability and Triage Journey |
| Clinic-specific project and timeline plugins | Sea Plugin Extension | Clinical Plugin Extension Journey |

## Documentation Architecture Audit

Artifact: `README.md`  
Type: Documentation index  
Responsibility: List the Clinic Owner Sea clinical journey maps and define the patient-centered Sea translation boundary.

- SRP: Pass — indexes only clinic Sea journey documentation and its boundary.
- Single state model: Pass — identifies Sea hierarchy as the source for workflow state and patient profile as the clinical anchor.
- Low coupling: Pass — references related modules without moving their state into Sea.
- Abstraction: Pass — documents usage semantics rather than implementation dependencies.
- Command-query: N/A — documentation index only.
- Failure safety: Pass — flags deletion, access, and module-boundary consequences for detailed journeys.
- Open/Closed: Pass — clinic-specific journeys extend platform journeys without editing the Sea source of truth.
- Core protection: N/A — no production core logic.
- KISS/YAGNI: Pass — adds scoped documentation only.
- Single source of truth: Pass — points platform Sea behavior to platform docs and clinic usage to this folder.
