# Clinic Owner Reservation Module Capability Journeys

## Purpose
This map lists additional high-value journeys the clinic reservation module shall provide beyond overview and core lifecycle management.

## Journey set

### 1. Waitlist and fill-gap journey
- Detect waitlisted demand per session.
- Trigger auto-promotion when a cancellation occurs.
- Notify promoted patients with confirmation window.
- Escalate unclaimed slots back to queue.

### 2. Follow-up reservation journey
- Start follow-up from completed reservation.
- Prefill patient, provider, and recommended time window.
- Offer nearest available slots.
- Save follow-up link for continuity of care.

### 3. Bulk operations journey
- Select multiple reservations in list view.
- Apply bulk confirm/cancel/message actions with safeguards.
- Validate each row and show partial-success reporting.

### 4. Communication and reminders journey
- Configure reminder cadence (email/SMS/in-app).
- Track delivery and acknowledgment status.
- Re-send failed reminders with fallback channel.

### 5. Exception handling journey
- Handle late arrivals, overbook approvals, and same-day edits.
- Require supervisor confirmation for policy overrides.
- Log override reason with compliance notes.

### 6. Reporting and performance journey
- Review reservation throughput, no-show rate, cancellation trends.
- Segment by provider, service, room, and time band.
- Export and schedule recurring operational reports.

## Cross-cutting module requirements
1. One source-of-truth reservation read model for dashboard, list, and calendar.
2. Clear separation: read queries vs state-changing commands.
3. Input validation and safe failure messages for every command.
4. Extensible hooks for plugins (custom alerts, reminders, and reports).
5. Simple UX: consistent terminology, predictable status colors, minimal clicks.

