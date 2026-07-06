# Sea Event Items Journey

## 1. Context

- App: Platform-wide Sea consumers
- Module: Sea
- Capability: Event item capture and timeline review
- Related apps: Clinic, Gym, tenant-specific Sea hosts
- Related modules: Files, Communications, Users
- Product / service: Vitarish Sea module
- Journey type: Current state with gaps
- Scenario: User enriches an event with text, uploaded files/media, reminders, and plugin timeline items.
- User goal: Capture all evidence and follow-up actions for an event in one chronological place.
- Business goal: Preserve event-level context, proof, and reminders without mixing them with project/procedure identity.
- Scope start: User opens an event timeline or event item input.
- Scope end: Text note, uploaded attachment, reminder, or plugin timeline item is created, displayed, deleted, or reviewed.
- Primary persona: Event contributor
- Evidence sources: Static code review of attachment, event-item, and reminder plugin components.
- Confidence level: Medium

## 2. Journey Stages

### Stage 1: Open event item timeline

- **User actions:** Open event; scan chronological timeline items; notice placeholders while plugin data loads; review attachments/reminders.
- **Touchpoints / channels:** `EventItemsTimeline`, `AttachmentPlugin`, `ReminderPlugin`, plugin timeline item model.
- **User questions:** What evidence and follow-ups already exist for this event?
- **Emotions:** Informed when everything appears in one stream.
- **Pain points:** Timeline combines multiple item sources; users may not immediately distinguish user-authored note, uploaded file, commit audit note, or reminder.
- **Needs:** Type-specific visual cues and consistent chronology.
- **Opportunities:** Add timeline filters by item type: text, image/video, other file, reminder, audit.
- **Success metrics:** Time to locate an event item; timeline item open rate.

### Stage 2: Add text through advanced input

- **User actions:** Type narrative, observation, measurement, or audit text into advanced input; submit; timeline refreshes.
- **Touchpoints / channels:** `AttachmentCreator`, `AdvancedInput`, `AddTextAttachmentMetadataAsync`.
- **User questions:** Is this note attached to the correct event? Will my text remain readable later?
- **Emotions:** Confident if note appears immediately.
- **Pain points:** Text notes are stored as text/plain attachment metadata, which means authored notes and automatic commit/uncommit audit notes share the attachment timeline category.
- **Needs:** Clear note type and author/date visibility.
- **Opportunities:** Label text notes versus system audit notes.
- **Success metrics:** Text note creation success rate; correction/deletion rate.

### Stage 3: Upload file or media through advanced input

- **User actions:** Use advanced input file upload; upload document, image, video, or other media accepted by Files; Sea stores file id/content metadata on the event; timeline refreshes.
- **Touchpoints / channels:** `AttachmentCreator`, `AdvancedInput`, Files upload callback, `AttachmentsDisplay`, `AttachmentDetailsDialog`.
- **User questions:** Did the file upload? Can viewers understand what it contains? Is media preview available?
- **Emotions:** Assured when thumbnail/preview appears.
- **Pain points:** Display logic previews image/video thumbnails and text content, while other content types rely on details/adornment behavior; accepted file constraints are not visible in Sea.
- **Needs:** File type/size guidance, upload progress/failure feedback, accessible media preview.
- **Opportunities:** Add file metadata labels and preview affordances for non-image/video files.
- **Success metrics:** Upload success rate; preview/open rate by content type; failed upload recovery rate.

### Stage 4: Review attachment display and details

- **User actions:** Review item author; inspect icon/content chip; open details; delete when allowed.
- **Touchpoints / channels:** `AttachmentsDisplay`, `AttachmentDetailsDialog`, `TextChip`, media thumbnail chip.
- **User questions:** Who added this? What kind of content is it? Can I view or remove it?
- **Emotions:** Trusting when author and type are clear.
- **Pain points:** Attachment display depends heavily on content type; unknown types may be less discoverable.
- **Needs:** Consistent file metadata, author, created time, and deletion feedback.
- **Opportunities:** Normalize attachment cards across content types.
- **Success metrics:** Attachment detail open rate; deletion success/failure rate.

### Stage 5: Add reminder as an event item action

- **User actions:** Click add reminder action; create reminder through Communications UI; timeline refreshes.
- **Touchpoints / channels:** `EventItemsInput`, `EventReminderAction`, `ReminderPlugin`, Communications `ReminderDisplay`.
- **User questions:** When should we follow up on this event? Will the reminder stay linked to this event?
- **Emotions:** Relieved when follow-up is scheduled.
- **Pain points:** Reminder lifecycle is owned by Communications; Sea only links by event source id and renders reminder timeline items.
- **Needs:** Clear source linkage, due date/time clarity, deletion callback.
- **Opportunities:** Show reminder count and next due reminder at event and procedure levels.
- **Success metrics:** Reminder creation rate; overdue reminder rate; reminder deletion rate.

### Stage 6: Understand template and commit interactions

- **User actions:** Create procedure from template; observe copied attachments; commit/uncommit events; observe automatic text audit items.
- **Touchpoints / channels:** `ProcedureCreateDialog`, `AttachmentPlugin`, `EventDetails`.
- **User questions:** Which event items are copied from templates, and which are newly generated by state changes?
- **Emotions:** Cautious when item provenance is unclear.
- **Pain points:** Template copy evidence covers attachments but not reminder copy; commit audit notes appear as text attachments.
- **Needs:** Provenance labels: template-copied, user-created, system-created.
- **Opportunities:** Add provenance metadata or visual labels for event items.
- **Success metrics:** User confusion/support tickets about copied items; audit note recognition.

## 3. Advanced Input Documentation Detail

The event item input is an advanced input, not a plain text box. Journey documentation must treat it as a mixed-content capture surface:

| Input mode | User intent | Stored/rendered as | Journey implication |
|---|---|---|---|
| Text submit | Narrative note, observation, or system audit note | `text/plain` attachment metadata | Text and audit notes share timeline space; labels matter. |
| File upload | Document or generic file evidence | File id/content metadata with content type and size | File constraints and details view are part of the user journey. |
| Image upload | Visual proof | Image thumbnail/chip in attachment display | Preview quality and privacy matter. |
| Video upload | Motion/media proof | Video-like thumbnail/chip based on content type | Playback/detail affordance should be clear. |
| Reminder action | Future follow-up | Communications reminder timeline item | Reminder is an event item conceptually but owned by Communications. |
| Plugin item | Tenant/module extension | `IEventItemPlugin` timeline render fragment | Timeline must tolerate mixed item sources. |

## 4. Project Management Mindset Trace

| Hierarchy level | Event item implication |
|---|---|
| Project | Provides context and tenant plugin identity for event item rendering. |
| Procedure | Provides workflow context and template copy behavior. |
| Event | Owns item association through event id/source id. |
| Event items | Include text attachments, file/media attachments, reminders, audit notes, and plugin items. |

## 5. Documentation Architecture Audit

Artifact: `sea-event-items-journey.md`  
Type: Documentation journey  
Responsibility: Document mixed event-item capture and timeline review, including advanced input, attachments, reminders, and plugin items.

- SRP: Pass — covers only event item capture/review.
- Single state model: Pass — event id/source id is the item association source.
- Low coupling: Pass — Files and Communications are referenced as boundaries, not redefined.
- Abstraction: Pass — documents event-item user value independent of specific storage implementation.
- Command-query: Pass — timeline review is query; text/file/reminder creation and deletion are commands.
- Failure safety: Pass — upload constraints, mixed ownership, and provenance gaps are explicit.
- Open/Closed: Pass — plugin event items are a documented extension point.
- Core protection: N/A — documentation only.
- KISS/YAGNI: Pass — describes current behavior without adding implementation ceremony.
- Single source of truth: Pass — advanced event item journey is scoped to this file.
