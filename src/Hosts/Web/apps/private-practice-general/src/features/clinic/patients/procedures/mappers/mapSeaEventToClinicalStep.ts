import type { ClinicalEventSummary, ClinicalEventDetail } from "../models/PatientProcedureEventModels";

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function mapToClinicalEventSummary(dto: any, position: number): ClinicalEventSummary {
  return {
    id: dto.id,
    procedureId: dto.procedureId,
    projectId: dto.projectId,
    title: dto.title,
    description: dto.description ?? undefined,
    isCommitted: dto.isCommitted,
    committedAt: dto.committedAt ?? undefined,
    committedBy: dto.committedBy,
    position,
    createdAt: dto.createdAt,
  };
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function mapToClinicalEventDetail(dto: any, position: number): ClinicalEventDetail {
  return {
    ...mapToClinicalEventSummary(dto, position),
  };
}
