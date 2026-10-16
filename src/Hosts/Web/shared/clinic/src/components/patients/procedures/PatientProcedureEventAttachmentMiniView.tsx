"use client";

import type { ReactNode } from "react";
import { Avatar } from "@heroui/react";
import { FaIcon } from "@vitarish/ui";
import { useProcedureEventAuthorNames } from "../../../hooks/patients/procedures/useProcedureEventAuthorNames";
import { usePatientProcedureAttachmentSource } from "../../../hooks/patients/procedures/usePatientProcedureAttachmentSource";
import { formatDateTime, useLocalizedText } from "../../../localization";
import type {
  EventItemSummary,
  ProcedureEventViewer,
} from "../../../models/patients/procedures/PatientProcedureEventItemModels";

interface PatientProcedureEventAttachmentMiniViewProps {
  items: EventItemSummary[];
  viewer?: ProcedureEventViewer;
}

function ImageAttachmentPreview({ item }: { item: EventItemSummary }) {
  const { t } = useLocalizedText();
  const source = usePatientProcedureAttachmentSource(
    item.contentThumbnail ?? item.content,
    true,
  );
  const label = item.fileName ?? t("clinic.procedures.items.genericAttachment");

  if (source.loading || !source.url) {
    return (
      <div
        aria-busy={source.loading}
        className="bg-surface-secondary flex size-16 items-center justify-center rounded-xl"
      >
        <FaIcon icon="image" aria-hidden="true" className="text-muted" />
      </div>
    );
  }

  return (
    <img
      alt={label}
      className="size-16 rounded-xl object-cover"
      src={source.url}
    />
  );
}

function EventAttachmentPreview({
  item,
  authorName,
}: {
  item: EventItemSummary;
  authorName?: string;
}) {
  const { locale, t } = useLocalizedText();
  const author = authorName ?? t("clinic.procedures.items.careTeam");

  let content: ReactNode;

  if (item.type === "note") {
    content = (
      <p
        dir="auto"
        className="text-foreground m-0 whitespace-pre-wrap text-base leading-relaxed"
      >
        {item.content}
      </p>
    );
  } else if (item.contentType?.startsWith("image/")) {
    content = <ImageAttachmentPreview item={item} />;
  } else {
    content = (
      <div className="text-foreground flex min-w-0 items-center gap-2 text-base">
        <FaIcon icon="file" aria-hidden="true" />
        <span dir="auto" className="truncate">
          {item.fileName ?? t("clinic.procedures.items.genericAttachment")}
        </span>
      </div>
    );
  }

  return (
    <article
      aria-label={author}
      className="flex min-w-0 items-start gap-3 py-2"
    >
      <Avatar
        color="default"
        size="sm"
        variant="soft"
        className="bg-surface-tertiary shrink-0"
      >
        <Avatar.Fallback>
          <FaIcon icon="user-doctor" aria-hidden="true" />
        </Avatar.Fallback>
      </Avatar>
      <div className="flex min-w-0 flex-1 flex-col gap-1">
        <div className="flex flex-wrap items-baseline gap-2 text-sm">
          <span className="text-foreground font-semibold">{author}</span>
          <time className="text-muted tabular-nums" dateTime={item.createdAt}>
            {formatDateTime(item.createdAt, locale)}
          </time>
        </div>
        {content}
      </div>
    </article>
  );
}

export function PatientProcedureEventAttachmentMiniView({
  items,
  viewer,
}: PatientProcedureEventAttachmentMiniViewProps) {
  const { t } = useLocalizedText();
  const authorNames = useProcedureEventAuthorNames();
  const visibleItems = items.slice(-3);
  const remainingCount = items.length - visibleItems.length;

  if (items.length === 0) return null;

  return (
    <div
      aria-label={t("clinic.procedures.items.attachmentsPreview")}
      className="flex min-w-0 flex-col divide-y divide-separator"
      role="group"
    >
      {visibleItems.map((item) => (
        <EventAttachmentPreview
          key={item.id}
          item={item}
          authorName={
            viewer?.id && item.authorId === viewer.id
              ? t("clinic.procedures.items.you")
              : item.authorId
                ? authorNames.get(item.authorId)
                : undefined
          }
        />
      ))}
      {remainingCount > 0 && (
        <span className="text-muted pt-2 text-sm">
          {t("clinic.procedures.items.moreAttachments", {
            count: remainingCount,
          })}
        </span>
      )}
    </div>
  );
}
