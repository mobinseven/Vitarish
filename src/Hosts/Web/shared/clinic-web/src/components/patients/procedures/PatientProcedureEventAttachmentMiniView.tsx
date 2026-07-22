"use client";

import type { ReactNode } from "react";
import { FaIcon } from "@vitarish/web-ui";
import { usePatientProcedureAttachmentSource } from "../../../hooks/patients/procedures/usePatientProcedureAttachmentSource";
import { formatDateTime, useLocalizedText } from "../../../localization";
import type { EventItemSummary } from "../../../models/patients/procedures/PatientProcedureEventItemModels";

interface PatientProcedureEventAttachmentMiniViewProps {
  items: EventItemSummary[];
}

function ImageAttachmentPreview({ item }: { item: EventItemSummary }) {
  const { t } = useLocalizedText();
  const source = usePatientProcedureAttachmentSource(item.contentThumbnail ?? item.content, true);
  const label = item.fileName ?? t("clinic.procedures.items.genericAttachment");

  if (source.loading || !source.url) {
    return (
      <div aria-busy={source.loading} className="bg-muted flex size-10 items-center justify-center rounded-medium">
        <FaIcon icon="image" aria-hidden="true" className="text-muted" />
      </div>
    );
  }

  return <img alt={label} className="size-10 rounded object-cover" src={source.url} />;
}

function EventAttachmentPreview({ item }: { item: EventItemSummary }) {
  const { locale, t } = useLocalizedText();
  const authorName = item.authorName ?? t("clinic.procedures.items.careTeam");

  let content: ReactNode;

  if (item.type === "note") {
    content = <p className="text-muted m-0 whitespace-pre-wrap text-xs leading-5">{item.content}</p>;
  } else if (item.contentType?.startsWith("image/")) {
    content = <ImageAttachmentPreview item={item} />;
  } else {
    content = (
      <div className="text-muted flex min-w-0 items-center gap-2 text-xs leading-5">
        <FaIcon icon="file" aria-hidden="true" />
        <span className="truncate">{item.fileName ?? t("clinic.procedures.items.genericAttachment")}</span>
      </div>
    );
  }

  return (
    <div className="flex min-w-0 items-start justify-between gap-2 py-1">
      <div className="min-w-0 flex-1">{content}</div>
      <div className="flex shrink-0 items-center gap-2">
        <span className="text-foreground text-xs font-medium leading-5">{authorName}</span>
        {/* <time className="text-muted tabular-nums text-xs leading-5" dateTime={item.createdAt}>
          {formatDateTime(item.createdAt, locale, { dateStyle: "short" })}
        </time> */}
      </div>
    </div>
  );
}

export function PatientProcedureEventAttachmentMiniView({
  items,
}: PatientProcedureEventAttachmentMiniViewProps) {
  const { t } = useLocalizedText();

  if (items.length === 0) return null;

  return (
    <div aria-label={t("clinic.procedures.items.attachmentsPreview")} className="flex min-w-0 flex-col" role="group">
      {items.map((item) => <EventAttachmentPreview key={item.id} item={item} />)}
    </div>
  );
}
