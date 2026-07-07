import {
  seaCreateProject,
  seaDeleteProject,
  seaGetProject,
  seaListProjects,
  seaMarkProjectRead,
  seaMarkProjectUnread,
  seaUpdateProject,
} from "@/api/generated/sea/sea";
import type { SeaCommandResult } from "@/api/generated/sea/model";
import type { SeaProjectReadDto } from "../../shared/models/seaStatModels";

export interface SeaProjectWriteDto {
  id?: string;
  name?: string;
  description?: string;
  identificationCode?: string;
  template: boolean;
  userIds?: string[];
  avatarFileId?: string;
}

export async function listSeaProjects(): Promise<SeaProjectReadDto[]> {
  return seaListProjects();
}

export async function getSeaProject(id: string): Promise<SeaProjectReadDto> {
  return seaGetProject(id);
}

export async function createSeaProject(data: SeaProjectWriteDto): Promise<SeaCommandResult> {
  return seaCreateProject(data);
}

export async function updateSeaProject(data: SeaProjectWriteDto): Promise<SeaCommandResult> {
  return seaUpdateProject(data);
}

export async function deleteSeaProject(id: string): Promise<SeaCommandResult> {
  return seaDeleteProject(id);
}

export async function markSeaProjectRead(id: string): Promise<SeaCommandResult> {
  return seaMarkProjectRead(id);
}

export async function markSeaProjectUnread(id: string): Promise<SeaCommandResult> {
  return seaMarkProjectUnread(id);
}
