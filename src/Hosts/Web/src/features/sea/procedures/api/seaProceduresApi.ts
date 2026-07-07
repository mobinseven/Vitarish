import {
  seaCreateProcedure,
  seaDeleteProcedure,
  seaGetProcedure,
  seaListProcedures,
  seaMarkProcedureRead,
  seaMarkProcedureUnread,
  seaUpdateProcedure,
} from "@/api/generated/sea/sea";
import type { SeaCommandResult } from "@/api/generated/sea/model";
import type { SeaProcedureReadDto } from "../../shared/models/seaStatModels";

export interface SeaProcedureWriteDto {
  projectId?: string;
  name?: string;
  description?: string;
  id?: string;
}

export async function listSeaProcedures(projectId: string): Promise<SeaProcedureReadDto[]> {
  return seaListProcedures(projectId);
}

export async function getSeaProcedure(id: string): Promise<SeaProcedureReadDto> {
  return seaGetProcedure(id);
}

export async function createSeaProcedure(data: SeaProcedureWriteDto): Promise<SeaCommandResult> {
  return seaCreateProcedure(data);
}

export async function updateSeaProcedure(data: SeaProcedureWriteDto): Promise<SeaCommandResult> {
  return seaUpdateProcedure(data);
}

export async function deleteSeaProcedure(id: string): Promise<SeaCommandResult> {
  return seaDeleteProcedure(id);
}

export async function markSeaProcedureRead(id: string): Promise<SeaCommandResult> {
  return seaMarkProcedureRead(id);
}

export async function markSeaProcedureUnread(id: string): Promise<SeaCommandResult> {
  return seaMarkProcedureUnread(id);
}
