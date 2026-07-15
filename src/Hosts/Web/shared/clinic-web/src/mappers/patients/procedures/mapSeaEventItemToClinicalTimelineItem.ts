import type { SeaAttachmentReadDto } from "@vitarish/sea-web";
import type { EventItemSummary, EventItemType } from "../../../models/patients/procedures/PatientProcedureEventItemModels";

function classifyItemType(kind: number, contentType?: string | null): EventItemType {
  if (kind === 0) return "note";
  if (!contentType) return "file";
  if (contentType.startsWith("audio/") || contentType.startsWith("video/")) return "media";
  if (contentType.startsWith("image/")) return "media";
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
    createdAt: dto.createdAt,
  };
}
