import { useQuery } from "@tanstack/react-query";

export function useClinicSettingsUsers() {
  return useQuery({
    queryKey: ["clinic", "settings", "users"],
    queryFn: async () => {
      // Placeholder: requires tenant users list endpoint
      return [];
    },
  });
}
