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
} from "@/api/generated/sea/sea";
import type {
  RoleType,
  RoleReadDto as SeaRoleReadDto,
  SeaCommandResult,
  UserRoleReadDto as SeaUserRoleReadDto,
} from "@/api/generated/sea/model";

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
  return seaListRoles();
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
  return seaListUserRoles(userId);
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
