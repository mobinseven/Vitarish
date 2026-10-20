import { filesUploadFile } from "../../api/generated/files/files";

/** Images every browser shows; the server accepts only these for logos and photos. */
export const displayableImageTypes = ["image/jpeg", "image/png", "image/webp"] as const;

/** A logo needs no more than this; larger files are refused before upload. */
export const maximumLogoBytes = 5 * 1024 * 1024;

export type ImageUploadRefusal = "type" | "size";

export function refuseImage(file: File, maximumBytes: number): ImageUploadRefusal | null {
  if (!(displayableImageTypes as readonly string[]).includes(file.type)) return "type";
  if (file.size > maximumBytes) return "size";
  return null;
}

async function sha256Hex(file: File): Promise<string> {
  const digest = await crypto.subtle.digest("SHA-256", await file.arrayBuffer());
  return Array.from(new Uint8Array(digest), (byte) => byte.toString(16).padStart(2, "0")).join("");
}

/**
 * Uploads an image to the current tenant through Files and returns its id. Files recomputes the
 * checksum itself; the claimed one only lets it refuse a corrupted transfer.
 */
export async function uploadTenantImage(file: File): Promise<string> {
  const response = await filesUploadFile({
    content: file,
    fileName: file.name,
    contentType: file.type,
    size: file.size,
    checksum: await sha256Hex(file),
  });
  if (!response.file.id) throw new Error("The uploaded file has no id.");
  return response.file.id;
}
