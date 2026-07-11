import { useMemo } from "react";

export interface JourneyWorkspaceOption {
  id: string;
  name: string;
  description: string;
}

export function useJourneyWorkspaceOptions() {
  const options = useMemo<JourneyWorkspaceOption[]>(() => [], []);

  return {
    options,
    isLoading: false,
    unavailableReason: "Workspace selection is not available yet because no workspace options API is connected.",
  };
}
