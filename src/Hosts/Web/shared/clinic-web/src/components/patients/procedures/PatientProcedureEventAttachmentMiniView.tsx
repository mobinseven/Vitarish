"use client";

import { Chip } from "@heroui/react";
import { ChatAttachment, ChatAttachmentGroup } from "@heroui-pro/react";
import { isStoredSeaAttachmentContent } from "@vitarish/sea-web";
import { useLocalizedText } from "../../../localization";
import type { EventItemSummary } from "../../../models/patients/procedures/PatientProcedureEventItemModels";

interface PatientProcedureEventAttachmentMiniViewProps {
  items: EventItemSummary[];
}

const visibleAttachmentLimit = 3;

function getPreviewSource(item: EventItemSummary): string | undefined {
  if (item.type !== "media") return undefined;

  const source = item.contentThumbnail ?? item.content;
  return source && !isStoredSeaAttachmentContent(source) ? source : undefined;
}

export function PatientProcedureEventAttachmentMiniView({
  items,
}: PatientProcedureEventAttachmentMiniViewProps) {
  const { t } = useLocalizedText();
  const attachments = items.filter((item) => item.type !== "note");

  if (attachments.length === 0) return null;

  const visibleAttachments = attachments.slice(0, visibleAttachmentLimit);
  const hiddenAttachmentCount = attachments.length - visibleAttachments.length;

  return (
    <div
      aria-label={t("clinic.procedures.items.attachmentsPreview")}
      className="flex min-w-0 flex-wrap items-center gap-2"
      role="group"
    >
      <ChatAttachmentGroup className="min-w-0 flex-wrap">
        {visibleAttachments.map((item) => (
          <ChatAttachment
            key={item.id}
            mimeType={item.contentType}
            name={item.fileName ?? t("clinic.procedures.items.genericAttachment")}
            src={getPreviewSource(item)}
          >
            <ChatAttachment.Preview />
            <ChatAttachment.Name>
              {item.fileName ?? t("clinic.procedures.items.genericAttachment")}
            </ChatAttachment.Name>
          </ChatAttachment>
        ))}
      </ChatAttachmentGroup>
      {hiddenAttachmentCount > 0 ? (
        <Chip size="sm" variant="secondary">
          {t("clinic.procedures.items.moreAttachments", { count: hiddenAttachmentCount })}
        </Chip>
      ) : null}
    </div>
  );
}
