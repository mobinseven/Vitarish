import type { PatientProcedureSummary, PatientProcedureDetail } from "../../../models/patients/procedures/PatientProcedureModels";

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function mapToPatientProcedureSummary(dto: any): PatientProcedureSummary {
  return {
    id: dto.id,
    projectId: dto.projectId,
    name: dto.name,
    description: dto.description ?? undefined,
    eventCount: dto.stats?.events ?? 0,
    committedEventCount: dto.stats?.committedEvents ?? 0,
    eventItemCount: dto.stats?.eventItems ?? 0,
    newItems: dto.stats?.newItems ?? 0,
    lastActivity: dto.stats?.lastUpdate,
    createdAt: dto.createdAt,
  };
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function mapToPatientProcedureDetail(dto: any): PatientProcedureDetail {
  return {
    ...mapToPatientProcedureSummary(dto),
    createdBy: dto.createdBy,
    updatedAt: dto.updatedAt,
  };
}
