# Patient Appointment Journey

## 1. Context
- Product / service: Vitarish Medical + Appointments modules
- Journey type: Current state with gaps
- Scenario: Admin books and manages a patient reservation.
- User goal: Schedule and update patient appointments.
- Business goal: Use appointment capacity reliably.
- Scope start: Admin opens patient profile or appointment lists.
- Scope end: Appointment is booked, statused, cancelled, or viewed.
- Primary persona: Clinic scheduler
- Evidence sources: Static code review assumptions
- Confidence level: Medium

## 2. Persona Snapshot
**Name / segment:** Scheduler

**Situation:** Booking patients into available shifts.

**Motivation:** Fill schedules without conflicts.

**Key needs:** Book, cancel, status, search, reschedule.

**Constraints:** Patient must be linked to a user; provider/room data is absent.

**Success means:** The right patient has the right appointment at the right time.

## 3. Journey Summary
**Narrative:** The scheduler opens a patient, confirms the patient is linked to a user, opens booking, selects an active shift, books the next available occurrence, then manages status or cancellation from reservation details.

**Emotional arc:** Start: efficient → Middle: blocked or confident → End: relieved

**Biggest friction point:** Booking cannot proceed until the patient has a linked user account.

**Highest-value opportunity:** Add rescheduling, provider/room assignment, and follow-up booking.

## 4. Journey Stages

### Stage 1: Book
**User goal:** Create an appointment for a patient.

**User actions:** Open patient; click add appointment; select active shift.

**Touchpoints / channels:** Patient appointments panel; booking dialog.

**User thoughts / questions:** “Which appointment slot is available?”

**Emotions:** Confident, 3, if shifts exist; frustrated, 4, if user linkage is missing.

**Pain points:** No explicit date picker; first bookable occurrence is chosen.

**Needs:** Visible slot/date choice and clear capacity.

**Opportunities:** Add date selection and room/provider context.

**Success metrics:** Booking completion; booking errors; linked-user blockers.

**Evidence:** Static code review assumption.

### Stage 2: Manage
**User goal:** Update appointment state.

**User actions:** Open appointment; confirm, mark present/completed, cancel/reject.

**Touchpoints / channels:** Reservation detail dialog.

**User thoughts / questions:** “Has the patient arrived?”

**Emotions:** In control, 3, because status actions are clear.

**Pain points:** Reschedule is not exposed in UI.

**Needs:** Reschedule, follow-up, calendar/list toggle.

**Opportunities:** Add edit/reschedule and follow-up action from details.

**Success metrics:** Reschedule completion; cancellation rate; no-show workflow completion.

**Evidence:** Static code review assumption.

## 5. Cross-Journey Insights

### Key pain points
1. Linked-user dependency — occurs before booking.
2. Missing reschedule UI — occurs after booking.
3. Missing provider/room assignment — occurs during schedule design.

### Moments that matter
- **Moment of truth:** Reservation save after capacity validation.
- **Moment of friction:** Patient lacks a linked user account.
- **Moment of delight:** Status actions match appointment operations.

### Design principles implied by the journey
- Show availability before committing.
- Prevent conflicts in services, not only the UI.
- Keep follow-up close to completed/cancelled appointments.

## 6. Technical Detail Addendum

### Current implementation touchpoints
- `PatientPage.razor` renders `PatientAppointmentsPanel` with the current patient project and routes missing user-link remediation back to `PatientEditPage.razor`.
- `PatientAppointmentsPanel` reads `Patient.UserIds.FirstOrDefault()` as the patient appointment identity, loads reservations with `ReservationDescriptorDto.ReservingPartyId`, orders them by date/start time, and opens `ReservationDetailDialog` for status management.
- PatientAppointmentBookingDialog injects IAccessControlledShiftManager, IAccessControlledReservationManager, and IShiftBookableDateQueryService; it lists active shifts, reserves the first future non-cancelled bookable occurrence, and optionally creates a post-booking reminder.
- `ReservationDetail` exposes status transitions for pending, confirmed, present, and waitlisted reservations, then delegates to reservation manager methods such as confirm, present, complete, cancel, and reject.
- Global appointment lists use `ReservationsList` and `ReservationFilters`; the current filter component only exposes `ReservationStatus`.

### Issues to address
- Booking depends on `ProjectReadDto.UserIds.FirstOrDefault()` rather than a dedicated patient scheduling identity. This should be made explicit to avoid ambiguous behavior when a patient has zero or multiple linked users.
- `PatientAppointmentBookingDialog.ReserveAsync()` automatically selects the earliest bookable occurrence; add a date/time picker or occurrence list so schedulers choose a specific appointment slot.
- `PatientAppointmentsPanel` has no patient-specific filters for upcoming/history, status, shift/provider, or date range; all reservations are displayed as one chronological list.
- Rescheduling is not exposed from `ReservationDetailDialog` or `ReservationDetail`, even though appointment services may support moving reservations elsewhere. Add a UI flow that separates read-only detail from state-changing move/cancel/transition commands.
- Provider and room assignment are absent from the patient appointment flow. Model these on shifts/occurrences or reservation metadata before displaying them in the patient panel.
- Cancellation and rejection transitions do not collect a reason in the visible UI. Add reason capture for auditability and follow-up workflows.
- Follow-up creation is not linked from completed/cancelled reservation states; add a follow-up action that reuses the booking dialog with patient context and a suggested date range.
