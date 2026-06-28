# Reminder Management Journey

## 1. Context

- App: Clinic (primary consumer), any app using Communications module
- Module: Communications
- Capability: Reminder creation, scheduling, delivery, action handling
- Related apps: Clinic (patient reminders), Appointments (shift reminders)
- Related modules: Users (reminder recipients), Parties (tenant context), Sea (event reminders)
- Product / service: Vitarish Communications module
- Journey type: Current state with gaps
- Scenario: Owner creates reminders for users; users view and act on reminders; system delivers reminders via email or SMS.
- User goal: Create, schedule, and manage reminders that keep users informed about important events.
- Business goal: Reduce missed appointments, events, and deadlines through timely notifications.
- Scope start: Owner opens the reminders page or creates a reminder from a context.
- Scope end: Reminder is created, scheduled, delivered, and acted upon.
- Primary persona: Owner (reminder creator) and User (reminder recipient)
- Evidence sources: Static code review of Communications Interface components, host pages, reminder scheduling logic
- Confidence level: Medium

## 2. Persona Snapshot

**Name / segment:** Reminder creator (owner/admin)

**Situation:** Managing follow-ups, appointments, and deadlines for users.

**Motivation:** Ensure users don't miss important events through timely reminders.

**Key needs:** Create reminders with schedule, channel, and recipient; view reminder status; manage recurring patterns.

**Constraints:** No recurring reminder patterns; no channel preferences per user; no delivery confirmation.

**Success means:** Reminders are created easily and delivered reliably.

---

**Name / segment:** Reminder recipient (user)

**Situation:** Receiving reminders about upcoming events, tasks, or deadlines.

**Motivation:** Stay informed and take timely action.

**Key needs:** View upcoming reminders; act on reminders; dismiss or snooze.

**Constraints:** No snooze functionality; no reminder priority levels; no grouping.

**Success means:** Reminders arrive on time and are easy to act on.

## 3. Journey Summary

**Narrative:** The owner opens the reminders page, creates a reminder with a schedule and channel (email/SMS), selects recipients, and saves. The system delivers the reminder at the scheduled time. The user receives the reminder, views details, and takes action (e.g., confirms an appointment). The owner monitors reminder delivery and manages active reminders.

**Emotional arc (creator):** Start: purposeful (creating reminder) → Middle: attentive (scheduling) → End: confident (reminder saved)

**Emotional arc (recipient):** Start: neutral (receiving reminder) → Middle: focused (reading details) → End: relieved (action taken)

**Biggest friction point:** No delivery confirmation; creator cannot verify if reminder was received.

**Highest-value opportunity:** Add delivery confirmation, recurring patterns, and channel preferences.

## 4. Journey Stages

### Stage 1: Reminder Creation

**User goal:** Create a new reminder for a user or group.

**User actions:** Open reminders page; click "Create Reminder"; enter title, description; select recipients via address book; choose channel (email/SMS); set schedule; save.

**Touchpoints / channels:** `RemindersGrid`, `ReminderCreatorDialog`, `AddressBookDialog`, `ReminderSchedulePicker`.

**User thoughts / questions:** "Who should receive this? When should it go out? Which channel is best?"

**Emotions:** Purposeful, 3, because the creation flow is structured.

**Pain points:** No recurring schedule option; no template-based creation; no bulk recipient selection from the address book.

**Needs:** Recurring schedules, templates, bulk selection.

**Opportunities:** Add recurring reminder patterns and template library.

**Success metrics:** Reminder creation completion rate; time to create reminder.

**Evidence:** Static code review assumption.

### Stage 2: Schedule Configuration

**User goal:** Set when and how the reminder is delivered.

**User actions:** Select delivery date/time; choose channel; set offset (e.g., "1 day before"); enable/disable.

**Touchpoints / channels:** `ReminderSchedulePicker`.

**User thoughts / questions:** "Is this schedule correct? Will it arrive on time?"

**Emotions:** Careful, 4, because timing matters.

**Pain points:** No timezone awareness; no relative scheduling (e.g., "2 hours before appointment"); no preview of delivery time.

**Needs:** Timezone support, relative scheduling, delivery preview.

**Opportunities:** Add timezone-aware scheduling and relative time offsets.

**Success metrics:** Schedule accuracy; on-time delivery rate.

**Evidence:** Static code review assumption.

### Stage 3: Recipient Selection

**User goal:** Choose who receives the reminder.

**User actions:** Open address book; search for users; select recipients; confirm selection.

**Touchpoints / channels:** `AddressBookDialog`.

**User thoughts / questions:** "Are these the right people? Can I select a group?"

**Emotions:** Neutral, 3, because selection is straightforward.

**Pain points:** No group/role-based selection; no recent recipients list; no recipient preview with contact info.

**Needs:** Group selection, recent recipients, contact info preview.

**Opportunities:** Add role-based recipient selection and recent recipients.

**Success metrics:** Recipient selection accuracy; wrong-recipient incidents.

**Evidence:** Static code review assumption.

### Stage 4: Reminder Delivery

**User goal:** System delivers the reminder at the scheduled time.

**User actions:** (System-level) System sends reminder via email or SMS at scheduled time.

**Touchpoints / channels:** Email, SMS (external channels).

**User thoughts / questions:** (Creator) "Was it delivered?" (Recipient) "Did I receive it?"

**Emotions:** (Creator) Anxious, 2, because delivery is not confirmed. (Recipient) Neutral, 3, on receipt.

**Pain points:** No delivery confirmation; no delivery status tracking; no retry on failure.

**Needs:** Delivery confirmation, status tracking, retry logic.

**Opportunities:** Add delivery receipt tracking and failure retry.

**Success metrics:** Delivery success rate; delivery latency.

**Evidence:** Static code review assumption.

### Stage 5: Reminder Viewing

**User goal:** View and understand received reminders.

**User actions:** Open reminder notification; read title, description, schedule; view details.

**Touchpoints / channels:** `ReminderDisplay`, `ReminderDetailsDialog`.

**User thoughts / questions:** "What is this reminder about? When is the event? What should I do?"

**Emotions:** Focused, 3, because the reminder is relevant.

**Pain points:** No priority indicator; no grouping of related reminders; no calendar integration view.

**Needs:** Priority levels, grouping, calendar view.

**Opportunities:** Add priority badges and reminder grouping by context.

**Success metrics:** Reminder open rate; time-to-action after viewing.

**Evidence:** Static code review assumption.

### Stage 6: Reminder Action

**User goal:** Act on the reminder (confirm, dismiss, or follow up).

**User actions:** Click action button on reminder; confirm attendance; dismiss; add note.

**Touchpoints / channels:** `ReminderAction`.

**User thoughts / questions:** "What action should I take? Can I snooze?"

**Emotions:** Satisfied, 4, if action is quick; Frustrated, 2, if no relevant action.

**Pain points:** No snooze functionality; no dismiss-and-archive; limited action types.

**Needs:** Snooze, dismiss, dismiss-and-archive, custom actions.

**Opportunities:** Add snooze with reschedule and custom action types.

**Success metrics:** Action completion rate; reminder follow-through.

**Evidence:** Static code review assumption.

### Stage 7: Reminder Monitoring

**User goal:** Monitor reminder delivery and effectiveness.

**User actions:** View reminder list; check delivery status; manage active reminders.

**Touchpoints / channels:** `RemindersGrid`, `ReminderDetailsDialog`.

**User thoughts / questions:** "Are reminders being delivered? Which ones need attention?"

**Emotions:** Systematic, 3, because monitoring is operational.

**Pain points:** No delivery status in the list; no effectiveness metrics; no overdue reminder alerts.

**Needs:** Delivery status display, effectiveness metrics, overdue alerts.

**Opportunities:** Add delivery status badges and effectiveness dashboard.

**Success metrics:** Reminder monitoring frequency; overdue resolution time.

**Evidence:** Static code review assumption.

### Stage 8: Reminder Testing (Admin)

**User goal:** Test reminder delivery and messaging.

**User actions:** Use admin testing panel to send test reminders; verify delivery.

**Touchpoints / channels:** `CommunicationsReminderTestingPanel`, `CommunicationsMessageTestingPanel`.

**User thoughts / questions:** "Does the reminder look correct? Does it arrive on time?"

**Emotions:** Methodical, 3, because testing is systematic.

**Pain points:** Testing is admin-only; no user-facing preview; no delivery simulation.

**Needs:** User-facing preview, delivery simulation, template testing.

**Opportunities:** Add reminder preview and delivery simulation.

**Success metrics:** Test-to-production consistency.

**Evidence:** Static code review assumption.

## 5. Cross-Journey Insights

### Key pain points

1. No delivery confirmation — occurs at delivery stage.
2. No recurring reminder patterns — occurs at creation stage.
3. No snooze or dismiss-and-archive — occurs at action stage.

### Moments that matter

- **Moment of truth:** Reminder arrives on time and the user takes action.
- **Moment of friction:** Creator has no way to verify delivery.
- **Moment of delight:** Reminder action completes the expected follow-through.

### Design principles implied by the journey

- Confirm delivery for every reminder.
- Support recurring patterns for repeated events.
- Make actions quick and context-appropriate.

## 6. Implementation Derivation

### Required artifacts

| Artifact | Why it is needed | Journey source | Priority |
|---|---|---|---|
| Delivery confirmation | Creator can verify reminder was received | Stage 4 | High |
| Recurring reminder patterns | Reduces manual creation for repeated events | Stage 1 | High |
| Snooze functionality | User can defer reminders to a later time | Stage 6 | Medium |
| Delivery status display | Creator sees delivery state in the reminder list | Stage 7 | Medium |

### Feature slices

| Slice | User value | Includes | Excludes | Acceptance signal |
|---|---|---|---|---|
| Delivery confirmation | Creator knows reminder was received | Delivery status tracking, status badge in list | Real-time push | Creator confidence in delivery increases |
| Recurring reminders | Same reminder sent on schedule | Daily/weekly/monthly patterns, end date, pattern editor | Complex conditional logic | Manual reminder creation decreases |
| Snooze | User defers reminder to later | Snooze duration picker, reschedule, snooze history | Custom snooze rules | Reminder follow-through increases |

### Domain / data implications

- Add delivery status entity (sent, delivered, failed, bounced).
- Add recurring pattern model (frequency, interval, end date).
- Add snooze state (snoozed until, snooze count).

### UI implications

- Add delivery status badge to `RemindersGrid`.
- Add recurring schedule section to `ReminderSchedulePicker`.
- Add snooze button to `ReminderAction`.

### API / service implications

- Add delivery status write from email/SMS providers.
- Add recurring pattern evaluation to scheduler.
- Add snooze command with reschedule.

### Validation, permissions, and safety

- Only owners can create reminders.
- Users can only act on their own reminders.
- Recurring patterns must have an end date to prevent infinite loops.

### Tests and acceptance criteria

- Delivery status updates after send.
- Recurring reminders fire on schedule.
- Snooze reschedules the reminder correctly.

### Metrics

- Delivery success rate.
- Reminder action completion rate.
- Recurring vs. one-time reminder ratio.

## 7. Technical Detail Addendum

### Current implementation touchpoints

- `src/Modules/Communications/Interface/Components/RemindersGrid.razor` — lists reminders with status and actions.
- `src/Modules/Communications/Interface/Components/ReminderDisplay.razor` — renders reminder content for display.
- `src/Modules/Communications/Interface/Components/ReminderDetailsDialog.razor` — detail dialog with full reminder info.
- `src/Modules/Communications/Interface/Components/ReminderCreatorDialog.razor` — create reminder form with recipients, schedule, channel.
- `src/Modules/Communications/Interface/Components/UserReminderCreatorDialog.razor` — user-facing reminder creation (simplified).
- `src/Modules/Communications/Interface/Components/ReminderAction.razor` — action button component for reminder follow-through.
- `src/Modules/Communications/Interface/Components/ReminderSchedulePicker.razor` — schedule configuration with date/time and channel selection.
- `src/Modules/Communications/Interface/Components/AddressBookDialog.razor` — recipient selection dialog with user search.
- `src/Modules/Communications/Interface/Components/Admins/CommunicationsReminderTestingPanel.razor` — admin panel for testing reminder delivery.
- `src/Modules/Communications/Interface/Components/Admins/CommunicationsMessageTestingPanel.razor` — admin panel for testing message templates.
- `src/Hosts/Main/Pages/Sea/Owners/SeaOwnerRemindersPage.razor` — host page for Sea module reminders.
- `src/Hosts/Main/Pages/Clinic/Owners/ClinicOwnerRemindersPage.razor` — host page for Clinic reminders.

### Issues to address

- `ReminderSchedulePicker` supports one-time scheduling only; no recurring pattern option (daily, weekly, monthly).
- No delivery status tracking; `RemindersGrid` shows creation status but not delivery confirmation.
- `ReminderAction` supports limited action types; no snooze, dismiss-and-archive, or custom actions.
- `AddressBookDialog` is user-search-only; no group or role-based recipient selection.
- No timezone awareness in `ReminderSchedulePicker`; reminders may deliver at unexpected local times.
- `ReminderCreatorDialog` has no template support; common reminder patterns must be recreated manually.
- `CommunicationsReminderTestingPanel` is admin-only; no user-facing preview or delivery simulation.
- No reminder effectiveness metrics (open rate, action completion rate, follow-through rate).
- `ReminderDisplay` has no priority indicator or grouping by context (e.g., all reminders for one patient).
