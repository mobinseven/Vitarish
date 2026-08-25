import { useQueries, useQuery } from "@tanstack/react-query";
import { patientProcedureKeys } from "./patientProcedureKeys";
import {
  listTemplateProcedures,
  listProcedures,
} from "@vitarish/sea";
import type { SeaProcedureReadDto, SeaProjectReadDto } from "@vitarish/sea";

export interface TemplateProcedureGroup {
  project: SeaProjectReadDto;
  procedures: SeaProcedureReadDto[];
}

export function usePatientProcedureTemplates() {
  const projectsQuery = useQuery({
    queryKey: patientProcedureKeys.templates(),
    queryFn: listTemplateProcedures,
  });

  const projects = projectsQuery.data ?? [];

  const proceduresQueries = useQueries({
    queries: projects.map((project) => ({
      queryKey: patientProcedureKeys.templateProcedures(project.id),
      queryFn: () => listProcedures(project.id),
      enabled: projectsQuery.isSuccess,
    })),
  });

  const isLoading = projectsQuery.isLoading || proceduresQueries.some((q) => q.isLoading);
  const error = projectsQuery.error || proceduresQueries.find((q) => q.error)?.error;

  const groups: TemplateProcedureGroup[] = projects.map((project, index) => ({
    project,
    procedures: proceduresQueries[index]?.data ?? [],
  }));

  return {
    data: groups,
    isLoading,
    error,
  };
}
