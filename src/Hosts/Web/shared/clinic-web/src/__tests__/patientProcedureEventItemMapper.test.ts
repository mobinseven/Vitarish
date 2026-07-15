import { describe, expect, it } from "vitest";
import type { SeaAttachmentReadDto } from "@vitarish/sea-web";
import { mapToEventItemSummary } from "../mappers/patients/procedures/mapSeaEventItemToClinicalTimelineItem";

function createAttachment(overrides: Partial<SeaAttachmentReadDto>): SeaAttachmentReadDto {
  return {
    id: "attachment-1",
    eventId: "event-1",
    fileName: null,
    content: "Clinical note",
    contentType: "text/plain",
    contentThumbnail: null,
    size: 13,
    kind: 0,
    createdAt: "2026-07-14T08:00:00Z",
    createdBy: null,
    updatedAt: null,
    updatedBy: null,
    deletedAt: null,
    deletedBy: null,
    isDeleted: false,
    metadata: null,
    metadataValues: null,
    ...overrides,
  };
}

describe("mapToEventItemSummary", () => {
  it("uses attachment kind as the authoritative note/file distinction", () => {
    const file = mapToEventItemSummary(
      createAttachment({ kind: 1, fileName: "instructions.txt" }),
    );
    const note = mapToEventItemSummary(createAttachment({ kind: 0 }));

    expect(file).toMatchObject({ type: "file", fileName: "instructions.txt" });
    expect(note).toMatchObject({ type: "note", fileName: undefined });
  });

  it("uses MIME type only to refine file attachments as media", () => {
    const media = mapToEventItemSummary(
      createAttachment({
        kind: 1,
        fileName: "scan.png",
        contentType: "image/png",
      }),
    );

    expect(media.type).toBe("media");
  });
});
