export const seaProcedureKeys = {
  all: ["sea", "procedures"] as const,
  list: (projectId: string) => [...seaProcedureKeys.all, "list", projectId] as const,
  detail: (id: string) => [...seaProcedureKeys.all, "detail", id] as const,
  events: (procedureId: string) => [...seaProcedureKeys.all, "events", procedureId] as const,
};
