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
3. Link providers to existing users (or create invitations inline).
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
  - duration
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

> All panels (tree, detail editor, validation, progress) read/write this same session state.

## Journey Stages

### Stage 0 - Entry and Intent
**Trigger**: Owner selects "Set up appointments" for the first time.

**System response**:
- Open onboarding workspace in focused mode.
- Show estimated completion time (e.g., "~8 minutes").
- Offer 2 start paths:
  - Start from template (recommended)
  - Start from scratch

**Friction reduction**:
- One-click template import with editable nodes.
- Auto-save draft immediately.

---

### Stage 1 - Build Service Tree (Types -> Services)
**Goal**: Define what can be booked.

**UI pattern**:
- Left pane: editable tree.
- Right pane: contextual properties for selected node.

**Key interactions**:
- Add service type inline (`+ Type`).
- Add service under selected type (`+ Service`).
- Reorder by drag-and-drop.
- Bulk-add services via comma/newline paste.

**Validation**:
- Service type name required.
- Service name and duration required.
- Duplicate names allowed only across different types (configurable rule).

**Microcopy examples**:
- "Start with broad categories, then add bookable services."
- "Tip: Paste a list to add multiple services quickly."

---

### Stage 2 - Add Providers and Link Users
**Goal**: Define who performs services and tie to account identity.

**UI pattern**:
- Split view with provider list and selected provider details.

**Key interactions**:
- Quick-add provider rows (name-first, details later).
- Link to existing users via searchable dropdown.
- If no user found, invite inline (email only) without leaving flow.
- Show linkage status badges: Linked / Invited / Missing.

**Validation**:
- Provider name required.
- Publish-blocking rule: each provider must be linked or invited.

**Friction reduction**:
- Keyboard-first entry for rapid multi-provider creation.
- Auto-suggest user match by provider name/email.

---

### Stage 3 - Assign Services to Providers
**Goal**: Configure provider capability matrix with minimal clicks.

**UI pattern options** (choose one based on usability testing):
1. **Matrix View (recommended)**
   - Rows: Providers
   - Columns: Services
   - Cell: toggle assignment
2. **Service-centric Tree View**
   - Expand service -> check providers

**Key interactions**:
- Bulk assign by service type.
- Copy assignments from one provider to another.
- "Assign all services in this type" shortcut.

**Validation**:
- Publish-blocking: every service has at least one provider.
- Warning: provider has zero services assigned.

---

### Stage 4 - Review and Publish
**Goal**: Confirm setup completeness and activate booking.

**Review summary blocks**:
- Total service types/services
- Total providers
- Unlinked/invited user count
- Unassigned services (if any)

**Actions**:
- "Fix issues" deep-links to exact node.
- "Save draft".
- "Publish appointments" (primary CTA).

**Post-publish**:
- Success state with next-step actions:
  - Configure availability calendar
  - Test booking flow
  - Share booking link

## Interaction Model (Compact, Low-Click)
- Persistent left tree + center workspace + right inspector.
- Inline create/rename/delete with confirmation only for destructive actions.
- Command bar with high-frequency actions:
  - Add Type
  - Add Service
  - Add Provider
  - Link User
  - Bulk Assign
- Global undo for recent edits during onboarding session.

## Error Handling and Safety
- Autosave every meaningful mutation.
- Non-blocking warnings shown inline and in summary panel.
- Publish blocked only by critical constraints:
  1. At least one service type and one service exist.
  2. Every service has >= 1 assigned provider.
  3. Every provider is linked to a user or has a pending invite.
- On network/API failure: keep local draft state, show retry CTA, never discard entered data.

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
3. Owner can assign services to providers using bulk or matrix interactions.
4. System blocks publish only for critical constraints and provides direct fix navigation.
5. Draft progress is recoverable after refresh/session interruption.

## Non-Goals
- Advanced scheduling rules (breaks, recurring availability, holiday logic).
- Billing package setup.
- Multi-location optimization.

## Future Extensions
- Import services/providers from CSV.
- AI-assisted template generation from clinic specialty.
- Role-based delegated onboarding tasks.
- Guided "smart defaults" based on similar clinics.
