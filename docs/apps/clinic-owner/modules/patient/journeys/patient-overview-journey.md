# Patient Overview Journey

## 1. Context
- Product / service: Vitarish Clinic owner app
- Journey type: Future state, based on partial current state
- Scenario: Clinician opens a patient and needs a concise care snapshot.
- User goal: Understand the patient quickly and act.
- Business goal: Reduce navigation and missed context.
- Scope start: User opens a patient profile.
- Scope end: User understands status and chooses an action.
- Primary persona: Clinician / clinic admin
- Evidence sources: Static code review assumptions
- Confidence level: Medium

## 2. Persona Snapshot
**Name / segment:** Clinician

**Situation:** Preparing for or reviewing a patient interaction.

**Motivation:** See key risks, appointments, procedures, and next actions.

**Key needs:** Summary, upcoming/history, flags, timeline, quick actions.

**Constraints:** Current data is split across profile, procedures, and appointment panels.

**Success means:** Clinician can act without hunting for context.

## 3. Journey Summary
**Narrative:** The clinician opens the patient and sees a compact summary, critical notes/flags, upcoming appointments, recent history, procedure status, timeline, and quick actions to edit, book, or add a procedure.

**Emotional arc:** Start: curious → Middle: oriented → End: confident

**Biggest friction point:** No consolidated timeline or flag area.

**Highest-value opportunity:** Create one patient overview source of truth that aggregates profile, appointments, procedures, and flags.

## 4. Journey Stages

### Stage 1: Orient
**User goal:** Understand the patient at a glance.

**User actions:** Open patient; scan summary, flags, upcoming items.

**Touchpoints / channels:** Patient overview panel.

**User thoughts / questions:** “What matters before I act?”

**Emotions:** Alert, 3, because medical context can be critical.

**Pain points:** Important data is not prioritized.

**Needs:** Flags, status, recent changes.

**Opportunities:** Add a top-of-page overview card.

**Success metrics:** Time to first action; missed-flag incidents; page bounce/navigation count.

**Evidence:** Static code review assumption.

### Stage 2: Act
**User goal:** Take the next clinical/admin action.

**User actions:** Edit patient; book/follow-up; add procedure; open history.

**Touchpoints / channels:** Quick actions and timeline.

**User thoughts / questions:** “What is the next best action?”

**Emotions:** Confident, 4, if next steps are obvious.

**Pain points:** Upcoming/history are not separated.

**Needs:** Clear quick actions and chronological activity.

**Opportunities:** Add unified timeline and action menu.

**Success metrics:** Action completion; extra clicks; support requests.

**Evidence:** Static code review assumption.

## 5. Cross-Journey Insights

### Key pain points
1. Data fragmentation — occurs on profile review.
2. Missing flags — occurs during clinical risk scanning.
3. History/upcoming not separated — occurs during planning.

### Moments that matter
- **Moment of truth:** Clinician decides what to do next.
- **Moment of friction:** User switches between panels to reconstruct history.
- **Moment of delight:** One screen answers “what matters now?”

### Design principles implied by the journey
- Prioritize safety-critical information.
- Separate upcoming, historical, and current-state data.
- Put quick actions next to the context that triggers them.

## 6. Technical Detail Addendum

### Current implementation touchpoints
- `ClinicOwnerPatientPage.razor` is the current de facto patient overview: it stacks `PatientDetails`, `ProceduresGrid`, `PatientAppointmentsPanel`, and any `IMedicalPatientSectionPlugin` sections.
- `PatientOverviewPanel` and `PatientSummaryCard` exist as compact read components, but `PatientPage.razor` does not currently render `PatientOverviewPanel` as the primary patient overview surface.
- `PatientDetails` displays profile description, clinical notes, surgery date, gender, date of birth/age, Sea stats, and linked user name.
- `ProceduresGrid` contributes procedure-level counts and navigation, while `PatientAppointmentsPanel` contributes reservation status badges and booking entry points.
- `TodayAppointmentsWidget` provides a dashboard-level appointment snapshot, but it is session/day scoped rather than patient-overview scoped.

### Issues to address
- There is no single `PatientOverviewViewModel` or aggregator service. Profile metadata, procedures, stats, reservations, plugins, and future flags are assembled directly by page/component composition.
- `PatientOverviewPanel` duplicates parts of `PatientDetails` but does not own upcoming appointments, procedure status, flags, or recent activity, so it cannot yet serve as the overview source of truth.
- Upcoming and historical appointments are not separated in `PatientAppointmentsPanel`; add derived read models such as `UpcomingAppointments`, `AppointmentHistory`, and `NextAppointment`.
- Procedure status, patient status, risk flags, allergies, and important notes are not prioritized at the top of `PatientPage.razor`; add a safety-first summary region before detailed lists.
- Cross-module data should be read through an overview query/adapter instead of making the page understand Sea and Appointments internals. Keep update actions in their owning components (`PatientEditForm`, `ProcedureDetails`, `ReservationDetail`) and let the overview perform read-only aggregation.
- Plugin sections are extensible but not ordered by clinical priority. Define stable overview slots such as flags, summary facts, next actions, timeline, and module extensions.
