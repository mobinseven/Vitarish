import { apiFetch } from "@/api/client/apiFetch";

const BASE = "/api/modules/sea";

export interface ProcedureReadDto {
  id: string;
  projectId: string;
  name: string;
  description?: string;
  stats?: { state?: string; procedures: number; events: number; committedEvents: number; eventItems: number; newItems: number; unread: boolean; lastUpdate?: string };
  createdAt: string;
  createdBy?: string;
  updatedAt?: string;
}

export interface EventReadDto {
  id: string;
  projectId: string;
  procedureId: string;
  previousEventId?: string;
  committedAt?: string;
  committedBy: string;
  title: string;
  description: string;
  isCommitted: boolean;
  stats?: { state?: string; procedures: number; events: number; committedEvents: number; eventItems: number; newItems: number; unread: boolean; lastUpdate?: string };
  createdAt: string;
}

export interface SeaCommandResult {
  succeeded: boolean;
  entityId?: string;
}

export async function listProcedures(projectId: string): Promise<ProcedureReadDto[]> {
  return apiFetch<ProcedureReadDto[]>({
    url: `${BASE}/projects/${projectId}/procedures`,
    method: "GET",
  });
}

export async function getProcedure(id: string): Promise<ProcedureReadDto> {
  return apiFetch<ProcedureReadDto>({
    url: `${BASE}/procedures/${id}`,
    method: "GET",
  });
}

export async function listProcedureEvents(procedureId: string): Promise<EventReadDto[]> {
  return apiFetch<EventReadDto[]>({
    url: `${BASE}/procedures/${procedureId}/events`,
    method: "GET",
  });
}

export async function getEvent(id: string): Promise<EventReadDto> {
  return apiFetch<EventReadDto>({
    url: `${BASE}/events/${id}`,
    method: "GET",
  });
}

export async function deleteEvent(id: string): Promise<SeaCommandResult> {
  return apiFetch<SeaCommandResult>({
    url: `${BASE}/events/${id}`,
    method: "DELETE",
  });
}
