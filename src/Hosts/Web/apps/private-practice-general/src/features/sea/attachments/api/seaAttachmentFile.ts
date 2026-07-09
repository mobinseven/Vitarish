import type { SeaAttachmentWriteDto } from "./seaAttachmentsApi";

export async function toSeaFileAttachment(eventId: string, file: File): Promise<SeaAttachmentWriteDto> {
  return {
    eventId,
    content: await readFileAsDataUrl(file),
    contentType: file.type || "application/octet-stream",
    size: file.size,
  };
}

function readFileAsDataUrl(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      if (typeof reader.result === "string") {
        resolve(reader.result);
        return;
      }

      reject(new Error("File could not be read as text."));
    };
    reader.onerror = () => reject(reader.error ?? new Error("File could not be read."));
    reader.readAsDataURL(file);
  });
}
