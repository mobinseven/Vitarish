# Sea Module — Journey Maps

## Overview

This folder is the source of truth for Sea module journey maps. The module is documented with a project management mindset:

```txt
Project → Procedure → Event → Event items
```

Event items include advanced-input text notes, file/media attachments, reminders, commit/uncommit audit text, and plugin-rendered timeline entries.

## Journey Map Files

- [Sea Module Interface Journey Coverage](./sea-module-interface-journey-coverage.md) — exhaustive coverage index mapping interface components to journey docs.
- [Sea Project Lifecycle](./sea-project-lifecycle-journey.md) — project discovery, create/edit/delete, avatar, and tenant project plugins.
- [Sea Procedure Lifecycle](./sea-procedure-lifecycle-journey.md) — procedure list, manual create, template create, edit, and delete.
- [Sea Event Lifecycle](./sea-event-lifecycle-journey.md) — ordered event review, create, reorder, edit, access, commit/uncommit, and delete.
- [Sea Event Items](./sea-event-items-journey.md) — advanced input, text notes, file/media attachments, reminders, commit audit notes, and plugin timeline items.
- [Sea Access and Roles](./sea-access-and-roles-journey.md) — role vocabulary, user read/write role assignment, event role assignment, and redacted participant experience.
- [Sea Template Reuse](./sea-template-reuse-journey.md) — template projects, template procedure browsing, copied content expectations, and adaptation after copy.
- [Sea Observability and Triage](./sea-observability-and-triage-journey.md) — stats, progress, unread/new item cues, and drill-down to event items.
- [Sea Plugin Extension](./sea-plugin-extension-journey.md) — tenant project plugins and event item creation/timeline plugins.
- [Legacy Sea Projects & Procedures](./sea-project-procedure-journey.md) — earlier combined journey retained for continuity; the files above are the current detailed coverage set.

## Scope

- **Module:** Sea
- **Scope:** Platform-wide (used by Clinic, Gym, and other tenant-specific apps)
- **Primary users:** Project owners, procedure coordinators, event contributors, access administrators, project participants, tenant-specific operators
- **Primary hierarchy:** Project → Procedure → Event → Event items

## Coverage Checklist

| Interface area | Covered by |
|---|---|
| Project list/details/edit/avatar/delete | Sea Project Lifecycle |
| Procedure list/create/edit/delete | Sea Procedure Lifecycle |
| Template project and procedure copy | Sea Template Reuse |
| Event list/create/order/edit/delete | Sea Event Lifecycle |
| Commit/uncommit | Sea Event Lifecycle and Sea Event Items |
| Advanced input text/file/media attachments | Sea Event Items |
| Reminders linked to events | Sea Event Items |
| Event roles and user roles | Sea Access and Roles |
| Stats/progress/unread indicators | Sea Observability and Triage |
| Tenant project UI plugins | Sea Plugin Extension |
| Event item plugins | Sea Event Items and Sea Plugin Extension |
