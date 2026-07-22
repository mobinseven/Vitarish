import type { EventItemSummary } from "./PatientProcedureEventItemModels";

export interface ClinicalEventSummary {
  id: string;
  procedureId: string;
  projectId: string;
  title: string;
  description?: string;
  isCommitted: boolean;
  committedAt?: string;
  committedBy?: string;
  attachments: EventItemSummary[];
  itemCount: number;
  position: number;
  createdAt: string;
}

export interface ClinicalEventDetail {
  id: string;
  procedureId: string;
  projectId: string;
  title: string;
  description?: string;
  isCommitted: boolean;
  committedAt?: string;
  committedBy?: string;
  position: number;
  createdAt: string;
  createdBy?: string;
}
