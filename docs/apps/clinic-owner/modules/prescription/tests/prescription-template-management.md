# Prescription template management suggested tests

## Scope

These test scenarios cover prescription template management and template loading into prescription drafts. They preserve these ownership boundaries:

- `PrescriptionTemplate` is the source of truth for reusable tenant template state.
- `PrescriptionRecord` is the source of truth for active draft/submitted prescription state.
- Loading copies item values into a draft and does not create an active link to mutable template state.
- Query flows do not mutate usage state, analytics, or last-used timestamps.

## Management command flows

1. **Create template succeeds for tenant owner/admin**
   - Given an authenticated tenant owner/admin for tenant A
   - When creating a template with a valid tenant id, name, and optional description
   - Then a tenant A template is persisted with `Active` lifecycle status and creation timestamp.

2. **Update template succeeds for active template**
   - Given an active tenant A template
   - When a tenant owner/admin updates name/description
   - Then only those editable fields and the update timestamp change.

3. **Archive template succeeds and prevents later mutation**
   - Given an active tenant A template
   - When a tenant owner/admin archives it
   - Then lifecycle status becomes `Archived`
   - And later update/add-item attempts are rejected.

4. **Add item succeeds with deterministic sort order**
   - Given an active tenant A template with existing items
   - When a tenant owner/admin adds a valid unique item
   - Then the item is persisted with tenant/template ownership, item fields, metadata, created timestamp, and next sort order.

5. **Remove item succeeds for item in same template**
   - Given an active tenant A template with an item
   - When a tenant owner/admin removes the item
   - Then that item is removed and the template update timestamp changes.

6. **List templates is read-only**
   - Given active and archived tenant A templates
   - When listing without archived templates
   - Then only active templates are returned
   - And no usage state, analytics, or last-used timestamps are mutated.

7. **Detail template is read-only**
   - Given a tenant A template with ordered items
   - When loading detail
   - Then item fields are returned in sort order
   - And no usage state, analytics, or last-used timestamps are mutated.

## Authorization and isolation

1. **Tenant isolation rejects cross-tenant management without metadata leakage**
   - Given a tenant B template id
   - When a tenant A caller attempts update/archive/add/remove
   - Then the command is rejected with the generic access-denied/not-found response
   - And no tenant B template metadata is returned.

2. **Unauthorized management is rejected**
   - Given a tenant user without owner/admin capability
   - When creating, updating, archiving, adding, or removing template items
   - Then the operation is rejected before mutation.

3. **Unauthorized load is rejected**
   - Given a caller without prescription authoring capability for the request tenant
   - When loading a template into a draft
   - Then the operation is rejected before template/prescription metadata is exposed.

## Template load flows

1. **Archived-template rejection**
   - Given an archived tenant A template
   - When loading it into a tenant A draft
   - Then loading fails and no draft items are copied.

2. **Non-draft rejection**
   - Given a tenant A prescription that is submitted or otherwise not draft/needs-correction
   - When loading an active template
   - Then loading fails and no draft items are copied.

3. **Duplicate conflict is all-or-nothing**
   - Given a draft that already has an item matching one template item by category/reference-code or category/display-name fallback
   - When loading a template containing multiple items
   - Then item-level duplicate failure details are returned
   - And none of the template items are copied.

4. **Unavailable reference code is all-or-nothing**
   - Given a template item with a reference code missing from the active medical reference source for the selected provider/category
   - When loading the template
   - Then item-level unavailable-code failure details are returned
   - And none of the template items are copied.

5. **Unsupported category/provider capability is all-or-nothing**
   - Given a selected provider that does not support one template item category
   - When loading the template
   - Then item-level unsupported-category failure details are returned
   - And none of the template items are copied.

6. **Cancellation preserves all-or-nothing behavior**
   - Given cancellation is requested before or during reference-code validation
   - When loading a template
   - Then cancellation is observed and no partial template copy is saved.

7. **Successful all-or-nothing load copies values only**
   - Given all template items are valid, supported, available in the active medical reference source, and non-duplicating
   - When loading the template into a draft/needs-correction prescription
   - Then every template item is copied to new `PrescriptionItem` rows with new ids and appended sort order
   - And no active relationship to `PrescriptionTemplateItem` remains.
