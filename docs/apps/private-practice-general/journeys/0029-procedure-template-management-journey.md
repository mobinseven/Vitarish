# Procedure Template Management Journey

Status: Future state  
Owner: Private Practice General app  
Derived from: [Procedure Template Management Decision Tree](../decision-trees/0002-procedure-template-management-decision-tree.md)  
Background evidence: [Clinical SEA Template Reuse Journey](clinical-sea-template-reuse-journey.md)  
Last reviewed: 2026-07-22

## Persona and outcome

A private-practice owner maintains reusable procedure templates before they are applied to patient care. Success means the owner can enter from Settings, organize routines and ordered steps, understand partial duplication, and recover from refetches or failures without confusing templates with patient procedures. Existing SEA behavior remains authoritative and is not redefined here.

## Journey map

| Stage | Goal and touchpoints | Intended experience | Success criteria | Failure and recovery |
| --- | --- | --- | --- | --- |
| Enter through Settings | Discover the localized Procedure Templates card and open the protected workspace route. | Clear clinic-configuration context without adding sidebar clutter. | Card emits the template intent; owner app resolves `/settings/procedure-templates`; workspace and Journey gates remain active. | Unauthorized users receive the existing protected-route outcome. |
| Select a template | Review loading, error, empty, and populated project states in the first pane. | Quickly orient to the current reusable template. | A still-valid selection survives refetch; otherwise a deterministic valid project is selected. | Retry remains in context; an empty collection offers creation. |
| Manage routines | Create, edit, select, duplicate, or confirm deletion in the second pane. | Changes feel deliberate and their scope is obvious. | Successful mutations refetch authoritative SEA data and preserve or recover selection. | Cancel is non-mutating; errors retain context; delete requires confirmation. |
| Edit ordered steps | Select, create, edit, reorder, or confirm deletion in the step pane/editor. | Order and boundary actions are predictable by mouse or keyboard. | Same-step selection survives reorder/refetch; create and delete choose a deterministic valid step. | Failed changes report an error without silently changing authoritative order. |
| Handle partial duplication | Duplicate a routine and its steps sequentially. | The owner understands whether the routine exists and which copy work failed. | Full success selects the duplicate; partial success explicitly says some steps failed and refetches truth. | Source remains selected if routine creation fails; retry does not claim an all-or-nothing rollback. |
| Recover | Continue after query/mutation failures, cancellation, deletion, or external refetch changes. | Stable context and no false success. | Valid project/routine/step IDs persist; invalid IDs recover deterministically to valid neighbors or empty states. | Localized retry/empty states remain accessible in English and Persian RTL. |

## Experience rules

- The three-pane panel uses the immersive clinic-workspace layout even though entry is under Settings.
- All icons follow the app Font Awesome convention; controls retain localized labels/tooltips and keyboard access.
- Destructive actions require explicit confirmation, and cancellation performs no mutation.
- Loading, empty, error, and partial-success states must be conveyed with text, not color alone.
- Clinic Web and SEA remain the presentation and persistence owners; the app route adds no duplicate components or API wrappers.
