# Clinic Owner Reservation List and Calendar Journey

## 1. Context
- Product / service: Vitarish Clinic owner app reservation module
- Journey type: Target state
- Scenario: Clinic owner switches between list and calendar to inspect reservations.
- User goal: Find reservations quickly and understand schedule distribution over time.
- Business goal: Improve planning accuracy and reduce manual coordination.
- Scope start: Owner chooses list or calendar view.
- Scope end: Owner locates target reservations and prepares action.
- Primary persona: Clinic owner / scheduler

## 2. Persona Snapshot
**Name / segment:** Owner-scheduler

**Situation:** Balancing day-to-day scheduling with strategic oversight.

**Motivation:** Quickly find what needs attention.

**Key needs:** Powerful filtering, timeline visibility, and context continuity.

**Success means:** Same query context works in both list and calendar.

## 3. Journey Summary
**Narrative:** The owner starts in list view for precision filtering, then toggles to calendar view to validate time distribution and conflicts. The selected filters, date range, clinic/location/provider/session state, and patient context persist across views.

**Biggest friction point:** Losing context when switching views.

**Highest-value opportunity:** One shared query/filter state model across list and calendar.

## 4. Journey Stages

### Stage 1: Filter in list view
**User goal:** Narrow results to relevant reservations.

**User actions:** Set date range, status, provider, room, session, and patient filters.

**Touchpoints / channels:** Reservation list; advanced filter drawer.

**Needs:** Fast search with saved filter presets.

**Opportunities:** Natural-language quick filter chips.

### Stage 2: Toggle to calendar view
**User goal:** Verify spatial/temporal distribution.

**User actions:** Switch from list to day/week/month calendar.

**Touchpoints / channels:** View toggle control.

**Needs:** Persisted filters and highlighted conflict markers.

**Opportunities:** Heatmap overlays for demand peaks.

### Stage 3: Inspect and open details
**User goal:** Understand reservation context before changing state.

**User actions:** Click list row or calendar slot.

**Touchpoints / channels:** Reservation detail side panel/dialog.

**Needs:** Read-only summary plus clear action shortcuts.

**Opportunities:** Adjacent slot suggestions for rescheduling.

## 5. Functional capabilities this module shall provide
1. Dual-mode list/calendar UI backed by one filter/query model.
2. Day/week/month calendar with status color coding.
3. Advanced filters (date, status, provider, room, patient, session, source).
4. Saved views/presets per owner/team.
5. Infinite-scroll list and lazy-loaded calendar events for performance.

