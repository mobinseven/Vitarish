import { useMutation, useQueryClient } from "@tanstack/react-query";
import { seaProjectKeys } from "./seaProjectKeys";
import { deleteSeaProject } from "../api/seaProjectsApi";

export function useDeleteSeaProject() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteSeaProject(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: seaProjectKeys.all }),
  });
}
