export const seaAttachmentKeys = {
  all: ["sea", "attachments"] as const,
  list: (eventId: string) => [...seaAttachmentKeys.all, "list", eventId] as const,
  count: (eventId: string) => [...seaAttachmentKeys.all, "count", eventId] as const,
};
