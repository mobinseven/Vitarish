export const seaRoleKeys = {
  all: ["sea", "roles"] as const,
  list: () => [...seaRoleKeys.all, "list"] as const,
  userRoles: (userId: string) => [...seaRoleKeys.all, "userRoles", userId] as const,
  eventRoles: (eventId: string) => [...seaRoleKeys.all, "eventRoles", eventId] as const,
  eventReadRoles: (eventId: string) => [...seaRoleKeys.all, "eventReadRoles", eventId] as const,
  eventWriteRoles: (eventId: string) => [...seaRoleKeys.all, "eventWriteRoles", eventId] as const,
};
