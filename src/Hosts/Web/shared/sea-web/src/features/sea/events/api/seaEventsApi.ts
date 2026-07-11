import {
  seaCommitEvent,
  seaCreateEventAfter,
  seaCreateFirstEvent,
  seaDeleteEvent,
  seaGetEvent,
  seaGetEventWritePermission,
  seaListEvents,
  seaMarkEventRead,
  seaMarkEventUnread,
  seaMoveEventDown,
  seaMoveEventUp,
  seaUncommitEvent,
  seaUpdateEvent,
} from "../../../../api/generated/sea/sea";
import type { SeaCommandResult } from "../../../../api/generated/sea/model";
import { mapSeaEvent } from "../../shared/models/seaReadDtoMappers";
import type { SeaEventReadDto } from "../../shared/models/seaStatModels";

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

export async function getSeaEventWritePermission(id: string): Promise<boolean> {
  return seaGetEventWritePermission(id);
}

export async function markSeaEventRead(id: string): Promise<SeaCommandResult> {
  return seaMarkEventRead(id);
}

export async function markSeaEventUnread(id: string): Promise<SeaCommandResult> {
  return seaMarkEventUnread(id);
}
