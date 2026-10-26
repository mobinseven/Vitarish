import { formatFileSize, type SupportedLocale } from "@vitarish/localization";

// File transfer records keep only the extension and size of a file, never its name.
export function describeTransferredFile(
  event: { resourceExtension?: string | null; resourceSize?: number | string | null },
  locale: SupportedLocale,
): string | undefined {
  const parts = [
    event.resourceExtension ? event.resourceExtension.toUpperCase() : undefined,
    // The contract carries a 64-bit size, which the generated client may type as a string.
    event.resourceSize != null && Number.isFinite(Number(event.resourceSize)) ? formatFileSize(Number(event.resourceSize), locale) : undefined,
  ].filter((part): part is string => Boolean(part));
  return parts.length === 0 ? undefined : parts.join(" · ");
}
