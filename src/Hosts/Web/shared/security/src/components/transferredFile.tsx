import { formatFileSize, type SupportedLocale } from "@vitarish/localization";
import type { ReactNode } from "react";

export interface TransferredFileDescriptionEvent {
  resourceName?: string | null;
  resourceExtension?: string | null;
  resourceSize?: number | string | null;
}

interface TransferredFileDescriptionProps {
  event: TransferredFileDescriptionEvent;
  locale: SupportedLocale;
  className?: string;
  accessibleLabel?: string;
  fallback?: string;
}

type TransferredFilePart = {
  key: "name" | "extension" | "size";
  value: string;
};

export function TransferredFileDescription({
  event,
  locale,
  className,
  accessibleLabel,
  fallback,
}: TransferredFileDescriptionProps): ReactNode {
  const parts: TransferredFilePart[] = [];
  if (event.resourceName) parts.push({ key: "name", value: event.resourceName });
  if (event.resourceExtension) parts.push({ key: "extension", value: event.resourceExtension.toUpperCase() });
  // The contract carries a 64-bit size, which the generated client may type as a string.
  if (event.resourceSize != null && Number.isFinite(Number(event.resourceSize))) {
    parts.push({ key: "size", value: formatFileSize(Number(event.resourceSize), locale) });
  }

  if (parts.length === 0) return fallback ?? null;

  return (
    <span className={className}>
      {accessibleLabel ? <span className="sr-only">{accessibleLabel}: </span> : null}
      {parts.map((part, index) => (
        <span key={part.key}>
          {index > 0 ? " · " : null}
          <bdi dir="auto">{part.value}</bdi>
        </span>
      ))}
    </span>
  );
}
