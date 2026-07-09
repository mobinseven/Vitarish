export type EventItemType = "note" | "file" | "media" | "reminder" | "audit" | "plugin";

export interface EventItemSummary {
  id: string;
  eventId: string;
  type: EventItemType;
  content: string;
  contentType?: string;
  contentThumbnail?: string;
  size?: number;
  author?: string;
  createdAt: string;
}
