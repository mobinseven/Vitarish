import { beforeEach, describe, expect, it, vi } from "vitest";
import { apiFetchBlob } from "@vitarish/web-api-client";
import {
  isStoredSeaAttachmentContent,
  loadStoredSeaAttachmentContent,
} from "../api/attachments/seaAttachmentContent";

vi.mock("@vitarish/web-api-client", () => ({
  apiFetchBlob: vi.fn(),
}));

describe("legacy SEA attachment content", () => {
  beforeEach(() => {
    vi.mocked(apiFetchBlob).mockReset();
  });

  it("recognizes stored Files-module identifiers without treating data URLs as file IDs", () => {
    expect(isStoredSeaAttachmentContent("019ea70e-dc73-73b3-9f03-1744bc58eeee")).toBe(true);
    expect(isStoredSeaAttachmentContent("data:image/png;base64,image")).toBe(false);
  });

  it("loads stored content through the authenticated binary transport", async () => {
    const blob = new Blob(["image"], { type: "image/png" });
    vi.mocked(apiFetchBlob).mockResolvedValue(blob);
    const controller = new AbortController();

    await expect(
      loadStoredSeaAttachmentContent(
        "019ea70e-dc73-73b3-9f03-1744bc58eeee",
        controller.signal,
      ),
    ).resolves.toBe(blob);
    expect(apiFetchBlob).toHaveBeenCalledWith(
      "/api/modules/files/019ea70e-dc73-73b3-9f03-1744bc58eeee/content",
      { signal: controller.signal },
    );
  });

  it("rejects non-file content before making a request", async () => {
    await expect(loadStoredSeaAttachmentContent("data:image/png;base64,image")).rejects.toBeInstanceOf(
      TypeError,
    );
    expect(apiFetchBlob).not.toHaveBeenCalled();
  });
});
