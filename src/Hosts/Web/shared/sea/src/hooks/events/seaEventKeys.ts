export const seaEventKeys = {
  all: ["sea", "events"] as const,
  list: (procedureId: string) => [...seaEventKeys.all, "list", procedureId] as const,
  detail: (id: string) => [...seaEventKeys.all, "detail", id] as const,
  capabilities: (id: string) => [...seaEventKeys.all, "capabilities", id] as const,
  writePermission: (id: string) => [...seaEventKeys.all, "writePermission", id] as const,
};
