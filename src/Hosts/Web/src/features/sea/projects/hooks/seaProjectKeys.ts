export const seaProjectKeys = {
  all: ["sea", "projects"] as const,
  list: () => [...seaProjectKeys.all, "list"] as const,
  detail: (id: string) => [...seaProjectKeys.all, "detail", id] as const,
  procedures: (projectId: string) => [...seaProjectKeys.all, "procedures", projectId] as const,
};
