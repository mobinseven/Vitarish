export const patientProcedureKeys = {
  all: ["clinic", "patientProcedures"] as const,
  list: (patientId: string) => [...patientProcedureKeys.all, "list", patientId] as const,
  detail: (procedureId: string) => [...patientProcedureKeys.all, "detail", procedureId] as const,
  events: (procedureId: string) => [...patientProcedureKeys.all, "events", procedureId] as const,
  eventDetail: (eventId: string) => [...patientProcedureKeys.all, "eventDetail", eventId] as const,
  eventItems: (eventId: string) => [...patientProcedureKeys.all, "eventItems", eventId] as const,
  access: (eventId: string) => [...patientProcedureKeys.all, "access", eventId] as const,
  templates: () => [...patientProcedureKeys.all, "templates"] as const,
  templateProcedures: (projectId: string) => [...patientProcedureKeys.all, "templateProcedures", projectId] as const,
  triage: (patientId: string) => [...patientProcedureKeys.all, "triage", patientId] as const,
};
