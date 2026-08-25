import { apiFetchBlob } from "@vitarish/api-client";

const storedFileIdPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function isStoredSeaAttachmentContent(content: string): boolean {
  return storedFileIdPattern.test(content.trim());
}

export async function loadStoredSeaAttachmentContent(
  fileId: string,
  signal?: AbortSignal,
): Promise<Blob> {
  if (!isStoredSeaAttachmentContent(fileId)) {
    throw new TypeError("SEA attachment content is not a stored file identifier.");
  }

  return apiFetchBlob(`/api/modules/files/${encodeURIComponent(fileId.trim())}/content`, {
    signal,
  });
}
