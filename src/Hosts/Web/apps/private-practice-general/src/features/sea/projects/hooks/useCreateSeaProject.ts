import { useMutation, useQueryClient } from "@tanstack/react-query";
import { seaProjectKeys } from "./seaProjectKeys";
import { createSeaProject, type SeaProjectWriteDto } from "../api/seaProjectsApi";

export function useCreateSeaProject() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: SeaProjectWriteDto) => createSeaProject(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: seaProjectKeys.all }),
  });
}
