import { useMutation, useQueryClient } from "@tanstack/react-query";
import { seaProjectKeys } from "./seaProjectKeys";
import { markSeaProjectRead } from "../api/seaProjectsApi";

export function useMarkSeaProjectRead() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => markSeaProjectRead(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: seaProjectKeys.all }),
  });
}
