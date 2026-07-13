import { useCallback } from "react";

export function usePatientUpdate() {
  const update = useCallback(async () => {
    throw new Error(
      "Patient update is not yet available. Backend endpoint required."
    );
  }, []);

  return { update, isPending: false, error: null as Error | null };
}
