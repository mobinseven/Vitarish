import {
  seaCreateProject,
  seaDeleteProject,
  seaGetProject,
  seaListProjects,
  seaMarkProjectRead,
  seaMarkProjectUnread,
  seaUpdateProject,
} from "../generated/sea/sea";
import type { SeaCommandResult } from "../generated/sea/model";
import { mapSeaProject } from "../../mappers/seaReadDtoMappers";
import type { SeaProjectReadDto } from "../../models/seaStatModels";

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
  const projects = await seaListProjects();
  return projects.map(mapSeaProject);
}

export async function getSeaProject(id: string): Promise<SeaProjectReadDto> {
  return mapSeaProject(await seaGetProject(id));
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
