# Patient Procedure Clinical Workspace Journey

Status: Future state  
Owner: Private Practice General app  
Derived from: [Patient Procedure Clinical Workspace Decision Tree](../decision-trees/0001-patient-procedure-workspace-decision-tree.md)  
Current-state evidence: [0022 procedure management](0022-patient-procedure-management-journey.md), [0023 procedure event](0023-patient-procedure-event-journey.md)  
Last reviewed: 2026-07-13

## Persona and outcome

The primary persona is a clinician/provider reviewing and updating one patient's procedure during care. Assistants are secondary users and receive exactly the actions allowed by backend permissions. Success means the user can orient, scan independently committed steps, review one step and its evidence, and complete a command without losing patient context or unsaved evidence.

Emotional arc: focused -> oriented -> confident while reviewing -> cautious during a clinical command -> reassured by explicit success or recoverable failure.

## Journey map

| Stage | Goal and touchpoints | Intended experience and emotion | Component annotations | Success criteria | Failure and recovery |
| --- | --- | --- | --- | --- | --- |
| Orient | Confirm patient/procedure identity and return path. | Immediate context; calm and oriented. | Breadcrumbs, semantic `h1`, completion Chip, back Button, shape-matched skeletons. | Identity and completion summary are visible before actions. | Access denied and procedure not-found preserve safe patient navigation. |
| Scan steps | Understand ordered draft/completed steps and select one. | Fast scanning without command clutter; attentive. | HeroUI Pro Timeline with current, success, and default states; text status; EmptyState. | Keyboard selection opens the existing event URL; first step is deterministic without duplicating SEA state. | Empty procedures offer a writable add action; list failure offers retry. |
| Review step | Keep selected context beside history on desktop and focused on mobile. | Confident that the selected step matches the URL. | Desktop card-level surface; mobile bottom Sheet with scrollable body and localized close name. | Back/refresh preserve the event deep link; closing mobile Sheet returns to procedure URL. | Invalid event shows inline not-found recovery inside procedure context. |
| Review activity | Read notes/files newest-first. | Trustworthy chronological evidence; deliberate. | Compact Pro Timeline, Chip, Link, Tooltip, semantic `time`. | Type, timestamp, filename, MIME type, locale-formatted size, and actions are available. | Legacy null filename uses localized generic attachment label; query failure retries in place. |
| Add evidence | Add a plain-text note or one file. | Cautious during entry, reassured by draft preservation. | TextArea, Button, Pro DropZone status UI. | Successful item appears after refresh; only successful local state clears. | Validation, network failure, and retry preserve note draft or selected file; no fake percentages. |
| Add/manage step | Create, edit, complete/reopen, reorder, or delete a clinical step. | Deliberate and safe. | Modal, Dropdown, AlertDialog, Toast; localized Earlier/Later actions. | Complete/reopen is contextual primary; other commands remain secondary and permission-bound. | Recoverable failures stay inline and retain selection/form values; destructive commands require confirmation. |

## Experience rules

- Timeline is navigation/history, not a Stepper: SEA permits independently committed, non-contiguous steps.
- Persian is the default RTL locale with complete English parity and locale-aware date, time, number, and file-size formatting.
- Visible focus, localized accessible names, status text in addition to color, and semantic time values are required.
- SEA remains the only persisted clinical state model. React presentation state never infers authorization or clinical transitions.

## Deferred scope

Actor names/avatars, analytics, notifications, new reminder/audit/plugin creation, attachment preview expansion, resumable or batch transfer, real transfer progress, Files-module migration, and large-media policy remain outside this journey.

