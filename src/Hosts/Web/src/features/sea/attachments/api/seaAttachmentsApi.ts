import {
  seaCreateAttachmentBatch,
  seaCreateFileAttachment,
  seaCreateTextAttachment,
  seaDeleteAttachment,
  seaGetAttachmentCount,
  seaListAttachments,
  seaUpdateTextAttachment,
} from "@/api/generated/sea/sea";
import type {
  AttachmentWriteDto,
  AttachmentReadDto as SeaAttachmentReadDto,
  SeaCommandResult,
} from "@/api/generated/sea/model";

export type { SeaAttachmentReadDto };

export interface SeaAttachmentWriteDto {
  eventId: string;
  content: string;
  contentType: string;
  contentThumbnail?: string;
  size: number;
}

function toGeneratedAttachmentWrite(data: SeaAttachmentWriteDto): AttachmentWriteDto {
  return {
    ...data,
    contentThumbnail: data.contentThumbnail ?? null,
  };
}

export async function listSeaAttachments(eventId: string): Promise<SeaAttachmentReadDto[]> {
  return seaListAttachments(eventId);
}

export async function getSeaAttachmentCount(eventId: string): Promise<number> {
  return seaGetAttachmentCount(eventId);
}

export async function createSeaTextAttachment(data: SeaAttachmentWriteDto): Promise<SeaCommandResult> {
  return seaCreateTextAttachment(toGeneratedAttachmentWrite(data));
}

export async function updateSeaTextAttachment(data: SeaAttachmentWriteDto): Promise<SeaCommandResult> {
  return seaUpdateTextAttachment(toGeneratedAttachmentWrite(data));
}

export async function createSeaFileAttachment(data: SeaAttachmentWriteDto): Promise<SeaCommandResult> {
  return seaCreateFileAttachment(toGeneratedAttachmentWrite(data));
}

export async function createSeaAttachmentBatch(data: SeaAttachmentWriteDto[]): Promise<SeaCommandResult> {
  return seaCreateAttachmentBatch(data.map(toGeneratedAttachmentWrite));
}

export async function deleteSeaAttachment(id: string): Promise<SeaCommandResult> {
  return seaDeleteAttachment(id);
}
