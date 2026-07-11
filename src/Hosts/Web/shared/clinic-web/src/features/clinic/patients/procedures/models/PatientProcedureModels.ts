export interface PatientProcedureSummary {
  id: string;
  projectId: string;
  name: string;
  description?: string;
  eventCount: number;
  committedEventCount: number;
  eventItemCount: number;
  newItems: number;
  lastActivity?: string;
  createdAt: string;
}

export interface PatientProcedureDetail {
  id: string;
  projectId: string;
  name: string;
  description?: string;
  eventCount: number;
  committedEventCount: number;
  eventItemCount: number;
  newItems: number;
  lastActivity?: string;
  createdAt: string;
  createdBy?: string;
  updatedAt?: string;
}
