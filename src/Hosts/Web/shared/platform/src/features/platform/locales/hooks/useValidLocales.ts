import { useQuery } from "@tanstack/react-query";

export interface ValidLocale {
  normalizedLocale: string;
  locale: string;
}

export const validLocaleKeys = {
  all: ["platform", "locales"] as const,
};

// Placeholder hook - replace with generated hook after Orval generation
export function useValidLocales() {
  // TODO: Replace with generated hook from Platform API
  return useQuery({
    queryKey: validLocaleKeys.all,
    queryFn: async (): Promise<ValidLocale[]> => {
      // Placeholder - will be replaced by generated client
      return [
        { normalizedLocale: "en-US", locale: "en-US" },
        { normalizedLocale: "fa-IR", locale: "fa-IR" },
      ];
    },
  });
}
