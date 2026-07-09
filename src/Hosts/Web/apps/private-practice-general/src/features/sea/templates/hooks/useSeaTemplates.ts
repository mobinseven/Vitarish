import { useQuery } from "@tanstack/react-query";
import { seaTemplateKeys } from "./seaTemplateKeys";
import { listSeaTemplateProjects, getSeaTemplateProject } from "../api/seaTemplatesApi";

export function useSeaTemplateProjects() {
  return useQuery({
    queryKey: seaTemplateKeys.list(),
    queryFn: listSeaTemplateProjects,
  });
}

export function useSeaTemplateProjectDetail(projectId: string) {
  return useQuery({
    queryKey: seaTemplateKeys.detail(projectId),
    queryFn: () => getSeaTemplateProject(projectId),
    enabled: !!projectId,
  });
}
