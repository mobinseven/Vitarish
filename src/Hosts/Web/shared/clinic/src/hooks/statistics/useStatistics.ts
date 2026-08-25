import { useQuery } from "@tanstack/react-query";

export function useStatistics() {
  return useQuery({
    queryKey: ["clinic", "statistics"],
    queryFn: async () => {
      // Placeholder: requires dedicated statistics read model/API endpoint
      // Do not aggregate from multiple list endpoints in UI
      return null;
    },
  });
}
