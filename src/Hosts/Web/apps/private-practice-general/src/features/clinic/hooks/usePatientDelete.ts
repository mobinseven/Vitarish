import { useCallback } from "react";

export function usePatientDelete() {
  const remove = useCallback(async () => {
    throw new Error(
      "Patient deletion is not yet available. Backend endpoint required."
    );
  }, []);

  return { remove, isPending: false };
}
