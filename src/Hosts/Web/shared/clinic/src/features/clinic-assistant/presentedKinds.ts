/**
 * The record kinds Clinic assistant surfaces show. The values are owned by the Clinic presentations on the server
 * (`ClinicPresentedRecordKinds`); this mirror is the web's single copy.
 */
export const clinicPresentedKinds = {
  patientCandidate: "clinic.patient-candidate",
  patientProfile: "clinic.patient-profile",
  appointment: "clinic.appointment",
  appointmentSlot: "clinic.appointment-slot",
  appointmentCount: "clinic.appointment-count",
  shift: "clinic.shift",
  shiftOccurrence: "clinic.shift-occurrence",
  reminder: "clinic.reminder",
  prescription: "clinic.prescription",
  procedure: "clinic.procedure",
  procedureEvent: "clinic.procedure-event",
  eventAttachment: "clinic.event-attachment",
  surgery: "clinic.surgery",
  surgeryConflict: "clinic.surgery-conflict",
  staffMember: "clinic.staff-member",
  setupGoal: "clinic.setup-goal",
} as const;

/** Font Awesome icon per record kind, without the `fa-` prefix. */
export const clinicPresentedKindIcons: Record<string, string> = {
  [clinicPresentedKinds.patientCandidate]: "user",
  [clinicPresentedKinds.patientProfile]: "id-card",
  [clinicPresentedKinds.appointment]: "calendar-check",
  [clinicPresentedKinds.appointmentSlot]: "clock",
  [clinicPresentedKinds.appointmentCount]: "calendar-days",
  [clinicPresentedKinds.shift]: "calendar-week",
  [clinicPresentedKinds.shiftOccurrence]: "calendar-day",
  [clinicPresentedKinds.reminder]: "bell",
  [clinicPresentedKinds.prescription]: "prescription",
  [clinicPresentedKinds.procedure]: "list-check",
  [clinicPresentedKinds.procedureEvent]: "notes-medical",
  [clinicPresentedKinds.eventAttachment]: "paperclip",
  [clinicPresentedKinds.surgery]: "user-doctor",
  [clinicPresentedKinds.surgeryConflict]: "triangle-exclamation",
  [clinicPresentedKinds.staffMember]: "user-tie",
  [clinicPresentedKinds.setupGoal]: "circle-check",
};
