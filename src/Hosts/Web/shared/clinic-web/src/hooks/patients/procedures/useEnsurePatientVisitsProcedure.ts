import { useEffect, useRef } from "react";
import { useCreatePatientProcedure } from "./usePatientProcedureMutations";
import { usePatientProcedures } from "./usePatientProcedures";

export const patientVisitsProcedureName = "Visits";

export function useEnsurePatientVisitsProcedure(patientId: string, enabled: boolean) {
  const proceduresQuery = usePatientProcedures(patientId);
  const createProcedure = useCreatePatientProcedure();
  const requestedPatientId = useRef<string | undefined>(undefined);
  const visitsProcedure = proceduresQuery.data?.find(
    ({ name }) => name === patientVisitsProcedureName,
  );

  useEffect(() => {
    if (
      !enabled ||
      !proceduresQuery.data ||
      visitsProcedure ||
      createProcedure.isPending ||
      requestedPatientId.current === patientId
    ) {
      return;
    }

    requestedPatientId.current = patientId;
    createProcedure.mutate(
      {
        projectId: patientId,
        name: patientVisitsProcedureName,
        description: "",
      },
      {
        onError: () => {
          requestedPatientId.current = undefined;
        },
      },
    );
  }, [
    createProcedure,
    enabled,
    patientId,
    proceduresQuery.data,
    visitsProcedure,
  ]);

  return {
    visitsProcedure,
    isPending: proceduresQuery.isLoading || createProcedure.isPending,
    error: proceduresQuery.error ?? createProcedure.error,
  };
}
