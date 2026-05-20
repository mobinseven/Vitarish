# Clinic Appointment Onboarding Setup Journey Map

## Purpose
Define a first-time onboarding journey for a **clinic owner** to configure appointment scheduling using a **single tree-based setup experience**:

- Service Types -> Services
- Service Providers -> Linked Users
- Provider-to-Service assignment

The design goal is a compact, intuitive flow with minimum clicks and context switches, replacing fragmented `list -> add/edit` CRUD screens per entity.

## Primary User
- **Clinic Owner** (or delegated Clinic Admin) with authority to configure services, providers, and user linkage.

## Outcome / Success Criteria
By the end of onboarding, the clinic owner can:
1. Create service taxonomy (types + services).
2. Register providers.
3. Link providers to existing users.
4. Assign which providers can perform which services.
5. Publish the setup so appointments can be booked.

## UX Principles
1. **Single active setup context**: one wizard/session controls the whole onboarding state.
2. **Tree-first interaction**: nested relationships are visible and editable in-place.
3. **Progressive disclosure**: start minimal, expand details only when needed.
4. **Inline actions over page hops**: quick add, rename, assign from the same canvas.
5. **Safe validation**: block publish only for critical errors; warn for optional data.
6. **Simple defaults**: prefill common values and suggested structures.

## Information Architecture (Single Source of Truth)
Use one onboarding state model to back all UI panels:

- **ServiceTypeNode**
  - id
  - name
  - order
  - children: ServiceNode[]
- **ServiceNode**
  - id
  - serviceTypeId
  - name
  - duration (in minutes)
  - price (optional initially)
  - assignedProviderIds[]
- **ProviderNode**
  - id
  - displayName
  - specialization (optional)
  - linkedUserId (nullable until linked)
  - serviceIds[]
- **LinkedUser**
  - id
  - fullName
  - email
  - status (active, invited, pending)

## Journey Stages

### Stage 0 - Entry and Intent
**Trigger**: Owner selects "appointments settings" from clinic dashboard.

**System response**:
- Open onboarding workspace in focused mode.

---

### Stage 1 - Build Service Tree (Types -> Services)
**Goal**: Define what can be booked/reserved.

**UI pattern**:
- Editable tree.

**Key interactions**:
- Add service type inline (`+ Type`).
- Add service under selected type (`+ Service`).

**Validation**:
- Service type name required.

**Microcopy examples**:
- "Start with broad categories, then add bookable services."

---

### Stage 2 - Add Providers and Link Users
**Goal**: Define who performs services and tie to account identity.

**UI pattern**:
- Same Tree view from stage 1 with provider list as drop down and ability to add new one on each service instance.

**Key interactions**:
- Select from providers list.
- Quick-add provider nodes (name-first, details later).
- Link to existing users via searchable dropdown.
- Show linkage status badges: Linked / Invited / Missing.

**Validation**:
- Provider name required.

**Friction reduction**:
- Keyboard-first entry for rapid multi-provider creation.


## Interaction Model (Compact, Low-Click)
- Persistent Tree view
- Inline create/rename/delete with confirmation only for destructive actions.
- Command bar with high-frequency actions:
  - Add Type
  - Add Service
  - Add/Find Provider
  - Link User

## Error Handling and Safety
- Autosave every meaningful mutation.
- Non-blocking warnings shown inline and in summary panel.

## Suggested Telemetry
Track friction and completion quality:
- Time to first service created.
- Total onboarding completion time.
- Drop-off stage.
- Number of clicks/actions per stage.
- Validation errors by type.
- Template vs scratch completion rate.

## Acceptance Criteria (Journey-Level)
1. Owner can complete first-time setup without leaving onboarding workspace.
2. Owner can create type -> service hierarchy and provider -> user links in one continuous flow.

## Non-Goals
- Advanced scheduling rules (breaks, recurring availability, holiday logic).
- Billing package setup.
- Multi-location optimization.

## Future Extensions
- Import services/providers from CSV.
- AI-assisted template generation from clinic specialty.
- Role-based delegated onboarding tasks.
- Guided "smart defaults" based on similar clinics.
