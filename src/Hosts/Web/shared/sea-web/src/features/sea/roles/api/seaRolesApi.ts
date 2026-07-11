import {
  seaAssignEventRole,
  seaAssignUserRole,
  seaCreateRole,
  seaDeleteRole,
  seaListEventReadRoles,
  seaListEventRoles,
  seaListEventWriteRoles,
  seaListRoles,
  seaListUserRoles,
  seaRemoveEventRole,
  seaRemoveUserRole,
  seaUpdateRole,
} from "../../../../api/generated/sea/sea";
import type {
  RoleType,
  SeaCommandResult,
} from "../../../../api/generated/sea/model";
import {
  mapSeaRole,
  mapSeaUserRole,
} from "../../shared/models/seaReadDtoMappers";
import type {
  SeaRoleReadDto,
  SeaUserRoleReadDto,
} from "../../shared/models/seaStatModels";

export type { SeaRoleReadDto, SeaUserRoleReadDto };

export interface SeaRoleWriteDto {
  name: string;
  id?: string;
}

export interface SeaUserRoleWriteDto {
  roleId: string;
  type: RoleType;
  id?: string;
}

export interface SeaEventRoleWriteDto {
  eventId: string;
  roleId: string;
  type: RoleType;
  id?: string;
}

export async function listSeaRoles(): Promise<SeaRoleReadDto[]> {
  const roles = await seaListRoles();
  return roles.map(mapSeaRole);
}

export async function createSeaRole(data: SeaRoleWriteDto): Promise<SeaCommandResult> {
  return seaCreateRole(data);
}

export async function updateSeaRole(data: SeaRoleWriteDto): Promise<SeaCommandResult> {
  return seaUpdateRole(data);
}

export async function deleteSeaRole(id: string): Promise<SeaCommandResult> {
  return seaDeleteRole(id);
}

export async function listSeaUserRoles(userId: string): Promise<SeaUserRoleReadDto[]> {
  const userRoles = await seaListUserRoles(userId);
  return userRoles.map(mapSeaUserRole);
}

export async function assignSeaUserRole(data: SeaUserRoleWriteDto): Promise<SeaCommandResult> {
  return seaAssignUserRole(data);
}

export async function removeSeaUserRole(id: string): Promise<SeaCommandResult> {
  return seaRemoveUserRole(id);
}

export async function listSeaEventRoles(eventId: string): Promise<string[]> {
  return seaListEventRoles(eventId);
}

export async function listSeaEventReadRoles(eventId: string): Promise<string[]> {
  return seaListEventReadRoles(eventId);
}

export async function listSeaEventWriteRoles(eventId: string): Promise<string[]> {
  return seaListEventWriteRoles(eventId);
}

export async function assignSeaEventRole(data: SeaEventRoleWriteDto): Promise<SeaCommandResult> {
  return seaAssignEventRole(data);
}

export async function removeSeaEventRole(id: string): Promise<SeaCommandResult> {
  return seaRemoveEventRole(id);
}
