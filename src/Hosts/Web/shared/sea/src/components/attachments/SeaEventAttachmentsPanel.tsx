"use client";

import { useRef, useState } from "react";
import { Button, Label, TextArea, TextField, Tooltip } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { SeaLoadingState, SeaEmptyState } from "../state/SeaStateSurface";
import { useSeaEventAttachments } from "../../hooks/attachments/useSeaAttachments";
import {
  useCreateSeaFileAttachment,
  useCreateSeaTextAttachment,
  useDeleteSeaAttachment,
} from "../../hooks/attachments/useSeaAttachmentMutations";
import { useLocalizedText } from "../../localization";
import { toSeaFileAttachment } from "../../api/attachments/seaAttachmentFile";
import type { SeaAttachmentReadDto } from "../../api/attachments/seaAttachmentsApi";
import { FaIcon } from "@vitarish/ui";

interface SeaEventAttachmentsPanelProps {
  eventId: string;
}

export function SeaEventAttachmentsPanel({
  eventId,
}: SeaEventAttachmentsPanelProps) {
  const { t } = useLocalizedText();
  const { data: attachments, isLoading } = useSeaEventAttachments(eventId);
  const createTextMutation = useCreateSeaTextAttachment();
  const createFileMutation = useCreateSeaFileAttachment();
  const deleteMutation = useDeleteSeaAttachment();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [newText, setNewText] = useState("");
  const [isAdding, setIsAdding] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<SeaAttachmentReadDto | null>(
    null,
  );
  const [fileError, setFileError] = useState<string | null>(null);

  const handleAddText = () => {
    if (!newText.trim()) return;
    createTextMutation.mutate(
      {
        eventId,
        content: newText.trim(),
        contentType: "text/plain",
        size: new TextEncoder().encode(newText.trim()).length,
        kind: 0,
      },
      {
        onSuccess: () => {
          setNewText("");
          setIsAdding(false);
        },
      },
    );
  };

  const handleDeleteAttachment = () => {
    if (!deleteTarget) return;

    deleteMutation.mutate(deleteTarget.id, {
      onSuccess: () => setDeleteTarget(null),
    });
  };

  const uploadFile = async (file: File | null | undefined) => {
    if (!file) return;

    setFileError(null);

    try {
      const attachment = await toSeaFileAttachment(eventId, file);
      createFileMutation.mutate(attachment);
    } catch {
      setFileError(t("sea.attachments.fileReadFailed"));
    }
  };

  if (isLoading) {
    return <SeaLoadingState label={t("sea.attachments.loading")} />;
  }

  const attachmentList = attachments ?? [];

  return (
    <Widget>
      <Widget.Header>
        <div className="flex items-center justify-between">
          <Widget.Title>{t("sea.attachments.title")}</Widget.Title>
          <Tooltip><Button size="sm" variant="secondary" onPress={() => setIsAdding(true)}><FaIcon icon="text" aria-hidden="true" />{t("sea.attachments.addText")}</Button><Tooltip.Content>{t("sea.attachments.addText")}</Tooltip.Content></Tooltip>
        </div>
      </Widget.Header>
      <Widget.Content className="flex flex-col gap-3">
        <div
          className="flex min-h-32 flex-col items-center justify-center gap-2 rounded-lg border border-dashed border-border p-4 text-center"
          onDragOver={(event) => event.preventDefault()}
          onDrop={(event) => {
            event.preventDefault();
            void uploadFile(event.dataTransfer.files.item(0));
          }}
        >
          <input
            ref={fileInputRef}
            className="sr-only"
            type="file"
            onChange={(event) => {
              void uploadFile(event.target.files?.item(0));
              event.currentTarget.value = "";
            }}
          />
          <p className="text-sm font-medium">
            {t("sea.attachments.dropzoneTitle")}
          </p>
          <p className="text-xs text-muted">
            {t("sea.attachments.dropzoneDescription")}
          </p>
          <Tooltip><Button
            size="sm"
            variant="secondary"
            onPress={() => fileInputRef.current?.click()}
            isDisabled={createFileMutation.isPending}
          ><FaIcon icon="paperclip" aria-hidden="true" />
            {createFileMutation.isPending
              ? t("common.states.saving")
              : t("sea.attachments.addFile")}
          </Button><Tooltip.Content>{t("sea.attachments.addFile")}</Tooltip.Content></Tooltip>
          {fileError && <p className="text-xs text-danger">{fileError}</p>}
        </div>

        {isAdding && (
          <div className="flex flex-col gap-2 rounded-lg border border-border p-3">
            <TextField value={newText} onChange={setNewText}>
              <Label>{t("sea.attachments.title")}</Label>
              <TextArea placeholder={t("sea.attachments.textPlaceholder")} />
            </TextField>
            <div className="flex justify-end gap-2">
              <Tooltip><Button
                size="sm"
                variant="ghost"
                onPress={() => {
                  setIsAdding(false);
                  setNewText("");
                }}
              ><FaIcon icon="xmark" aria-hidden="true" />
                {t("common.actions.cancel")}
              </Button><Tooltip.Content>{t("common.actions.cancel")}</Tooltip.Content></Tooltip>
              <Tooltip><Button
                size="sm"
                variant="primary"
                onPress={handleAddText}
                isDisabled={!newText.trim() || createTextMutation.isPending}
              ><FaIcon icon="check" aria-hidden="true" />
                {t("common.actions.save")}
              </Button><Tooltip.Content>{t("common.actions.save")}</Tooltip.Content></Tooltip>
            </div>
          </div>
        )}

        {attachmentList.length === 0 && !isAdding ? (
          <SeaEmptyState title={t("sea.attachments.emptyState")} />
        ) : (
          attachmentList.map((attachment) => (
            <AttachmentItem
              key={attachment.id}
              attachment={attachment}
              onDelete={() => setDeleteTarget(attachment)}
              isDeleting={deleteMutation.isPending}
              t={t}
            />
          ))
        )}

        {deleteTarget && (
          <div className="rounded-lg border border-danger/20 bg-danger/5 p-4">
            <p className="text-sm mb-2">{t("sea.attachments.confirmDelete")}</p>
            <p className="text-sm text-muted mb-3">
              {deleteTarget.contentType} - {deleteTarget.size}{" "}
              {t("sea.attachments.byteUnit")}
            </p>
            <div className="flex gap-2">
              <Tooltip><Button
                size="sm"
                variant="ghost"
                onPress={() => setDeleteTarget(null)}
                isDisabled={deleteMutation.isPending}
              ><FaIcon icon="xmark" aria-hidden="true" />
                {t("common.actions.cancel")}
              </Button><Tooltip.Content>{t("common.actions.cancel")}</Tooltip.Content></Tooltip>
              <Tooltip><Button
                size="sm"
                variant="danger"
                onPress={handleDeleteAttachment}
                isDisabled={deleteMutation.isPending}
              ><FaIcon icon="trash" aria-hidden="true" />
                {deleteMutation.isPending
                  ? t("common.states.saving")
                  : t("common.actions.delete")}
              </Button><Tooltip.Content>{t("common.actions.delete")}</Tooltip.Content></Tooltip>
            </div>
          </div>
        )}
      </Widget.Content>
    </Widget>
  );
}

function AttachmentItem({
  attachment,
  onDelete,
  isDeleting,
  t,
}: {
  attachment: SeaAttachmentReadDto;
  onDelete: () => void;
  isDeleting: boolean;
  t: (key: string) => string;
}) {
  const isTextAttachment = attachment.contentType?.startsWith("text/");
  const fileHref = isTextAttachment ? null : attachment.content;

  return (
    <div className="flex items-start justify-between gap-3 rounded-lg bg-surface-secondary/70 p-3">
      <div className="min-w-0 flex-1">
        {isTextAttachment ? (
          <p className="text-sm whitespace-pre-wrap">{attachment.content}</p>
        ) : (
          <a
            className="text-sm font-medium text-accent underline-offset-2 hover:underline"
            href={fileHref ?? "#"}
            target="_blank"
            rel="noreferrer"
          >
            {t("sea.attachments.openFile")}
          </a>
        )}
        <p className="text-xs text-muted mt-1">
          {attachment.contentType} - {attachment.size}{" "}
          {t("sea.attachments.byteUnit")}
        </p>
      </div>
      <Tooltip><Button
        size="sm"
        variant="danger-soft"
        onPress={onDelete}
        isDisabled={isDeleting}
        aria-label={t("common.actions.delete")}
        isIconOnly
      ><FaIcon icon="trash" aria-hidden="true" /></Button><Tooltip.Content>{t("common.actions.delete")}</Tooltip.Content></Tooltip>
    </div>
  );
}
