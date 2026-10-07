import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@vitarish/api-client";
import type { FileUploadPolicyResponse } from "../api/generated/files/model";

const filesUploadFile = vi.fn();
const filesGetUploadPolicy = vi.fn();
vi.mock("../api/generated/files/files", () => ({
  filesUploadFile: (...args: unknown[]) => filesUploadFile(...args),
  filesGetUploadPolicy: (...args: unknown[]) => filesGetUploadPolicy(...args),
}));

const {
  checkFileForUpload,
  fileUploadAccept,
  FileUploadRejectedError,
  toSeaFileAttachment,
} = await import("../api/attachments/seaAttachmentFile");

const policy: FileUploadPolicyResponse = {
  maximumContentBytes: 16,
  formats: [
    { mediaType: "image/heic", extensions: [".heic"] },
    { mediaType: "video/mp4", extensions: [".mp4", ".m4v"] },
  ],
};

describe("SEA attachment file upload", () => {
  beforeEach(() => {
    filesUploadFile.mockReset();
    filesGetUploadPolicy.mockReset().mockResolvedValue(policy);
  });

  it("uploads the bytes to Files and references the stored file from the attachment", async () => {
    filesUploadFile.mockResolvedValueOnce({ file: { id: "0f5c2b4e-7d1a-4c8e-9b3f-2a6d8e1c4b7a" } });
    const file = new File(["walk"], "walk.MP4", { type: "video/mp4" });

    await expect(toSeaFileAttachment("event-1", file)).resolves.toEqual({
      eventId: "event-1",
      content: "0f5c2b4e-7d1a-4c8e-9b3f-2a6d8e1c4b7a",
      contentType: "video/mp4",
      size: 4,
      kind: 1,
      fileName: "walk.MP4",
    });
    expect(filesUploadFile).toHaveBeenCalledWith({
      content: file,
      fileName: "walk.MP4",
      contentType: "video/mp4",
      size: 4,
      // SHA-256 of "walk"
      checksum: "7a6c4fac6c5cd7209ee2eecd9d6ad3673c55315117de1d8daa5aeb3f71bbd7c7",
    });
  });

  it("declares the accepted media type from the extension when the browser reports none", async () => {
    filesUploadFile.mockResolvedValueOnce({ file: { id: "0f5c2b4e-7d1a-4c8e-9b3f-2a6d8e1c4b7a" } });

    const attachment = await toSeaFileAttachment("event-1", new File(["heic"], "IMG_0001.heic"), policy);

    expect(attachment.contentType).toBe("image/heic");
    expect(filesGetUploadPolicy).not.toHaveBeenCalled();
  });

  it.each([
    ["typeNotAllowed", new File(["MZ"], "setup.exe", { type: "application/x-msdownload" })],
    ["typeNotAllowed", new File(["text"], "notes.txt", { type: "text/plain" })],
    ["tooLarge", new File(["x".repeat(17)], "walk.mp4", { type: "video/mp4" })],
    ["tooLarge", new File([], "walk.mp4", { type: "video/mp4" })],
  ])("rejects a file the policy does not accept (%s) without uploading it", async (reason, file) => {
    const rejection = toSeaFileAttachment("event-1", file, policy);

    await expect(rejection).rejects.toBeInstanceOf(FileUploadRejectedError);
    await expect(rejection).rejects.toMatchObject({ reason, maximumContentBytes: 16 });
    expect(filesUploadFile).not.toHaveBeenCalled();
  });

  it.each([
    [413, "tooLarge"],
    [415, "typeNotAllowed"],
  ])("maps a server %s rejection to the same reason", async (status, reason) => {
    filesUploadFile.mockRejectedValueOnce(new ApiError({ status, title: "Rejected" }));

    await expect(toSeaFileAttachment("event-1", new File(["walk"], "walk.mp4"), policy))
      .rejects.toMatchObject({ reason });
  });

  it("builds the picker filter from every accepted extension and media type", () => {
    expect(fileUploadAccept(policy)).toBe(".heic,image/heic,.mp4,.m4v,video/mp4");
    expect(checkFileForUpload(policy, new File(["walk"], "walk.m4v"))).toBeUndefined();
  });
});
