import { ApiError } from "@vitarish/api-client";
import { filesGetUploadPolicy, filesUploadFile } from "../generated/files/files";
import type { FileUploadFormatResponse, FileUploadPolicyResponse } from "../generated/files/model";
import type { SeaAttachmentWriteDto } from "./seaAttachmentsApi";

export type FileUploadRejectionReason = "typeNotAllowed" | "tooLarge";

/** A file the Files upload policy does not accept, found before or by the upload. */
export class FileUploadRejectedError extends Error {
  constructor(
    readonly reason: FileUploadRejectionReason,
    readonly maximumContentBytes: number,
  ) {
    super(reason === "tooLarge" ? "The file is larger than uploads allow." : "This file type is not accepted.");
    this.name = "FileUploadRejectedError";
  }
}

const statusRejections: Partial<Record<number, FileUploadRejectionReason>> = {
  413: "tooLarge",
  415: "typeNotAllowed",
};

/** The picker filter for the accepted formats: their extensions and media types. */
export function fileUploadAccept(policy: FileUploadPolicyResponse): string {
  return policy.formats.flatMap((format) => [...format.extensions, format.mediaType]).join(",");
}

/**
 * The accepted format for a file, chosen by its extension because browsers often report no type
 * (or a vendor alias) for HEIC and video files. The server still checks the content's signature.
 */
export function findFileUploadFormat(policy: FileUploadPolicyResponse, file: File): FileUploadFormatResponse | undefined {
  const dot = file.name.lastIndexOf(".");
  const extension = dot < 0 ? "" : file.name.slice(dot).toLowerCase();
  return policy.formats.find((format) => format.extensions.includes(extension));
}

/** Why the policy would reject a file, or undefined when it may be uploaded. */
export function checkFileForUpload(policy: FileUploadPolicyResponse, file: File): FileUploadRejectedError | undefined {
  const maximumContentBytes = Number(policy.maximumContentBytes);
  if (!findFileUploadFormat(policy, file)) return new FileUploadRejectedError("typeNotAllowed", maximumContentBytes);
  if (file.size === 0 || file.size > maximumContentBytes) return new FileUploadRejectedError("tooLarge", maximumContentBytes);
  return undefined;
}

/**
 * Uploads the file's bytes to Files, which streams them to storage after checking the format, size and
 * checksum, and returns the attachment payload that references the stored file.
 */
export async function toSeaFileAttachment(
  eventId: string,
  file: File,
  policy?: FileUploadPolicyResponse,
): Promise<SeaAttachmentWriteDto> {
  const uploadPolicy = policy ?? (await filesGetUploadPolicy());
  const rejection = checkFileForUpload(uploadPolicy, file);
  if (rejection) throw rejection;
  const contentType = findFileUploadFormat(uploadPolicy, file)!.mediaType;

  let stored;
  try {
    stored = await filesUploadFile({
      content: file,
      fileName: file.name,
      contentType,
      size: file.size,
      checksum: await sha256Hex(file),
    });
  } catch (error) {
    const reason = error instanceof ApiError ? statusRejections[error.status] : undefined;
    if (reason) throw new FileUploadRejectedError(reason, Number(uploadPolicy.maximumContentBytes));
    throw error;
  }

  const fileId = stored.file.id;
  if (!fileId) throw new Error("The upload response did not identify the stored file.");

  return {
    eventId,
    content: fileId,
    contentType,
    size: file.size,
    kind: 1,
    fileName: file.name,
  };
}

async function sha256Hex(file: Blob): Promise<string> {
  const digest = await crypto.subtle.digest("SHA-256", await file.arrayBuffer());
  return Array.from(new Uint8Array(digest), (value) => value.toString(16).padStart(2, "0")).join("");
}
