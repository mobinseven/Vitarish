export type ClinicNavigationIntent =
  | { kind: "shiftDetail"; shiftId: string }
  | { kind: "shiftsNew" }
  | { kind: "reservationDetail"; reservationId: string }
  | { kind: "userShiftDetail"; shiftId: string }
  | { kind: "userReservationDetail"; reservationId: string }
  | { kind: "patientDetail"; patientId: string }
  | { kind: "patientNew" }
  | { kind: "patientEdit"; patientId: string }
  | { kind: "patientIndex" }
  | { kind: "patientProcedureDetail"; patientId: string; procedureId: string }
  | { kind: "patientProcedureEventDetail"; patientId: string; procedureId: string; eventId: string }
  | { kind: "checkIn" }
  | { kind: "settingsIndex" }
  | { kind: "settingsUsers" }
  | { kind: "settingsStatistics" }
  | { kind: "reminders" }
  | { kind: "serviceTypes" }
  | { kind: "services" }
  | { kind: "serviceProviders" };
