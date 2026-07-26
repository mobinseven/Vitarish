export type NavigationIntent =
  | { kind: "dashboard" }
  | { kind: "ownerShiftIndex" }
  | { kind: "ownerShiftCreate" }
  | { kind: "ownerShiftDetail"; shiftId: string }
  | { kind: "ownerShiftEdit"; shiftId: string }
  | { kind: "ownerShiftOccurrence"; shiftId: string; occurrenceDate: string }
  | { kind: "ownerReservationIndex" }
  | { kind: "ownerReservationDetail"; reservationId: string }
  | { kind: "userShiftIndex" }
  | { kind: "userShiftDetail"; shiftId: string }
  | { kind: "userReservationIndex" }
  | { kind: "userReservationDetail"; reservationId: string }
  | { kind: "patientDetail"; patientId: string }
  | { kind: "patientNew" }
  | { kind: "patientEdit"; patientId: string }
  | { kind: "patientIndex" }
  | { kind: "patientProcedureDetail"; patientId: string; procedureId: string }
  | { kind: "patientProcedureEventDetail"; patientId: string; procedureId: string; eventId: string }
  | { kind: "checkIn" }
  | { kind: "appointmentSettings" }
  | { kind: "clinicSettings" }
  | { kind: "clinicSettingsUsers" }
  | { kind: "clinicStatistics" }
  | { kind: "procedureTemplateManagement" }
  | { kind: "prescriptionTemplateManagement" }
  | { kind: "reminders" }
  | { kind: "serviceIndex" }
  | { kind: "serviceDetail"; serviceId: string }
  | { kind: "serviceTypeIndex" }
  | { kind: "serviceTypeDetail"; serviceTypeId: string }
  | { kind: "serviceProviderIndex" }
  | { kind: "serviceProviderDetail"; serviceProviderId: string };
