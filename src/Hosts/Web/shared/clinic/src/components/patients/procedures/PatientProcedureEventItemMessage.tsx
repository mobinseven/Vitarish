"use client";

import { useState } from "react";
import { Avatar, Button, Modal } from "@heroui/react";
import { ChatAttachment, ContextMenu } from "@heroui-pro/react";
import { MenuItem } from "react-aria-components";
import { FaIcon } from "@vitarish/ui";
import { isStoredSeaAttachmentContent } from "@vitarish/sea";
import { usePatientProcedureAttachmentSource } from "../../../hooks/patients/procedures/usePatientProcedureAttachmentSource";
import {
  formatDate,
  formatFileSize,
  useLocalizedText,
} from "../../../localization";
import type {
  EventItemSummary,
  ProcedureEventViewer,
} from "../../../models/patients/procedures/PatientProcedureEventItemModels";

export interface PatientProcedureEventItemMessageProps {
  /** The author's display name when the practice's members could be resolved. */
  authorName?: string;
  canWrite: boolean;
  isDeleting: boolean;
  item: EventItemSummary;
  showAuthor?: boolean;
  viewer?: ProcedureEventViewer;
  onDelete: () => void;
}

interface AttachmentPreviewDialogProps {
  contentType?: string;
  fileName: string;
  isOpen: boolean;
  size?: number;
  src: string;
  onOpenChange: (isOpen: boolean) => void;
}

function AttachmentPreviewDialog({
  contentType,
  fileName,
  isOpen,
  size,
  src,
  onOpenChange,
}: AttachmentPreviewDialogProps) {
  const { t, locale } = useLocalizedText();

  return (
    <Modal.Backdrop isOpen={isOpen} variant="blur" onOpenChange={onOpenChange}>
      <Modal.Container placement="center" size="cover">
        <Modal.Dialog aria-label={fileName} className="sm:max-w-5xl">
          <Modal.CloseTrigger
            aria-label={t("clinic.procedures.items.closePreview")}
          />
          <Modal.Header className="min-w-0">
            <Modal.Heading className="truncate">{fileName}</Modal.Heading>
          </Modal.Header>
          <Modal.Body className="gap-4">
            <div className="bg-default-50 flex min-h-64 items-center justify-center overflow-hidden rounded-2xl p-3 sm:min-h-96">
              {contentType?.startsWith("video/") ? (
                <video
                  aria-label={fileName}
                  className="max-h-[65vh] max-w-full rounded-xl"
                  controls
                  src={src}
                />
              ) : contentType?.startsWith("audio/") ? (
                <audio
                  aria-label={fileName}
                  className="w-full max-w-xl"
                  controls
                  src={src}
                />
              ) : (
                <img
                  alt={fileName}
                  className="max-h-[65vh] max-w-full rounded-xl object-contain"
                  src={src}
                />
              )}
            </div>
            <dl className="text-muted flex flex-wrap gap-x-6 gap-y-2 text-sm">
              {contentType ? (
                <div className="flex gap-2">
                  <dt>{t("clinic.procedures.items.fileType")}</dt>
                  <dd className="text-foreground m-0" dir="ltr">
                    {contentType}
                  </dd>
                </div>
              ) : null}
              {size == null ? null : (
                <div className="flex gap-2">
                  <dt>{t("clinic.procedures.items.fileSize")}</dt>
                  <dd className="text-foreground m-0">
                    {formatFileSize(size, locale)}
                  </dd>
                </div>
              )}
            </dl>
          </Modal.Body>
          <Modal.Footer>
            <Button slot="close" variant="tertiary">
              {t("clinic.procedures.items.closePreview")}
            </Button>
            <a
              className="bg-accent text-accent-foreground inline-flex min-h-9 items-center gap-2 rounded-xl px-4 text-sm font-medium no-underline"
              download={fileName}
              href={src}
            >
              <FaIcon icon="download" aria-hidden="true" className="size-4" />
              {t("clinic.procedures.items.download")}
            </a>
          </Modal.Footer>
        </Modal.Dialog>
      </Modal.Container>
    </Modal.Backdrop>
  );
}

export function PatientProcedureEventItemMessage({
  authorName,
  canWrite,
  isDeleting,
  item,
  showAuthor = true,
  viewer,
  onDelete,
}: PatientProcedureEventItemMessageProps) {
  const { t, locale } = useLocalizedText();
  const [isPreviewOpen, setIsPreviewOpen] = useState(false);
  const source = usePatientProcedureAttachmentSource(
    item.content,
    item.type !== "note",
  );
  const isCurrentUser = Boolean(viewer?.id && item.authorId === viewer.id);
  const authorLabel = isCurrentUser
    ? t("clinic.procedures.items.you")
    : (authorName ?? item.authorName ?? t("clinic.procedures.items.careTeam"));
  const fileName =
    item.fileName ?? t("clinic.procedures.items.genericAttachment");
  const attachmentPreview = (
    <ChatAttachment
      className="max-w-full"
      mimeType={item.contentType}
      name={fileName}
      src={
        item.type === "media"
          ? item.contentThumbnail &&
            !isStoredSeaAttachmentContent(item.contentThumbnail)
            ? item.contentThumbnail
            : source.url
          : undefined
      }
    />
  );
  const attachment =
    item.type === "note" ? null : item.type === "media" ? (
      <>
        <Button
          aria-label={`${t("clinic.procedures.items.preview")} ${fileName}`}
          aria-busy={source.loading}
          className="h-auto max-w-full p-0"
          isDisabled={!source.url}
          variant="ghost"
          onPress={() => setIsPreviewOpen(true)}
        >
          {attachmentPreview}
        </Button>
        {source.url ? (
          <AttachmentPreviewDialog
            contentType={item.contentType}
            fileName={fileName}
            isOpen={isPreviewOpen}
            size={item.size}
            src={source.url}
            onOpenChange={setIsPreviewOpen}
          />
        ) : null}
      </>
    ) : source.url ? (
      <a
        aria-label={`${t("clinic.procedures.items.download")} ${fileName}`}
        className="inline-flex max-w-full cursor-[var(--cursor-interactive)] rounded-2xl no-underline"
        download={fileName}
        href={source.url}
      >
        {attachmentPreview}
      </a>
    ) : (
      <div aria-busy={source.loading} className="inline-flex max-w-full">
        {attachmentPreview}
      </div>
    );
  const time = (
    <time className="text-muted text-sm tabular-nums" dateTime={item.createdAt}>
      {formatDate(item.createdAt, locale, { timeStyle: "short" })}
    </time>
  );
  const message = (
    <article
      aria-label={authorLabel}
      className={`flex w-full gap-3 ${isCurrentUser ? "flex-row-reverse" : ""}`}
    >
      {showAuthor ? (
        <Avatar
          color="default"
          size="sm"
          variant="soft"
          className="bg-surface-secondary mt-1 shrink-0"
        >
          <Avatar.Fallback>
            <FaIcon
              icon={isCurrentUser ? "user" : "user-doctor"}
              aria-hidden="true"
            />
          </Avatar.Fallback>
        </Avatar>
      ) : (
        <span aria-hidden="true" className="w-8 shrink-0" />
      )}
      <div
        className={`flex min-w-0 max-w-[min(36rem,85%)] flex-col gap-1 ${isCurrentUser ? "items-end" : "items-start"}`}
      >
        {showAuthor ? (
          <div
            className={`flex flex-wrap items-baseline gap-2 ${isCurrentUser ? "flex-row-reverse" : ""}`}
          >
            <span className="text-foreground text-sm font-semibold">
              {authorLabel}
            </span>
            {time}
          </div>
        ) : null}
        {attachment ?? (
          <p
            dir="auto"
            className={`text-foreground m-0 whitespace-pre-wrap rounded-3xl px-4 py-2.5 text-base leading-relaxed ${isCurrentUser ? "border-separator bg-surface rounded-se-md border" : "bg-surface-secondary rounded-ss-md"}`}
          >
            {item.content}
          </p>
        )}
        {!showAuthor ? time : null}
      </div>
    </article>
  );

  if (!canWrite) return message;
  return (
    <ContextMenu isDisabled={isDeleting}>
      <ContextMenu.Trigger>{message}</ContextMenu.Trigger>
      <ContextMenu.Popover>
        <ContextMenu.Menu
          aria-label={t("clinic.procedures.items.moreActions")}
          onAction={(key) => key === "delete" && onDelete()}
        >
          <MenuItem
            className="text-danger flex items-center gap-2 px-3 py-2 text-base"
            id="delete"
            textValue={t("clinic.procedures.items.delete")}
          >
            <FaIcon icon="trash-can" aria-hidden="true" className="size-4" />
            <span>{t("clinic.procedures.items.delete")}</span>
          </MenuItem>
        </ContextMenu.Menu>
      </ContextMenu.Popover>
    </ContextMenu>
  );
}
