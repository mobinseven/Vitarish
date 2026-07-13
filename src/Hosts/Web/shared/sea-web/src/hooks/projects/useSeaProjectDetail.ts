import { useQuery } from "@tanstack/react-query";
import { seaProjectKeys } from "./seaProjectKeys";
import { getSeaProject } from "../../api/projects/seaProjectsApi";

export function useSeaProjectDetail(projectId: string) {
  return useQuery({
    queryKey: seaProjectKeys.detail(projectId),
    queryFn: () => getSeaProject(projectId),
    enabled: !!projectId,
  });
}
