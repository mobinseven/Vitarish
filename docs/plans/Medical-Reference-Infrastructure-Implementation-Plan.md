# Medical Reference Infrastructure — Initial Implementation Plan

## Purpose
Build a stable **Medical** module that acts as the source of truth for shared medical reference data used by future clinical, reservation, billing, insurance, and prescription (future) modules.

This initial scope is **reference data only** (no patient-specific workflows).

---

## 0) Scope and Design Principles

### In scope
- Medical code systems and releases
- Medical codes and cross-standard mappings
- Drug reference catalog (drug, product, package)
- Medical service reference catalog and service coding
- RBRVS/RVU reference valuation data
- Import tracking, source versioning, effective dating
- Search/resolution query APIs for other modules
- Admin curation APIs for local reference additions/retirements

### Explicitly out of scope
- Prescriptions and medication request workflow
- Service order workflow
- Patient-specific clinical data
- Insurance coverage snapshots
- Authorization tokens
- Claim processing
- Dispensing workflow
- Appointment/provider scheduling
- Drug interaction checking, dose checking, formulary rules
- Medication administration records
- Inventory and revenue-cycle accounting
- Full clinical decision support
- Separate audit infrastructure

### Core principles applied
- Keep design simple and explicit
- One responsibility per component
- Imported standard data is immutable + versioned
- Separate imported data from local curation
- Separate write/import operations from read/query operations
- RBRVS/RVU treated as valuation reference data (not prices)
- External source quirks hidden behind adapters
- Use existing platform audit mechanism for all state changes
- One source of truth per code system release

---

## 1) Module Boundaries

Create a module namespace: `MedicalReferenceCatalog` with clear internal submodules.

### 1. Code System Registry
**Owns:**
- `CodeSystem`
- `CodeSystemRelease`
- `MedicalCode`
- Active release selection

**Responsibility:**
Lifecycle and lookup of terminology systems and immutable releases.

### 2. Drug Reference Catalog
**Owns:**
- `Drug`
- `DrugProduct`
- `PackagedDrug`
- Drug identifier resolution rules (RxNorm/NDC/local)

**Responsibility:**
Canonical drug reference entities and relationships.

### 3. Service Reference Catalog
**Owns:**
- `MedicalService`
- `ServiceCode`

**Responsibility:**
Clinical/operational service concepts and their standard/local coding.

### 4. Service Valuation Catalog
**Owns:**
- `ServiceValuation`

**Responsibility:**
Versioned RVU/RBRVS-style valuation data by code/year/locality/facility context.

### 5. Code Mapping Registry
**Owns:**
- `CodeMap`

**Responsibility:**
Cross-code-system relationships with type/confidence/effective dating.

### 6. Import Pipeline
**Owns:**
- `ImportBatch`
- Adapter orchestration
- Validation and persistence workflow

**Responsibility:**
Translate external source payloads into canonical command inputs, track results.

### 7. Reference Search API (Read Model)
**Owns:**
- Search projections/indexes
- Typeahead and resolution query endpoints

**Responsibility:**
Fast, read-only retrieval for consuming modules.

### 8. Administrative Curation API
**Owns:**
- Local reference curation commands (create/update/retire local data)
- Activation/deactivation and review actions

**Responsibility:**
Controlled manual maintenance of local curated reference records.

---

## 2) Data Separation Model

Use clear data ownership partitions.

## A. Global medical reference data
- Shared canonical entities and release metadata.
- No tenant overrides directly in canonical imported rows.

## B. Imported standard data
- Rows created by import adapters and linked to `ImportBatch`.
- Immutable after successful import.
- Corrections happen via newer release or local overlay records.

## C. Locally curated reference data
- Locally authored records (e.g., local services, local code additions, local mappings).
- Explicitly marked `record_origin = Local`.
- Never overwrite imported rows.

## D. Tenant/clinic-specific configuration (optional)
- Minimal configuration tables only (e.g., preferred release pointer, local code visibility flags).
- Tenant data references global canonical IDs.
- No tenant-specific duplication of imported standards.

## E. Import/integration logic
- External adapters outside core domain.
- Pipeline normalizes into internal commands.

## F. Read/search models
- Denormalized search tables/indexes generated from canonical tables.
- Rebuildable at any time from source-of-truth tables.

---

## 3) Data Model (Entities, Fields, Relationships, Constraints)

> Naming uses simple, expressive terms; avoids ambiguous names like `CodeConcept`.

### 3.1 CodeSystem
**Purpose:** Supported terminology/code system definition.

**Key fields:**
- `id` (PK)
- `system_key` (stable unique key, e.g., `RXNORM`, `NDC`, `CPT`)
- `name`
- `authority`
- `category` (Drug, Diagnosis, Procedure, Service, Local, Other)
- `canonical_uri`
- `description`
- `is_active`
- `created_at`, `updated_at`

**Constraints:**
- Unique(`system_key`)
- `canonical_uri` optional but if present must be valid URI format

### 3.2 CodeSystemRelease
**Purpose:** Immutable release/version of one code system.

**Key fields:**
- `id` (PK)
- `code_system_id` (FK -> CodeSystem)
- `release_name` (e.g., `2026-01`, `Q1-2026`)
- `release_date`
- `effective_from`, `effective_to` (nullable)
- `source_locator` (URL/path)
- `source_version`
- `import_batch_id` (FK -> ImportBatch)
- `is_active`
- `created_at`

**Constraints:**
- Unique(`code_system_id`, `release_name`)
- One release belongs to exactly one code system
- `effective_to` >= `effective_from` when provided

### 3.3 MedicalCode
**Purpose:** Code term within one release.

**Key fields:**
- `id` (PK)
- `code_system_release_id` (FK)
- `code`
- `display_name`
- `long_description`
- `status` (Imported, Active, Deprecated, Retired, Superseded | Draft/Active/Retired for local)
- `effective_from`, `effective_to`
- `record_origin` (Imported, Local)
- `source_metadata_json`
- `import_batch_id` (FK, nullable for local)
- `created_at`, `updated_at`

**Constraints:**
- Unique(`code_system_release_id`, `code`, `record_origin`)
- Every medical code belongs to exactly one release
- Imported rows immutable post-import success

### 3.4 CodeMap
**Purpose:** Mapping between source and target medical codes.

**Key fields:**
- `id` (PK)
- `source_medical_code_id` (FK -> MedicalCode)
- `target_medical_code_id` (FK -> MedicalCode)
- `mapping_type` (Equivalent, Narrower, Broader, Related, LocalOverride)
- `confidence` (0..1 decimal)
- `status` (Draft/Active/Retired or Imported/Active/etc.)
- `effective_from`, `effective_to`
- `record_origin` (Imported, Local)
- `import_batch_id` (nullable)
- `source_note`

**Constraints:**
- Source and target must exist
- Optional identity uniqueness: Unique(`source_medical_code_id`, `target_medical_code_id`, `mapping_type`, `record_origin`, `effective_from`)

### 3.5 Drug
**Purpose:** Clinical drug concept.

**Key fields:**
- `id` (PK)
- `primary_name`
- `normalized_name`
- `primary_identifier_type` (RxNorm, ATC, Local)
- `primary_identifier_value`
- `status`
- `effective_from`, `effective_to`
- `record_origin`
- `import_batch_id` (nullable)

**Constraints:**
- Unique(`primary_identifier_type`, `primary_identifier_value`, `record_origin`, `effective_from`)
- `normalized_name` indexed for search

### 3.6 DrugProduct
**Purpose:** Selectable/marketed drug product tied to drug concept.

**Key fields:**
- `id` (PK)
- `drug_id` (FK -> Drug)
- `brand_name`
- `generic_name`
- `dose_form`
- `route`
- `strength`
- `manufacturer`
- `market_status` (Active, Discontinued, etc.)
- `effective_from`, `effective_to`
- `record_origin`
- `import_batch_id`

**Constraints:**
- Must reference valid `drug_id`

### 3.7 PackagedDrug
**Purpose:** Package-level product identity (NDC/package unit context).

**Key fields:**
- `id` (PK)
- `drug_product_id` (FK -> DrugProduct)
- `package_identifier`
- `identifier_source` (NDC, Local, Other)
- `ndc_code` (nullable)
- `package_size`
- `package_unit`
- `package_description`
- `marketing_start_date`, `marketing_end_date`
- `source_release_id` (FK -> CodeSystemRelease, nullable)
- `record_origin`
- `import_batch_id`

**Constraints:**
- Must reference valid `drug_product_id`
- Unique(`identifier_source`, `package_identifier`, `source_release_id`)

### 3.8 MedicalService
**Purpose:** Meaning-level service concept.

**Key fields:**
- `id` (PK)
- `name`
- `category`
- `specialty`
- `description`
- `status`
- `effective_from`, `effective_to`
- `record_origin`
- `import_batch_id` (nullable)

### 3.9 ServiceCode
**Purpose:** Link between medical service and a coded representation.

**Key fields:**
- `id` (PK)
- `medical_service_id` (FK -> MedicalService)
- `code_system_release_id` (FK -> CodeSystemRelease)
- `code`
- `display_name`
- `modifier_applicability_json`
- `is_primary`
- `status`
- `effective_from`, `effective_to`
- `record_origin`
- `import_batch_id`

**Constraints:**
- Must reference valid service + release
- Unique(`medical_service_id`, `code_system_release_id`, `code`, `record_origin`, `effective_from`)

### 3.10 ServiceValuation
**Purpose:** Versioned RVU/RBRVS valuation reference for service code.

**Key fields:**
- `id` (PK)
- `service_code_id` (FK -> ServiceCode)
- `valuation_year`
- `locality`
- `facility_indicator` (Facility/NonFacility/Other)
- `work_rvu`
- `practice_expense_rvu`
- `malpractice_rvu`
- `conversion_factor` (optional)
- `policy_indicators_json`
- `effective_from`, `effective_to`
- `source_release_id` (FK -> CodeSystemRelease or dedicated valuation release)
- `record_origin`
- `import_batch_id`

**Constraints:**
- Unique(`service_code_id`, `valuation_year`, `locality`, `facility_indicator`, `record_origin`, `effective_from`)
- Must not be treated as price fields by domain naming or API docs

### 3.11 ImportBatch
**Purpose:** Trace one import operation and outcomes.

**Key fields:**
- `id` (PK)
- `source_type` (RxNorm, NDC, CPT, HCPCS, ICD, ICHI, ATC, RBRVS, LocalCsv)
- `source_version`
- `source_locator`
- `source_checksum`
- `status` (Pending, Validating, Imported, Failed, PartiallyImported, RolledBack)
- `validation_summary`
- `error_report`
- `imported_by`
- `imported_at`
- `started_at`, `completed_at`

**Constraints:**
- Unique(`source_type`, `source_version`) to ensure one logical batch per published version
- Unique(`source_checksum`) to guarantee checksum-level idempotency
- If re-processing is needed, create an explicit reimport record linked to the original batch instead of inserting a second Imported batch for the same version

---

## 4) Database Design

### Canonical tables
- `code_system`
- `code_system_release`
- `medical_code`
- `code_map`
- `drug`
- `drug_product`
- `packaged_drug`
- `medical_service`
- `service_code`
- `service_valuation`
- `import_batch`

### Optional tenant configuration tables (no canonical duplication)
- `tenant_reference_preference` (tenant -> preferred release per code system)
- `tenant_reference_visibility` (tenant -> local enable/disable flags)

### Keys and FK summary
- Surrogate UUID/ULID PKs for all tables.
- Required FK constraints for all parent-child relationships.
- Restrict delete for canonical parents; use retirement statuses instead of deletes.

### Uniqueness rules (minimum)
- Medical code unique inside release
- Release name unique inside code system
- Packaged identifier unique in source+release context
- Service valuation unique across service_code/year/locality/facility/effective_from

### Indexes
- `medical_code(code_system_release_id, code, record_origin)` unique + btree
- `medical_code(display_name)` trigram/full-text index for typeahead
- `drug(normalized_name)` full-text/trigram
- `packaged_drug(identifier_source, package_identifier)`
- `service_code(code_system_release_id, code)`
- `service_valuation(service_code_id, valuation_year, locality, facility_indicator)`
- `code_map(source_medical_code_id)` and `code_map(target_medical_code_id)`
- `import_batch(status, source_type, imported_at)`

### Versioning strategy
- Immutable imported rows keyed to a specific `CodeSystemRelease`/`ImportBatch`.
- New source publication => new release rows; no in-place update.
- Local curation stored as separate local-origin rows.

### Effective date strategy
- All major entities include `effective_from/effective_to`.
- Query helpers: `is_effective_on(date)` derived predicate.
- No overlapping effective ranges for same uniqueness dimension unless explicitly allowed.

### Retirement strategy
- Soft retirement via status transitions (`Active -> Retired`, etc.).
- Keep historical rows queryable for audit/history lookups.

### Import traceability
- Every imported canonical row stores `import_batch_id`.
- `ImportBatch` stores source version/checksum/status/errors.

### Tenant boundaries
- Default: global shared reference catalog.
- If tenant config enabled, store only selection/visibility preferences in tenant tables.
- Tenant tables cannot modify imported canonical content.

---

## 5) Command Model (Write Side)

All commands:
- Execute through command handlers
- Use existing platform authorization policy
- Use existing platform audit mechanism on success/failure state changes

### 5.1 Registry & import commands
1. **RegisterCodeSystem**
   - Input: system_key, name, authority, category, uri, description
   - Validate: unique system_key, category valid
   - Auth: PlatformReferenceAdmin
   - Result: code_system_id
   - Failures: duplicate key, invalid uri/category

2. **RegisterCodeSystemRelease**
   - Input: code_system_id, release_name/date, effective range, source metadata
   - Validate: code_system exists, uniqueness, date consistency
   - Auth: PlatformReferenceAdmin
   - Result: release_id
   - Failures: missing code system, duplicate release, invalid dates

3. **ImportCodeSystemRelease**
4. **ImportDrugCatalog**
5. **ImportServiceCatalog**
6. **ImportServiceValuations**
   - Input: adapter payload ref + metadata + dry-run flag
   - Validate: source checksum/version integrity, schema validation, reference integrity
   - Auth: ReferenceImporter
   - Result: import_batch_id + counts + warnings
   - Failures: parse failure, validation failure, FK conflicts, duplicate import

### 5.2 Local curation commands
7. **CreateLocalMedicalCode**
8. **RetireMedicalCode**
9. **CreateCodeMap**
10. **RetireCodeMap**
11. **CreateDrug**
12. **UpdateLocalDrug**
13. **RetireDrug**
14. **CreateMedicalService**
15. **UpdateLocalMedicalService**
16. **RetireMedicalService**
   - Input: entity-specific fields + effective dates
   - Validate: origin must be Local for updates; imported rows not editable
   - Auth: ReferenceCurator
   - Result: entity id/version
   - Failures: immutable imported row update attempt, invalid state transition, constraint conflicts

### 5.3 Release/state/admin commands
17. **ActivateCodeSystemRelease**
18. **DeactivateCodeSystemRelease**
   - Validate: release exists; activation policy (single active per system or multi-active by date rule)
   - Auth: PlatformReferenceAdmin

19. **RebuildReferenceSearchIndex**
   - Validate: canonical tables accessible
   - Auth: PlatformReferenceAdmin or Operations
   - Result: rebuild job id + completion status

---

## 6) Query Model (Read Side)

Provide read-only query services/endpoints optimized for consumption.

### Drug queries
- `SearchDrugs(text, activeOnly, asOfDate, limit, tenantId?)`
- `GetDrug(drugId, asOfDate?)`
- `GetDrugProduct(drugProductId, asOfDate?)`
- `GetPackagedDrug(packageIdOrIdentifier, source?, asOfDate?)`
- `ResolveDrugIdentifier(identifierType, identifierValue, releaseId?, asOfDate?)`

### Service queries
- `SearchMedicalServices(text, category?, specialty?, activeOnly, asOfDate)`
- `GetMedicalService(serviceId, asOfDate?)`
- `ResolveServiceCode(codeSystemKey, code, releaseId?, asOfDate?)`
- `GetServiceValuation(serviceCodeId|code, year, locality, facilityIndicator, asOfDate?)`

### Terminology/mapping queries
- `SearchMedicalCodes(systemKey, textOrCode, releaseId?, activeOnly, asOfDate)`
- `ResolveMedicalCode(systemKey, code, releaseId?, asOfDate?)`
- `GetCodeMaps(sourceCodeId|system+code, targetSystem?, mappingType?, asOfDate?)`
- `GetActiveCodeSystemRelease(systemKey, asOfDate?)`
- `GetImportBatchStatus(importBatchId)`

### Query capabilities required
- Typeahead + exact lookup
- Release-specific + active-only + historical-as-of-date
- Cross-code-system mapping traversal
- Service valuation by year/locality/facility

---

## 7) Status and Lifecycle

### Imported reference records
States: `Imported -> Active -> Deprecated -> Retired` and optional `Superseded`.

Valid transitions:
- Imported -> Active (on release activation)
- Active -> Deprecated
- Deprecated -> Retired
- Active/Deprecated -> Superseded (when replacement release/map is available)

### Local records
States: `Draft -> Active -> Retired`

Valid transitions:
- Draft -> Active
- Active -> Retired
- Retired is terminal (create new record for reactivation scenarios)

### ImportBatch
States: `Pending -> Validating -> Imported`
Error branches:
- `Validating -> Failed`
- `Validating -> PartiallyImported`
- `Imported/PartiallyImported -> RolledBack` (admin action)

### Roles
- **PlatformReferenceAdmin:** register systems/releases, activate/deactivate, rebuild index, rollback batch
- **ReferenceImporter:** execute imports and view import diagnostics
- **ReferenceCurator:** create/update/retire local records and local mappings
- **ReferenceReader:** query-only access (other modules/services)

---

## 8) Validation Rules and Invariants

1. Every `MedicalCode` belongs to exactly one `CodeSystemRelease`.
2. Every `CodeSystemRelease` belongs to exactly one `CodeSystem`.
3. `(code_system_release_id, code, record_origin)` must be unique, allowing imported and local overlay rows to coexist.
4. Imported records are immutable after import success.
5. Local corrections never overwrite imported rows.
6. `CodeMap` must point to valid source/target `MedicalCode` rows.
7. `ServiceValuation` must point to valid `ServiceCode`.
8. RVU/RBRVS fields are reference valuation data, not pricing.
9. `PackagedDrug` must point to valid `DrugProduct`.
10. `DrugProduct` must point to valid `Drug`.
11. `ServiceCode` must point to valid `MedicalService`.
12. Search read model must be rebuildable from canonical tables.
13. Every imported row must be traceable to one `ImportBatch`.
14. Every state-changing operation must go through platform audit mechanism.
15. Effective date intervals must be valid (`from <= to`) and non-overlapping per uniqueness dimension unless explicitly allowed.
16. For PostgreSQL deployments, enforce non-overlap with exclusion constraints (`EXCLUDE USING GIST`) on each interval-managed entity; for non-PostgreSQL engines, enforce the same invariant in command-handler validation before commit.

---

## 9) Integration Boundaries and Adapters

Adapters stay in infrastructure integration layer, not in core domain.

### Planned adapters
- RxNorm import adapter
- NDC import adapter
- ATC import adapter
- ICD import adapter
- ICHI import adapter
- CPT/HCPCS import adapter
- RBRVS/RVU import adapter
- Local CSV import adapter
- Future FHIR terminology adapter

### Adapter contract
Each adapter must:
1. Parse source-specific payload/API
2. Normalize into internal DTOs
3. Emit canonical import commands (`Import*`)
4. Attach source version/checksum/provenance
5. Report structured validation issues

Core domain must **not** depend on:
- Source file formats
- Vendor API quirks
- Payer-specific behaviors
- External terminology transport details

---

## 10) Phased Implementation Sequence

## Phase 1 — Core Code System Registry
**Goal:** Establish immutable terminology release foundation.

**Deliverables:**
- Entities/tables: `CodeSystem`, `CodeSystemRelease`, `MedicalCode`, `ImportBatch`
- Core commands: Register system/release, import code release, activate/deactivate release
- Basic queries: resolve code, active release lookup, import status
- Audit integration wired for all commands

**Acceptance criteria:**
- Can import one code system release end-to-end with traceable batch
- Duplicate code in same release blocked
- Imported rows immutable

**Migration notes:**
- Initial schema migration for core tables + indexes

**Risks:**
- Large terminology import volume/performance

**Deferred:**
- Advanced cross-system mapping

## Phase 2 — Drug Reference Catalog
**Goal:** Add drug concept/product/package model and identifier resolution.

**Deliverables:**
- Entities/tables: `Drug`, `DrugProduct`, `PackagedDrug`
- Import command + adapter integration for drug sources
- Queries: SearchDrugs, GetDrug/Product/PackagedDrug, ResolveDrugIdentifier

**Acceptance criteria:**
- Drug/product/package relationships enforce FK constraints
- Package identifier uniqueness enforced by source+release

**Migration notes:**
- Add drug tables and search indexes

**Risks:**
- Identifier normalization mismatch across sources

**Deferred:**
- Clinical prescribing semantics

## Phase 3 — Service Reference Catalog
**Goal:** Model service meaning + coding links.

**Deliverables:**
- Entities/tables: `MedicalService`, `ServiceCode`
- Import/local curation commands
- Queries: SearchMedicalServices, ResolveServiceCode

**Acceptance criteria:**
- Service codes resolve by system/release/code reliably
- Local services can coexist with imported services without overwrite

**Migration notes:**
- Add service tables/indexes

**Risks:**
- Ambiguous mapping when multiple active releases exist

**Deferred:**
- Order workflow links

## Phase 4 — Service Valuation Catalog
**Goal:** Add RVU/RBRVS valuation reference support.

**Deliverables:**
- Entity/table: `ServiceValuation`
- ImportServiceValuations command + adapter
- Query: GetServiceValuation(year/locality/facility)

**Acceptance criteria:**
- Uniqueness rule enforced for valuation dimension
- API/docs clearly describe values as valuation references, not prices

**Migration notes:**
- Add valuation table/indexes

**Risks:**
- Frequent annual updates requiring operational import discipline

**Deferred:**
- Payer-specific fee schedules

## Phase 5 — Code Mapping Registry
**Goal:** Add interoperable code mapping.

**Deliverables:**
- Entity/table: `CodeMap`
- Commands: Create/RetireCodeMap + import map support
- Query: GetCodeMaps and map traversal

**Acceptance criteria:**
- Map integrity enforced (valid source/target codes)
- Effective dating and status filtering supported

**Migration notes:**
- Add mapping table/indexes

**Risks:**
- Mapping conflict rules across multiple map sources

**Deferred:**
- Automated semantic conflict resolution

## Phase 6 — Search and Read APIs
**Goal:** Add high-performance query interface.

**Deliverables:**
- Denormalized search projection tables/indexes
- Query endpoints for typeahead/exact/historical lookups
- RebuildReferenceSearchIndex command/job

**Acceptance criteria:**
- P95 lookup targets met for core queries
- Index rebuild from canonical tables validated

**Migration notes:**
- Add projection schema and backfill job

**Risks:**
- Index staleness if projection updates fail

**Deferred:**
- Relevance tuning beyond baseline

## Phase 7 — Administrative Curation API
**Goal:** Enable operational local curation with governance.

**Deliverables:**
- Local create/update/retire command endpoints
- Import review/approval screens or APIs
- Role-based authorization and workflow docs

**Acceptance criteria:**
- Imported data cannot be edited directly
- Local curation captured via audit and visible in history

**Migration notes:**
- Optional tenant preference tables if enabled

**Risks:**
- Misuse of local curation creating terminology drift

**Deferred:**
- Advanced stewardship workflows

---

## 11) Minimal API Surface (First Cut)

To keep implementation simple, start with:
- Command handlers (write side) behind internal application contracts
- Query service interfaces (read side) for each catalog
- One import orchestration service + adapter abstraction
- One search projection updater + rebuild command

Avoid premature abstractions beyond these boundaries.

---

## 12) Implementation Guardrails for Engineering

1. Keep canonical imported tables append-only by release.
2. Never mutate imported rows to fix errors; ship new release or local overlay.
3. Use `record_origin` consistently (`Imported` vs `Local`).
4. Keep tenant customization in separate tenant tables only.
5. Keep query/read services isolated from write/import services.
6. Ensure all command handlers invoke platform audit hooks.
7. Maintain deterministic import behavior (idempotent by checksum/version where possible).
8. Add integration tests per phase for invariants and lifecycle transitions.

This plan delivers a simple, stable reference foundation while preserving extensibility for future clinical and financial modules.
