import { useQuery } from "@tanstack/react-query";
import { seaProcedureKeys } from "./seaProcedureKeys";
import { getSeaProcedure } from "../api/seaProceduresApi";

export function useSeaProcedureDetail(procedureId: string) {
  return useQuery({
    queryKey: seaProcedureKeys.detail(procedureId),
    queryFn: () => getSeaProcedure(procedureId),
    enabled: !!procedureId,
  });
}
