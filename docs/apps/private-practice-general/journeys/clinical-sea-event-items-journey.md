# Clinic Sea Clinical Event Items Journey

## 1. Context

- App: Clinic Owner
- Module: Sea usage in patient workflows
- Capability: Clinical notes, evidence, reminders, and timeline items under a procedure event
- Related modules: Sea, Files, Communications, Medical, Users
- Journey type: Future state translation of current Sea event items with clinical gaps
- Scenario: Clinical staff add and review event-level notes, documents, media, reminders, automatic audit text, and plugin-rendered items.
- User goal: Capture supporting clinical evidence under the correct patient, procedure, and step.
- Business goal: Maintain trustworthy event-level context and follow-up history without creating duplicate records.
- Scope start: User opens a clinical event timeline or event item input.
- Scope end: Note, file/media, reminder, audit item, or plugin item is created, reviewed, deleted, or escalated.
- Primary persona: Provider / assistant / coordinator
- Evidence sources: Platform Sea Event Items journey; Clinic Owner patient procedure event journey.
- Confidence level: Medium

## 2. Clinical Journey Stages

### Stage 1: Review clinical timeline items

- **Clinical actions:** Scan notes, documents, photos/videos, reminders, audit notes, and plugin items in chronological order.
- **Sea source:** EventItemsTimeline.
- **User questions:** What evidence supports this step? Which item is a user note versus system audit? Is anything copied from a template?
- **Pain points:** Multiple item types share one timeline; copied items and audit notes may be unclear.
- **Needs:** Type labels, author/time, provenance, clinical relevance cues, accessible previews.
- **Opportunities:** Add filters for notes, files, media, reminders, audit, and medical plugin items.
- **Success metrics:** Time to locate item; detail-open rate; misinterpreted copied item incidents.

### Stage 2: Add clinical note

- **Clinical actions:** Enter observation, measurement narrative, handoff, instruction, or context for this event.
- **Sea source:** Text attachment metadata through advanced input.
- **User questions:** Is this note attached to the correct event? Can others see who wrote it and when?
- **Pain points:** Text notes and automatic commit audit notes may both appear as text attachment-like items.
- **Needs:** Distinct note/audit labels, required non-empty content, save failure preserving draft text.
- **Opportunities:** Provide clinical note templates only for present high-value workflows, not generic templating.
- **Success metrics:** Note creation success; correction/deletion rate; draft-loss reports.

### Stage 3: Upload clinical document or media

- **Clinical actions:** Upload consent, referral, scan, lab file, wound/photo/media, or post-procedure evidence.
- **Sea source:** Files upload linked to event attachment metadata.
- **User questions:** Did upload finish? Is media preview available? Are file type/size constraints clear? Is this sensitive?
- **Pain points:** Non-image/video files can be less discoverable; Sea does not own Files constraints.
- **Needs:** Upload progress/failure feedback, file metadata labels, privacy warnings where needed.
- **Opportunities:** Add clinical media labels and source/provenance fields without duplicating Files metadata.
- **Success metrics:** Upload success; preview/open rate; failed upload recovery.

### Stage 4: Add event-linked reminder

- **Clinical actions:** Schedule follow-up for this step, such as call patient, review result, check healing, or request missing document.
- **Sea source:** Communications reminder linked to event source id.
- **User questions:** Is this an event follow-up or an appointment? Who owns completion?
- **Pain points:** Reminder state belongs to Communications; Sea renders linkage but should not imply appointment booking.
- **Needs:** Clear source label, due date/time, assignee/owner visibility when supported, deletion/cancellation feedback.
- **Opportunities:** Surface next due event reminder at patient/procedure level.
- **Success metrics:** Reminder creation; overdue reminder rate; confusion with appointment booking.

### Stage 5: Review commit/uncommit audit text

- **Clinical actions:** Verify when a step was marked complete or reopened and by whom.
- **Sea source:** Automatic text audit items from commit/uncommit operations.
- **User questions:** Is this a user-authored clinical note or system state-change record?
- **Pain points:** Audit text in the same timeline can be mistaken for clinical narrative.
- **Needs:** System audit styling and state-change labels.
- **Opportunities:** Dedicated event state history if governance requires stronger separation.
- **Success metrics:** Audit item discoverability; erroneous edits/deletions; compliance review findings.

### Stage 6: Delete or correct event items

- **Clinical actions:** Remove mistaken attachment/note/reminder when allowed; add correction note when deletion is inappropriate.
- **Sea source:** Event item deletion callbacks and owning module commands.
- **User questions:** Is deletion allowed? Will evidence disappear from clinical history?
- **Pain points:** Item deletion may have different rules per source module.
- **Needs:** Source-specific confirmation, authorization feedback, audit/correction guidance.
- **Opportunities:** Prefer correction notes over destructive deletion for clinically relevant content when policy requires retention.
- **Success metrics:** Deletion success/failure; correction note usage; unauthorized delete attempts.

## 3. Documentation Architecture Audit

Artifact: `clinic-sea-event-items-journey.md`  
Type: Documentation journey  
Responsibility: Translate Sea event item capture and timeline review into clinical notes, evidence, reminders, and audit usage.

- SRP: Pass — covers only event-level supporting items.
- Single state model: Pass — item ownership stays with Sea/Files/Communications as appropriate.
- Low coupling: Pass — module ownership is explicit.
- Abstraction: Pass — no transport or storage details are required.
- Command-query: Pass — timeline review is read-only; add/delete operations are commands.
- Failure safety: Pass — draft loss, upload failure, reminder confusion, audit ambiguity, and deletion risks are documented.
- Open/Closed: Pass — clinical labels/plugins extend timeline interpretation.
- Core protection: N/A — documentation only.
- KISS/YAGNI: Pass — avoids broad note-template framework unless needed.
- Single source of truth: Pass — separates note, file, reminder, and audit ownership.
