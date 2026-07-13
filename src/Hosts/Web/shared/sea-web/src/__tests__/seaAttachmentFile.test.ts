import { describe, expect, it } from "vitest";
import { toSeaFileAttachment } from "../api/attachments/seaAttachmentFile";

describe("SEA attachment file mapping", () => {
  it("maps a browser file into an inline SEA file attachment payload", async () => {
    const file = new File(["hello"], "note.txt", { type: "text/plain" });

    await expect(toSeaFileAttachment("event-1", file)).resolves.toEqual({
      eventId: "event-1",
      content: "data:text/plain;base64,aGVsbG8=",
      contentType: "text/plain",
      size: 5,
    });
  });

  it("uses a binary fallback content type when the browser file has none", async () => {
    const file = new File(["hello"], "note");

    const attachment = await toSeaFileAttachment("event-1", file);

    expect(attachment.contentType).toBe("application/octet-stream");
  });
});
