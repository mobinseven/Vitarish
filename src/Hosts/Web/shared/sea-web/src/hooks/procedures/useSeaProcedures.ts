import { useQuery } from "@tanstack/react-query";
import { seaProcedureKeys } from "./seaProcedureKeys";
import { listSeaProcedures } from "../../api/procedures/seaProceduresApi";

export function useSeaProcedures(projectId: string) {
  return useQuery({
    queryKey: seaProcedureKeys.list(projectId),
    queryFn: () => listSeaProcedures(projectId),
    enabled: !!projectId,
  });
}
