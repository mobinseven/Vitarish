# Journey: Prescription Template List

## 1. Context
- **Persona:** Prescribing user.
- **Trigger:** Prescribing user opens prescription creation and wants to reuse a saved set of prescription items.
- **Goals:**
  1. A prescribing user creates a reusable prescription template list.
  2. A prescribing user selects and loads one of their templates while creating a new prescription.
  3. Loaded template items become draft prescription items, while the draft prescription remains the source of truth for the active prescription.

## Current Implementation Alignment
- **Status:** `planned`.
- **Implemented now:** No dedicated template list, editor, picker, template domain entity, template command, or template query is currently identified in the owner prescription journey matrix.
- **Target artifacts:** `PrescriptionTemplate`, `PrescriptionTemplateItem`, `CreatePrescriptionTemplateCommand`, `UpdatePrescriptionTemplateCommand`, `ArchivePrescriptionTemplateCommand`, `AddPrescriptionTemplateItemCommand`, `RemovePrescriptionTemplateItemCommand`, `LoadPrescriptionTemplateIntoDraftCommand`, `GetPrescriptionTemplatesQuery`, `GetPrescriptionTemplateQuery`, `PrescriptionTemplateList`, `PrescriptionTemplateEditor`, and `PrescriptionTemplatePicker`.
- **Authoritative state:** `PrescriptionTemplate` owns reusable template state. `PrescriptionRecord` owns active draft and submitted prescription state. Loading a template copies validated item data into a draft and must not link the draft to mutable template state.
- **Known gaps:** Routes, UI surfaces, domain entities, EF mappings, handlers, authorization checks, provider-capability validation, conflict decisions, and tests are not implemented yet.

## 2. Artifact List and Responsibilities

| Artifact | Classification | Responsibility |
| --- | --- | --- |
| `PrescriptionTemplate` | Domain / core logic | Own user-scoped template identity, name, description, owner user id, and lifecycle state. |
| `PrescriptionTemplateItem` | Domain / core logic | Own reusable prescription item data copied into drafts during template load. |
| `CreatePrescriptionTemplateCommand` | Application command | Create one user-owned prescription template with validated metadata. |
| `UpdatePrescriptionTemplateCommand` | Application command | Update editable template metadata without mutating active prescriptions. |
| `ArchivePrescriptionTemplateCommand` | Application command | Move a user-owned template out of active selection while preserving history. |
| `AddPrescriptionTemplateItemCommand` | Application command | Add one validated reusable item to a user-owned template. |
| `RemovePrescriptionTemplateItemCommand` | Application command | Remove one reusable item from a user-owned template. |
| `LoadPrescriptionTemplateIntoDraftCommand` | Application command | Copy validated template items into an existing draft prescription. |
| `GetPrescriptionTemplatesQuery` | Application query | Return the current user's template list for discovery and selection. |
| `GetPrescriptionTemplateQuery` | Application query | Return one user-owned template and its reusable items for view/edit. |
| EF template mappings | Infrastructure / persistence model | Persist template entities in the Medical prescription infrastructure area. |
| Template command/query handlers | Infrastructure adapter | Execute template commands and queries under `src/Modules/Medical/Infrastructure/Handlers/Prescriptions/` with cancellation propagation. |
| `PrescriptionTemplateList` | Interface / UI | Show discoverable user-owned templates and lifecycle actions. |
| `PrescriptionTemplateEditor` | Interface / UI | Capture template metadata and reusable item edits. |
| `PrescriptionTemplatePicker` | Interface / UI | Let a prescribing user choose a template from prescription creation. |
| `OwnerPrescriptionCreatePage` integration | Presentation / integration boundary | Expose template discovery and load entry points during new prescription creation. |
| `PrescriptionDetail` integration | Presentation / integration boundary | Apply template loading to the draft authoring flow only when the prescription is a draft. |
| Prescription template tests | Test | Mirror Medical prescription tests under `tests/Modules/Prescription.Tests/` for ownership, validation, loading, and failure behavior. |

## 3. Dependency Direction
1. Domain entities define template state and invariants without UI, EF, provider adapter, or framework dependencies.
2. Application commands and queries depend on domain entities and contracts only.
3. Infrastructure handlers and EF mappings depend inward on application/domain contracts and implement persistence in the Medical prescription infrastructure area.
4. UI components depend on command/query contracts and route state only; they do not own business rules.
5. Provider capability checks remain behind existing prescription/medical application boundaries and are consumed by the load command rather than embedded in UI-only logic.

## 4. State Ownership and Source of Truth
- `PrescriptionTemplate` is the single source of truth for reusable template identity, ownership, metadata, item collection, and active/archived lifecycle state.
- `PrescriptionTemplateItem` is the single source of truth for reusable item values while they remain in the template.
- `PrescriptionRecord` remains the single source of truth for active prescription drafts and submitted prescriptions.
- Loading a template creates copied draft prescription items through `LoadPrescriptionTemplateIntoDraftCommand`; copied draft items have no active state link back to the mutable template.
- Provider medical code reference data remains owned by the Medical reference-code source; template items store only the reusable fields required to revalidate and copy into a draft.

## 5. Command / Query Split
- Queries return template data only and must not mutate usage state, last-used timestamps, draft contents, or audit-significant state.
- Commands mutate template state or draft state and return only command results needed for orchestration, such as ids, success/failure status, validation messages, or item-level load results.
- Loading a template is a command because it mutates `PrescriptionRecord` draft items.
- Template discovery from prescription creation is a query because it only reads templates available to the current prescribing user.

## 6. UX Stages

### Stage 1: Template List Discovery from Prescription Creation
| Step | User Action | System Response |
| --- | --- | --- |
| 1.1 | Opens `OwnerPrescriptionCreatePage` or a draft in `PrescriptionDetail`. | Shows a template entry point only in draft authoring contexts. |
| 1.2 | Opens template picker. | Runs `GetPrescriptionTemplatesQuery` scoped to the current user and shows active templates. |
| 1.3 | Filters or selects a template. | Shows metadata, item count, and any provider compatibility warnings available before load. |

### Stage 2: Create Template from Scratch
| Step | User Action | System Response |
| --- | --- | --- |
| 2.1 | Opens `PrescriptionTemplateEditor`. | Displays empty metadata and item editor. |
| 2.2 | Enters name and optional description. | Validates required metadata before command submission. |
| 2.3 | Saves template. | Runs `CreatePrescriptionTemplateCommand` for the current user and returns the new template id. |
| 2.4 | Adds reusable items. | Runs `AddPrescriptionTemplateItemCommand` per item after validating required item fields. |

### Stage 3: Optionally Create Template from Existing Prescription
| Step | User Action | System Response |
| --- | --- | --- |
| 3.1 | Chooses "Save as template" from an existing draft or submitted prescription if product workflow requires it now. | Opens `PrescriptionTemplateEditor` prefilled with copied item values, not linked prescription items. |
| 3.2 | Reviews copied metadata and items. | Requires explicit save through `CreatePrescriptionTemplateCommand` and item commands. |
| 3.3 | Cancels. | No template is created and no prescription state changes. |

### Stage 4: Edit Template Metadata and Items
| Step | User Action | System Response |
| --- | --- | --- |
| 4.1 | Opens an owned template. | Runs `GetPrescriptionTemplateQuery`; rejects access if the current user does not own the template. |
| 4.2 | Edits name or description. | Runs `UpdatePrescriptionTemplateCommand` with metadata validation. |
| 4.3 | Adds or removes reusable items. | Runs item commands; draft prescriptions loaded earlier are not changed. |
| 4.4 | Saves changes. | Persists template changes only. |

### Stage 5: Delete or Archive a Template
| Step | User Action | System Response |
| --- | --- | --- |
| 5.1 | Chooses archive/delete from the list or editor. | Shows confirmation and explains existing drafts/submissions are unaffected. |
| 5.2 | Confirms. | Runs `ArchivePrescriptionTemplateCommand` as the preferred lifecycle-preserving action. |
| 5.3 | Reopens picker. | Archived templates are hidden from default selection and can be excluded from `GetPrescriptionTemplatesQuery` results unless an archive view is requested. |

### Stage 6: Load Template into Draft Authoring Flow
| Step | User Action | System Response |
| --- | --- | --- |
| 6.1 | Selects a template in `PrescriptionTemplatePicker`. | Runs `GetPrescriptionTemplateQuery` for preview and ownership confirmation. |
| 6.2 | Clicks load into draft. | Runs `LoadPrescriptionTemplateIntoDraftCommand` against the current draft. |
| 6.3 | Command validates ownership, draft status, item fields, provider medical codes, provider capabilities, and duplicate conflicts. | If all checks pass, copied items are added as draft prescription items owned by `PrescriptionRecord`. |
| 6.4 | User continues authoring. | Draft validation and submission continue through the existing prescription flow. |

### Stage 7: Load Failure and Conflict Handling
| Scenario | System Behavior | UI Manifestation |
| --- | --- | --- |
| Current user does not own template | Reject command/query before exposing template detail. | Shows not-found or access-denied message without leaking another user's template metadata. |
| Target prescription is not draft | Reject `LoadPrescriptionTemplateIntoDraftCommand`. | Shows "Templates can only be loaded into drafts." |
| Required template metadata or item fields are invalid | Reject create/update/item/load command with field-level errors. | Highlights metadata or item rows requiring correction. |
| Medical code unavailable | Reject load for the affected item before draft mutation. | Shows code unavailable message and affected item. |
| Provider capability mismatch | Reject load for unsupported item categories or fields. | Shows provider capability guidance and payer/provider context. |
| Duplicate item conflict | Reject the load before draft mutation unless explicit replacement/skip behavior is later designed. | Shows duplicate items and asks user to edit draft or template. |
| Cancellation requested | Handler stops work and returns cancellation without partial mutation. | UI returns to safe idle or canceled state. |

## 7. Failure-Handling Strategy
- Access is scoped by owner user id for every command and query; non-owned templates are rejected.
- Loading into non-draft prescriptions is rejected before any item copy is attempted.
- Metadata validation requires a non-empty template name and valid owner context.
- Item validation requires all fields needed to create a valid draft prescription item.
- Provider compatibility is checked before load using selected draft provider capabilities and current Medical reference-code availability.
- The journey chooses **all-or-nothing loading** for now: if any template item is invalid, unavailable, unsupported, or duplicate-conflicting, no items are copied into the draft.
- Command handlers must propagate cancellation tokens through EF, medical-code, provider-capability, and draft persistence calls.
- Failures are returned as explicit command/query results for UI orchestration; exceptions are reserved for unexpected infrastructure failures and must be logged without leaking provider or internal details to the prescribing user.

## 8. Extensibility Points Justified Now
- Archive is preferred over hard delete because templates are user-authored reusable clinical content and may need lifecycle recovery or audit-safe hiding.
- `LoadPrescriptionTemplateIntoDraftCommand` is the single extension point for future item-level load policies; the current policy remains all-or-nothing to avoid partial clinical drafts.
- Provider capability validation is kept in the load command boundary so additional providers can be supported without moving business rules into UI components.

## 9. Intentionally Left Out
- Template sharing between users or clinics is excluded because the stated goal is user-scoped templates.
- Template versioning is excluded until there is a concrete audit or rollback requirement for template changes.
- Item-level partial loading is excluded for the first journey to keep draft mutation predictable and all-or-nothing.
- Usage analytics, last-used timestamps, and recommendation ranking are excluded because queries must not mutate usage state and no current requirement needs ranking.
- Generic repository, mediator, event sourcing, and CQRS framework additions are excluded because current command/query artifacts are sufficient for the planned journey.

## 10. Per-Artifact Rule Audit

| Artifact | SRP | Single state model | Low coupling | Abstraction | Command-query | Failure safety | Open/Closed | Core protection | KISS/YAGNI | Single source of truth |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `PrescriptionTemplate` | Pass — owns template state only. | Pass — reusable state only. | Pass — no UI/provider details. | Pass — domain entity. | N/A — entity. | Pass — enforces lifecycle invariants. | Pass — new entity. | Pass — core has no infrastructure dependency. | Pass — only required fields. | Pass — template state owner. |
| `PrescriptionTemplateItem` | Pass — owns reusable item data only. | Pass — reusable item state only. | Pass — no draft back-link. | Pass — domain value/entity. | N/A — entity. | Pass — validates required item fields. | Pass — new item artifact. | Pass — no infrastructure dependency. | Pass — no extra versioning. | Pass — reusable item owner. |
| Template commands | Pass — one mutation per command. | Pass — mutate template or draft source only. | Pass — use contracts. | Pass — application boundary. | Pass — commands do not act as queries. | Pass — explicit validation and cancellation. | Pass — new commands extend behavior. | Pass — no UI/EF dependency in contracts. | Pass — command set matches stages. | Pass — mutate authoritative owners. |
| Template queries | Pass — read template data only. | Pass — no usage mutation. | Pass — scoped read contracts. | Pass — application boundary. | Pass — queries do not mutate. | Pass — ownership rejection. | Pass — new queries extend reads. | Pass — no UI/EF dependency in contracts. | Pass — only list/detail. | Pass — read template source. |
| EF mappings and handlers | Pass — persistence/handler execution only. | Pass — persist domain owners. | Pass — infrastructure depends inward. | Pass — implement contracts. | Pass — handlers follow command/query intent. | Pass — propagate cancellation and failures. | Pass — new handlers. | N/A — infrastructure adapter. | Pass — no generic repository. | Pass — no duplicate business rules. |
| UI components | Pass — list, edit, or pick only. | Pass — do not own prescription state. | Pass — use command/query contracts. | Pass — presentation boundary. | Pass — invoke commands or queries explicitly. | Pass — display explicit failures. | Pass — new components. | N/A — presentation. | Pass — no speculative sharing UI. | Pass — display authoritative state. |
| Tests | Pass — verify planned behavior. | Pass — assert source boundaries. | Pass — test public boundaries. | Pass — use existing test seams. | Pass — command/query cases split. | Pass — cover failures. | Pass — mirror existing area. | N/A — tests. | Pass — targeted coverage. | Pass — assert source ownership. |

## 11. Trade-offs
- All-or-nothing loading is stricter than item-level partial loading, but it prevents hidden partial clinical drafts and keeps duplicate/conflict handling explicit.
- Archiving is preferred over hard delete to preserve lifecycle intent, but a hard delete command may still be considered later if retention policy permits.
- Creating from an existing draft or submitted prescription is marked optional because it should be implemented only if required now, not as speculative convenience.

## 12. Rejected Alternatives
- **Active link from draft item to template item:** Rejected because it creates a parallel active state model where mutable template edits could affect draft prescriptions.
- **Query that updates last-used state:** Rejected because queries must not mutate state.
- **Partial load by default:** Rejected because it creates clinical ambiguity when only some template items are copied.
- **Business validation in UI components:** Rejected because provider compatibility and draft mutation rules belong in application commands, not presentation artifacts.
