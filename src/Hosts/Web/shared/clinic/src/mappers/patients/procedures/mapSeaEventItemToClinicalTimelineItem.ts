import type { SeaAttachmentReadDto } from "@vitarish/sea";
import type { EventItemSummary, EventItemType } from "../../../models/patients/procedures/PatientProcedureEventItemModels";

function classifyItemType(kind: number, contentType?: string | null): EventItemType {
  const mimeType = contentType?.trim().toLowerCase();
  if (
    mimeType?.startsWith("audio/") ||
    mimeType?.startsWith("video/") ||
    mimeType?.startsWith("image/")
  ) {
    return "media";
  }
  if (kind === 0 && (!mimeType || mimeType.startsWith("text/"))) return "note";
  return "file";
}

export function mapToEventItemSummary(dto: SeaAttachmentReadDto): EventItemSummary {
  return {
    id: dto.id,
    eventId: dto.eventId,
    type: classifyItemType(dto.kind, dto.contentType),
    content: dto.content,
    contentType: dto.contentType ?? undefined,
    contentThumbnail: dto.contentThumbnail ?? undefined,
    size: dto.size,
    fileName: dto.fileName ?? undefined,
    authorId: dto.createdBy ?? undefined,
    createdAt: dto.createdAt,
  };
}
