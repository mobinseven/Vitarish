"use client";

import { useState } from "react";
import { Avatar, Button, Modal } from "@heroui/react";
import {
  ChatAttachment,
  ChatAttachmentGroup,
  ChatMessage,
  ChatMessageActions,
  ChatSource,
  ContextMenu,
  EmptyState,
} from "@heroui-pro/react";
import { MenuItem } from "react-aria-components";
import { FaIcon } from "@vitarish/ui";
import { isStoredSeaAttachmentContent } from "@vitarish/sea";
import { usePatientProcedureAttachmentSource } from "../../../hooks/patients/procedures/usePatientProcedureAttachmentSource";
import {
  formatDateTime,
  formatFileSize,
  useLocalizedText,
} from "../../../localization";
import type {
  EventItemSummary,
  ProcedureEventViewer,
} from "../../../models/patients/procedures/PatientProcedureEventItemModels";

export interface PatientProcedureEventItemMessageProps {
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
            <dl className="text-muted flex flex-wrap gap-x-6 gap-y-2 text-xs">
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
            <Button slot="close" variant="secondary">
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
    ? (viewer?.label ?? t("clinic.procedures.items.you"))
    : (item.authorName ?? t("clinic.procedures.items.careTeam"));
  const fileName =
    item.fileName ?? t("clinic.procedures.items.genericAttachment");
  const metadata = (
    <div
      className={`text-muted flex min-w-0 flex-wrap items-center gap-2 text-xs ${isCurrentUser ? "justify-end" : "justify-start"}`}
    >
      <span className="text-foreground font-medium">{authorLabel}</span>
      <span aria-hidden="true">·</span>
      <time className="tabular-nums" dateTime={item.createdAt}>
        {formatDateTime(item.createdAt, locale)}
      </time>
    </div>
  );
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
  const body = attachment ? (
    <ChatAttachmentGroup className={isCurrentUser ? "justify-end" : undefined}>
      {attachment}
    </ChatAttachmentGroup>
  ) : (
    <ChatMessage.Content>
      <p className="m-0 whitespace-pre-wrap text-sm leading-relaxed">
        {item.content}
      </p>
    </ChatMessage.Content>
  );
  const attachmentError = source.error ? null : null;
  const message = isCurrentUser ? (
    <ChatMessage.User id={item.id}>
      {attachment ? body : <ChatMessage.Bubble>{body}</ChatMessage.Bubble>}
      {attachmentError}
    </ChatMessage.User>
  ) : (
    <ChatMessage.Assistant id={item.id}>
      <ChatMessage.Body>
        {showAuthor ? (
          <ChatSource sourceType="document" title={item.authorName} />
        ) : null}
        {body}
        {attachmentError}
      </ChatMessage.Body>
    </ChatMessage.Assistant>
  );

  if (!canWrite && isCurrentUser) return message;
  return (
    <ContextMenu isDisabled={canWrite && isDeleting}>
      <ContextMenu.Trigger>{message}</ContextMenu.Trigger>
      <ContextMenu.Popover>
        <ContextMenu.Menu
          aria-label={t("clinic.procedures.items.moreActions")}
          onAction={
            canWrite ? (key) => key === "delete" && onDelete() : undefined
          }
        >
          {!isCurrentUser ? (
            <>
              <MenuItem
                className="cursor-default px-3 py-2"
                id="metadata"
                isDisabled
                textValue={authorLabel}
              >
                {metadata}
              </MenuItem>
              {canWrite ? <ContextMenu.Separator /> : null}
            </>
          ) : null}
          {canWrite ? (
            <MenuItem
              className="text-danger flex items-center gap-2 px-3 py-2 text-sm"
              id="delete"
              textValue={t("clinic.procedures.items.delete")}
            >
              <FaIcon icon="trash-can" aria-hidden="true" className="size-4" />
              <span>{t("clinic.procedures.items.delete")}</span>
            </MenuItem>
          ) : null}
        </ContextMenu.Menu>
      </ContextMenu.Popover>
    </ContextMenu>
  );
}
