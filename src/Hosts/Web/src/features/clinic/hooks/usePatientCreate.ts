import { useCallback } from "react";

export function usePatientCreate() {
  const create = useCallback(async () => {
    throw new Error(
      "Patient creation is not yet available. Backend endpoint required."
    );
  }, []);

  return { create, isPending: false };
}
