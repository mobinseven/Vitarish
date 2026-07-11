import type { EventItemSummary, EventItemType } from "../models/PatientProcedureEventItemModels";

function classifyItemType(contentType?: string | null): EventItemType {
  if (!contentType) return "note";
  if (contentType.startsWith("audio/") || contentType.startsWith("video/")) return "media";
  if (contentType.startsWith("image/")) return "media";
  if (contentType === "text/plain" || contentType === "text/markdown") return "note";
  return "file";
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function mapToEventItemSummary(dto: any): EventItemSummary {
  return {
    id: dto.id,
    eventId: dto.eventId,
    type: classifyItemType(dto.contentType),
    content: dto.content,
    contentType: dto.contentType ?? undefined,
    contentThumbnail: dto.contentThumbnail ?? undefined,
    size: typeof dto.size === "string" ? Number(dto.size) : dto.size ?? undefined,
    createdAt: dto.createdAt,
  };
}
