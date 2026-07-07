import {
  seaCreateProcedure,
  seaDeleteProcedure,
  seaGetProcedure,
  seaListProcedures,
  seaUpdateProcedure,
} from "@/api/generated/sea/sea";
import {
  seaCommitEvent,
  seaCreateEventAfter,
  seaCreateFirstEvent,
  seaDeleteEvent,
  seaGetEvent,
  seaListEvents,
  seaMoveEventDown,
  seaMoveEventUp,
  seaUncommitEvent,
  seaUpdateEvent,
} from "@/api/generated/sea/sea";
import {
  seaDeleteAttachment,
  seaListAttachments,
  seaCreateTextAttachment,
  seaCreateFileAttachment,
} from "@/api/generated/sea/sea";
import {
  seaListEventRoles,
  seaListEventReadRoles,
  seaListEventWriteRoles,
  seaAssignEventRole,
  seaRemoveEventRole,
  seaListRoles,
} from "@/api/generated/sea/sea";
import { seaListProjects } from "@/api/generated/sea/sea";
import type { SeaCommandResult } from "@/api/generated/sea/model";

export { type SeaCommandResult };

// --- Procedure operations ---

export async function listProcedures(projectId: string) {
  return seaListProcedures(projectId);
}

export async function getProcedure(id: string) {
  return seaGetProcedure(id);
}

export async function createProcedure(data: { projectId?: string; name?: string; description?: string; id?: string }) {
  return seaCreateProcedure(data);
}

export async function updateProcedure(data: { projectId?: string; name?: string; description?: string; id?: string }) {
  return seaUpdateProcedure(data);
}

export async function deleteProcedure(id: string): Promise<SeaCommandResult> {
  return seaDeleteProcedure(id);
}

// --- Event operations ---

export async function listProcedureEvents(procedureId: string) {
  return seaListEvents(procedureId);
}

export async function getEvent(id: string) {
  return seaGetEvent(id);
}

export async function createEvent(data: { projectId?: string; procedureId?: string; title?: string; description?: string; previousEventId?: string }): Promise<SeaCommandResult> {
  return data.previousEventId
    ? seaCreateEventAfter(data.previousEventId, data)
    : seaCreateFirstEvent(data);
}

export async function updateEvent(data: { projectId?: string; procedureId?: string; title?: string; description?: string; id?: string }): Promise<SeaCommandResult> {
  return seaUpdateEvent(data);
}

export async function deleteEvent(id: string): Promise<SeaCommandResult> {
  return seaDeleteEvent(id);
}

export async function moveEventUp(id: string): Promise<SeaCommandResult> {
  return seaMoveEventUp(id);
}

export async function moveEventDown(id: string): Promise<SeaCommandResult> {
  return seaMoveEventDown(id);
}

export async function commitEvent(id: string): Promise<SeaCommandResult> {
  return seaCommitEvent(id);
}

export async function uncommitEvent(id: string): Promise<SeaCommandResult> {
  return seaUncommitEvent(id);
}

// --- Event item (attachment) operations ---

export async function listEventItems(eventId: string) {
  return seaListAttachments(eventId);
}

export async function createEventItemText(data: { eventId: string; content: string; contentType: string; size: number }): Promise<SeaCommandResult> {
  return seaCreateTextAttachment({
    eventId: data.eventId,
    content: data.content,
    contentType: data.contentType,
    contentThumbnail: null,
    size: data.size,
  });
}

export async function createEventItemFile(data: { eventId: string; content: string; contentType: string; size: number }): Promise<SeaCommandResult> {
  return seaCreateFileAttachment({
    eventId: data.eventId,
    content: data.content,
    contentType: data.contentType,
    contentThumbnail: null,
    size: data.size,
  });
}

export async function deleteEventItem(id: string): Promise<SeaCommandResult> {
  return seaDeleteAttachment(id);
}

// --- Access / role operations ---

export async function listEventRoles(eventId: string): Promise<string[]> {
  return seaListEventRoles(eventId);
}

export async function listEventReadRoles(eventId: string): Promise<string[]> {
  return seaListEventReadRoles(eventId);
}

export async function listEventWriteRoles(eventId: string): Promise<string[]> {
  return seaListEventWriteRoles(eventId);
}

export async function assignEventRole(data: { eventId: string; roleId: string; type: string }): Promise<SeaCommandResult> {
  return seaAssignEventRole(data as never);
}

export async function removeEventRole(id: string): Promise<SeaCommandResult> {
  return seaRemoveEventRole(id);
}

export async function listAllRoles() {
  return seaListRoles();
}

// --- Template operations ---

export async function listTemplateProcedures() {
  const projects = await seaListProjects();
  return projects.filter((p) => p.template);
}
