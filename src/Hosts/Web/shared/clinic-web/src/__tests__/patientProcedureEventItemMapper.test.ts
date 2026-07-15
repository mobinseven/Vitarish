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
  it("preserves explicit file kinds even when the file contains text", () => {
    const file = mapToEventItemSummary(
      createAttachment({ kind: 1, fileName: "instructions.txt" }),
    );
    const note = mapToEventItemSummary(createAttachment({ kind: 0 }));

    expect(file).toMatchObject({ type: "file", fileName: "instructions.txt" });
    expect(note).toMatchObject({ type: "note", fileName: undefined });
  });

  it("uses MIME type to recover legacy files whose kind was left at the note default", () => {
    const legacyImage = mapToEventItemSummary(
      createAttachment({
        content: "019ea70e-dc73-73b3-9f03-1744bc58eeee",
        contentType: "image/png",
        fileName: "scan.png",
        kind: 0,
      }),
    );
    const legacyDocument = mapToEventItemSummary(
      createAttachment({
        content: "019ea722-3f7e-7814-b72e-e54388c37b4e",
        contentType: "application/pdf",
        fileName: "report.pdf",
        kind: 0,
      }),
    );

    expect(legacyImage.type).toBe("media");
    expect(legacyDocument.type).toBe("file");
  });
});
