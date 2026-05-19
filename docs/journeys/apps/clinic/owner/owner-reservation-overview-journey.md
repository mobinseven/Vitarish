# Clinic Owner Reservation Overview Journey

## 1. Context
- Product / service: Vitarish Clinic owner app reservation module
- Journey type: Target state
- Scenario: Clinic owner needs a single overview of reservations across providers and sessions.
- User goal: Understand reservation load, risk, and priorities quickly.
- Business goal: Improve seat utilization and reduce no-shows/cancellations.
- Scope start: Owner opens Reservations module.
- Scope end: Owner identifies actions and navigates to the correct management flow.
- Primary persona: Clinic owner

## 2. Persona Snapshot
**Name / segment:** Clinic owner

**Situation:** Managing daily/weekly appointment demand.

**Motivation:** Keep schedules full while protecting care quality.

**Key needs:** Clear counts, statuses, alerts, and next actions.

**Success means:** Owner can decide what to do in under 60 seconds.

## 3. Journey Summary
**Narrative:** The owner opens the reservation module and sees a consolidated dashboard with today’s totals, status distribution, waitlist pressure, and critical alerts. From each insight card, the owner drills into filtered list or calendar views and resolves issues.

**Biggest friction point:** Overview data is fragmented across different pages.

**Highest-value opportunity:** Provide one source-of-truth overview read model for reservation health.

## 4. Journey Stages

### Stage 1: Open reservation overview
**User goal:** Get a fast operational snapshot.

**User actions:** Open reservation module landing page.

**Touchpoints / channels:** Reservation overview page.

**Needs:** Today/this-week KPI cards (booked, confirmed, waitlisted, cancelled, completed, no-show).

**Opportunities:** Add trend indicators versus previous week.

### Stage 2: Detect risk
**User goal:** Identify urgent scheduling issues.

**User actions:** Review alert widgets and exception tables.

**Touchpoints / channels:** Alert panel; exception queues.

**Needs:** Over-capacity risk, waitlist spikes, high cancellation/no-show rates.

**Opportunities:** Prioritized alert severity and recommended actions.

### Stage 3: Decide next action
**User goal:** Route to resolution flow immediately.

**User actions:** Click card/alert to open filtered list or calendar.

**Touchpoints / channels:** KPI cards; alert links.

**Needs:** Deep links that preserve filter context.

**Opportunities:** “Resolve now” actions with pre-applied filters.

## 5. Functional capabilities this module shall provide
1. Unified reservation KPIs with real-time refresh.
2. Status distribution by day/week/month.
3. Alert engine for capacity, cancellation, and waitlist anomalies.
4. Drill-down links into list and calendar views with shared filters.
5. Exportable overview (CSV/PDF) for operational review.

