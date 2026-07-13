export const seaTemplateKeys = {
  all: ["sea", "templates"] as const,
  list: () => [...seaTemplateKeys.all, "list"] as const,
  detail: (id: string) => [...seaTemplateKeys.all, "detail", id] as const,
};
