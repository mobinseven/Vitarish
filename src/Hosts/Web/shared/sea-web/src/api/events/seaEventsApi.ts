import {
  seaCommitEvent,
  seaCreateEventAfter,
  seaCreateFirstEvent,
  seaDeleteEvent,
  seaGetEvent,
  seaGetEventCapabilities,
  seaGetEventWritePermission,
  seaListEvents,
  seaMarkEventRead,
  seaMarkEventUnread,
  seaMoveEventDown,
  seaMoveEventUp,
  seaUncommitEvent,
  seaUpdateEvent,
} from "../generated/sea/sea";
import type { SeaCommandResult } from "../generated/sea/model";
import { mapSeaEvent } from "../../mappers/seaReadDtoMappers";
import type { SeaEventReadDto, SeaEventCapabilities } from "../../models/seaStatModels";

export interface SeaEventWriteDto {
  projectId?: string;
  procedureId?: string;
  title?: string;
  description?: string;
  previousEventId?: string;
  committedAt?: string;
  committedBy?: string;
  id?: string;
}

export async function listSeaEvents(procedureId: string): Promise<SeaEventReadDto[]> {
  const events = await seaListEvents(procedureId);
  return events.map(mapSeaEvent);
}

export async function getSeaEvent(id: string): Promise<SeaEventReadDto> {
  return mapSeaEvent(await seaGetEvent(id));
}

export async function createSeaEventAfter(previousEventId: string | undefined, data: SeaEventWriteDto): Promise<SeaCommandResult> {
  return previousEventId ? seaCreateEventAfter(previousEventId, data) : seaCreateFirstEvent(data);
}

export async function updateSeaEvent(data: SeaEventWriteDto): Promise<SeaCommandResult> {
  return seaUpdateEvent(data);
}

export async function deleteSeaEvent(id: string): Promise<SeaCommandResult> {
  return seaDeleteEvent(id);
}

export async function moveSeaEventUp(id: string): Promise<SeaCommandResult> {
  return seaMoveEventUp(id);
}

export async function moveSeaEventDown(id: string): Promise<SeaCommandResult> {
  return seaMoveEventDown(id);
}

export async function commitSeaEvent(id: string): Promise<SeaCommandResult> {
  return seaCommitEvent(id);
}

export async function uncommitSeaEvent(id: string): Promise<SeaCommandResult> {
  return seaUncommitEvent(id);
}

export async function getSeaEventCapabilities(id: string): Promise<SeaEventCapabilities> {
  const result = await seaGetEventCapabilities(id);
  return {
    canRead: result.canRead ?? false,
    canWrite: result.canWrite ?? false,
    isElevated: result.canManage ?? false,
  };
}

export async function getSeaEventWritePermission(id: string): Promise<boolean> {
  return seaGetEventWritePermission(id);
}

export async function markSeaEventRead(id: string): Promise<SeaCommandResult> {
  return seaMarkEventRead(id);
}

export async function markSeaEventUnread(id: string): Promise<SeaCommandResult> {
  return seaMarkEventUnread(id);
}
