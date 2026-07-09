import { useQuery } from "@tanstack/react-query";
import { seaProjectKeys } from "./seaProjectKeys";
import { listSeaProjects } from "../api/seaProjectsApi";

export function useSeaProjects() {
  return useQuery({
    queryKey: seaProjectKeys.list(),
    queryFn: listSeaProjects,
  });
}
