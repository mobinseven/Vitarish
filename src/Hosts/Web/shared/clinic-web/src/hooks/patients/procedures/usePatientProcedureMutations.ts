import { useMutation, useQueryClient } from "@tanstack/react-query";
import { patientProcedureKeys } from "./patientProcedureKeys";
import {
  createEvent,
  createProcedure,
  deleteProcedure,
  listProcedureEvents,
  updateProcedure,
} from "@vitarish/sea-web";

export function useCreatePatientProcedure() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (data: {
      projectId?: string;
      name?: string;
      description?: string;
      id?: string;
      templateProcedureId?: string;
    }) => {
      const templateEvents = data.templateProcedureId
        ? await listProcedureEvents(data.templateProcedureId)
        : [];
      const result = await createProcedure(data);

      if (!result.entityId) {
        throw new Error("The created procedure did not return an entity id.");
      }

      let previousEventId: string | undefined;
      for (const event of templateEvents) {
        const eventResult = await createEvent({
          projectId: data.projectId,
          procedureId: result.entityId,
          title: event.title,
          description: event.description,
          previousEventId,
        });

        if (!eventResult.entityId) {
          throw new Error("A copied procedure event did not return an entity id.");
        }

        previousEventId = eventResult.entityId;
      }

      return result;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: patientProcedureKeys.all }),
  });
}

export function useUpdatePatientProcedure() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: { projectId?: string; name?: string; description?: string; id?: string }) => updateProcedure(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: patientProcedureKeys.all }),
  });
}

export function useDeletePatientProcedure() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteProcedure(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: patientProcedureKeys.all }),
  });
}
