import { useMutation, useQueryClient } from "@tanstack/react-query";
import { patientProcedureKeys } from "./patientProcedureKeys";
import { createEventItemText, createEventItemFile, deleteEventItem } from "@vitarish/sea-web";
import { toSeaFileAttachment } from "@vitarish/sea-web";

export function useCreatePatientProcedureEventItemText() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: { eventId: string; content: string; contentType: string; size: number }) =>
      createEventItemText(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: patientProcedureKeys.all }),
  });
}

export function useCreatePatientProcedureEventItemFile() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ eventId, file }: { eventId: string; file: File }) => {
      const attachment = await toSeaFileAttachment(eventId, file);
      return createEventItemFile({ ...attachment, fileName: file.name });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: patientProcedureKeys.all }),
  });
}

export function useDeletePatientProcedureEventItem() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteEventItem(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: patientProcedureKeys.all }),
  });
}
