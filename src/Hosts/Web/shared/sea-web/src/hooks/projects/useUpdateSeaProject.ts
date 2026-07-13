import { useMutation, useQueryClient } from "@tanstack/react-query";
import { seaProjectKeys } from "./seaProjectKeys";
import { updateSeaProject, type SeaProjectWriteDto } from "../../api/projects/seaProjectsApi";

export function useUpdateSeaProject() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: SeaProjectWriteDto) => updateSeaProject(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: seaProjectKeys.all }),
  });
}
