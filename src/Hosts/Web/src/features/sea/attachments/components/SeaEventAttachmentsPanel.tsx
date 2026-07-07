"use client";

import { useRef, useState } from "react";
import { Button, Label, TextArea, TextField } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { SeaLoadingState, SeaEmptyState } from "../../shared/components/SeaStateSurface";
import { useSeaEventAttachments } from "../hooks/useSeaAttachments";
import { useCreateSeaFileAttachment, useCreateSeaTextAttachment, useDeleteSeaAttachment } from "../hooks/useSeaAttachmentMutations";
import { useLocalizedText } from "@/localization";
import { toSeaFileAttachment } from "../api/seaAttachmentFile";
import type { SeaAttachmentReadDto } from "../api/seaAttachmentsApi";

interface SeaEventAttachmentsPanelProps {
  eventId: string;
}

export function SeaEventAttachmentsPanel({ eventId }: SeaEventAttachmentsPanelProps) {
  const { t } = useLocalizedText();
  const { data: attachments, isLoading } = useSeaEventAttachments(eventId);
  const createTextMutation = useCreateSeaTextAttachment();
  const createFileMutation = useCreateSeaFileAttachment();
  const deleteMutation = useDeleteSeaAttachment();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [newText, setNewText] = useState("");
  const [isAdding, setIsAdding] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<SeaAttachmentReadDto | null>(null);
  const [fileError, setFileError] = useState<string | null>(null);

  const handleAddText = () => {
    if (!newText.trim()) return;
    createTextMutation.mutate(
      {
        eventId,
        content: newText.trim(),
        contentType: "text/plain",
        size: new TextEncoder().encode(newText.trim()).length,
      },
      {
        onSuccess: () => {
          setNewText("");
          setIsAdding(false);
        },
      }
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
          <Button size="sm" variant="ghost" onPress={() => setIsAdding(true)}>
            {t("sea.attachments.addText")}
          </Button>
        </div>
      </Widget.Header>
      <Widget.Content className="flex flex-col gap-3">
        <div
          className="flex flex-col items-center justify-center gap-2 rounded-lg border border-dashed border-border p-4 text-center"
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
          <p className="text-sm font-medium">{t("sea.attachments.dropzoneTitle")}</p>
          <p className="text-xs text-muted">{t("sea.attachments.dropzoneDescription")}</p>
          <Button
            size="sm"
            variant="ghost"
            onPress={() => fileInputRef.current?.click()}
            isDisabled={createFileMutation.isPending}
          >
            {createFileMutation.isPending ? t("common.states.saving") : t("sea.attachments.addFile")}
          </Button>
          {fileError && <p className="text-xs text-danger">{fileError}</p>}
        </div>

        {isAdding && (
          <div className="flex flex-col gap-2 rounded-lg border border-border p-3">
            <TextField value={newText} onChange={setNewText}>
              <Label>{t("sea.attachments.title")}</Label>
              <TextArea placeholder={t("sea.attachments.textPlaceholder")} />
            </TextField>
            <div className="flex justify-end gap-2">
              <Button size="sm" variant="ghost" onPress={() => { setIsAdding(false); setNewText(""); }}>
                {t("common.actions.cancel")}
              </Button>
              <Button
                size="sm"
                variant="primary"
                onPress={handleAddText}
                isDisabled={!newText.trim() || createTextMutation.isPending}
              >
                {t("common.actions.save")}
              </Button>
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
              {deleteTarget.contentType} - {deleteTarget.size} {t("sea.attachments.byteUnit")}
            </p>
            <div className="flex gap-2">
              <Button
                size="sm"
                variant="ghost"
                onPress={() => setDeleteTarget(null)}
                isDisabled={deleteMutation.isPending}
              >
                {t("common.actions.cancel")}
              </Button>
              <Button
                size="sm"
                variant="danger"
                onPress={handleDeleteAttachment}
                isDisabled={deleteMutation.isPending}
              >
                {deleteMutation.isPending ? t("common.states.saving") : t("common.actions.delete")}
              </Button>
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
  return (
    <div className="flex items-start justify-between rounded-lg border border-border p-3">
      <div className="min-w-0 flex-1">
        <p className="text-sm whitespace-pre-wrap">{attachment.content}</p>
        <p className="text-xs text-muted mt-1">
          {attachment.contentType} - {attachment.size} {t("sea.attachments.byteUnit")}
        </p>
      </div>
      <Button
        size="sm"
        variant="ghost"
        onPress={onDelete}
        isDisabled={isDeleting}
      >
        {t("common.actions.delete")}
      </Button>
    </div>
  );
}
