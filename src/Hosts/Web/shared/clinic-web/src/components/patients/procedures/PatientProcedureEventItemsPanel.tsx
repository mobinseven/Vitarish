"use client";

import { useEffect, useState } from "react";
import { Button, Label, Modal } from "@heroui/react";
import {
  ChatAttachment,
  ChatAttachmentGroup,
  ChatAttachmentInput,
  ChatConversation,
  ChatMessage,
  ContextMenu,
  PromptInput,
} from "@heroui-pro/react";
import { MenuItem } from "react-aria-components";
import {
  isStoredSeaAttachmentContent,
  loadStoredSeaAttachmentContent,
  toSeaFileAttachment,
} from "@vitarish/sea-web";
import {
  useCreatePatientProcedureEventItemFile,
  useCreatePatientProcedureEventItemText,
  useDeletePatientProcedureEventItem,
} from "../../../hooks/patients/procedures/usePatientProcedureEventItemMutations";
import { usePatientProcedureEventItems } from "../../../hooks/patients/procedures/usePatientProcedureEventItems";
import { formatDateTime, formatFileSize, useLocalizedText } from "../../../localization";
import type {
  EventItemSummary,
  ProcedureEventViewer,
} from "../../../models/patients/procedures/PatientProcedureEventItemModels";

interface PatientProcedureEventItemsPanelProps {
  eventId: string;
  canWrite: boolean;
  viewer?: ProcedureEventViewer;
}

interface PatientProcedureEventItemMessageProps {
  canWrite: boolean;
  isDeleting: boolean;
  item: EventItemSummary;
  viewer?: ProcedureEventViewer;
  onDelete: () => void;
}

interface AttachmentSourceState {
  error: boolean;
  loading: boolean;
  url?: string;
}

function useAttachmentSource(content: string, resolveStoredContent: boolean): AttachmentSourceState {
  const isStoredContent = resolveStoredContent && isStoredSeaAttachmentContent(content);
  const [state, setState] = useState<AttachmentSourceState>(() => ({
    error: false,
    loading: isStoredContent,
    url: isStoredContent ? undefined : content,
  }));

  useEffect(() => {
    if (!isStoredContent) {
      setState({ error: false, loading: false, url: content });
      return;
    }

    const controller = new AbortController();
    let objectUrl: string | undefined;
    setState({ error: false, loading: true });

    void loadStoredSeaAttachmentContent(content, controller.signal)
      .then((blob) => {
        if (controller.signal.aborted) return;
        objectUrl = URL.createObjectURL(blob);
        setState({ error: false, loading: false, url: objectUrl });
      })
      .catch((error: unknown) => {
        if (
          controller.signal.aborted ||
          (error instanceof DOMException && error.name === "AbortError")
        ) {
          return;
        }
        setState({ error: true, loading: false });
      });

    return () => {
      controller.abort();
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [content, isStoredContent]);

  return state;
}

function downloadAttachment(url: string, fileName: string): void {
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = fileName;
  anchor.style.display = "none";
  document.body.append(anchor);
  anchor.click();
  anchor.remove();
}

interface AttachmentImagePreviewProps {
  contentType?: string;
  fileName: string;
  isOpen: boolean;
  size?: number;
  src: string;
  onOpenChange: (isOpen: boolean) => void;
}

function AttachmentImagePreview({
  contentType,
  fileName,
  isOpen,
  size,
  src,
  onOpenChange,
}: AttachmentImagePreviewProps) {
  const { t, locale } = useLocalizedText();

  return (
    <Modal.Backdrop isOpen={isOpen} variant="blur" onOpenChange={onOpenChange}>
      <Modal.Container placement="center" size="cover">
        <Modal.Dialog aria-label={fileName} className="sm:max-w-5xl">
          <Modal.CloseTrigger aria-label={t("clinic.procedures.items.closePreview")} />
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
                <audio aria-label={fileName} className="w-full max-w-xl" controls src={src} />
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
                  <dd className="text-foreground m-0">{formatFileSize(size, locale)}</dd>
                </div>
              )}
            </dl>
          </Modal.Body>
          <Modal.Footer>
            <Button slot="close" variant="secondary">
              {t("clinic.procedures.items.closePreview")}
            </Button>
            <Button onPress={() => downloadAttachment(src, fileName)}>
              <i aria-hidden="true" className="fa-solid fa-download size-4" />
              {t("clinic.procedures.items.download")}
            </Button>
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
  viewer,
  onDelete,
}: PatientProcedureEventItemMessageProps) {
  const { t, locale } = useLocalizedText();
  const [isPreviewOpen, setIsPreviewOpen] = useState(false);
  const source = useAttachmentSource(item.content, item.type !== "note");
  const isCurrentUser = Boolean(viewer?.id && item.authorId === viewer.id);
  const authorLabel = isCurrentUser
    ? viewer?.label ?? t("clinic.procedures.items.you")
    : item.authorName ?? t("clinic.procedures.items.careTeam");
  const fileName = item.fileName ?? t("clinic.procedures.items.genericAttachment");
  const metadata = (
    <div
      className={`text-muted flex min-w-0 flex-wrap items-center gap-2 text-xs ${
        isCurrentUser ? "justify-end" : "justify-start"
      }`}
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
          ? item.contentThumbnail && !isStoredSeaAttachmentContent(item.contentThumbnail)
            ? item.contentThumbnail
            : source.url
          : undefined
      }
    />
  );
  const attachment = item.type === "note" ? null : item.type === "media" ? (
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
        <AttachmentImagePreview
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
  const attachmentError = source.error ? (
    <span className="text-danger text-xs" role="alert">
      {t("clinic.procedures.items.attachmentUnavailable")}
    </span>
  ) : null;
  const message = isCurrentUser ? (
      <ChatMessage.User id={item.id} role="article">
        {attachment ? (
          <ChatAttachmentGroup className="justify-end">{attachment}</ChatAttachmentGroup>
        ) : (
          <ChatMessage.Bubble>
            <ChatMessage.Content>
              <p className="m-0 whitespace-pre-wrap text-sm leading-relaxed">{item.content}</p>
            </ChatMessage.Content>
          </ChatMessage.Bubble>
        )}
        {attachmentError}
      </ChatMessage.User>
  ) : (
    <ChatMessage.Assistant id={item.id} role="article">
      <ChatMessage.Avatar
        show
        alt={authorLabel}
        fallback={authorLabel}
      />
      <ChatMessage.Body>
        {attachment ? (
          <ChatAttachmentGroup>{attachment}</ChatAttachmentGroup>
        ) : (
          <ChatMessage.Content>
            <p className="m-0 whitespace-pre-wrap text-sm leading-relaxed">{item.content}</p>
          </ChatMessage.Content>
        )}
        {attachmentError}
      </ChatMessage.Body>
    </ChatMessage.Assistant>
  );

  const hasContextMenu = canWrite || !isCurrentUser;

  return hasContextMenu ? (
    <ContextMenu isDisabled={canWrite && isDeleting}>
      <ContextMenu.Trigger>{message}</ContextMenu.Trigger>
      <ContextMenu.Popover>
        <ContextMenu.Menu
          aria-label={t("clinic.procedures.items.moreActions")}
          onAction={canWrite ? (key) => key === "delete" && onDelete() : undefined}
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
              <i aria-hidden="true" className="fa-solid fa-trash-can size-4" />
              <Label>{t("clinic.procedures.items.delete")}</Label>
            </MenuItem>
          ) : null}
        </ContextMenu.Menu>
      </ContextMenu.Popover>
    </ContextMenu>
  ) : (
    message
  );
}

export function PatientProcedureEventItemsPanel({
  eventId,
  canWrite,
  viewer,
}: PatientProcedureEventItemsPanelProps) {
  const { t } = useLocalizedText();
  const query = usePatientProcedureEventItems(eventId);
  const createNote = useCreatePatientProcedureEventItemText();
  const createFile = useCreatePatientProcedureEventItemFile();
  const remove = useDeletePatientProcedureEventItem();
  const [note, setNote] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | undefined>();
  const [fileFailed, setFileFailed] = useState(false);

  useEffect(() => {
    return () => {
      if (previewUrl) URL.revokeObjectURL(previewUrl);
    };
  }, [previewUrl]);

  const selectFile = (nextFile: File | undefined) => {
    setFile(nextFile ?? null);
    setFileFailed(false);
    setPreviewUrl(
      nextFile && (nextFile.type.startsWith("image/") || nextFile.type.startsWith("video/"))
        ? URL.createObjectURL(nextFile)
        : undefined,
    );
  };

  const submit = async () => {
    const content = note.trim();
    if (!content && !file) return;

    if (content) {
      try {
        await createNote.mutateAsync({
          eventId,
          content,
          contentType: "text/plain",
          size: new Blob([content]).size,
        });
        setNote("");
      } catch {
        // The localized mutation error is rendered below and the draft remains intact.
      }
    }

    if (file) {
      setFileFailed(false);
      try {
        const attachment = await toSeaFileAttachment(eventId, file);
        await createFile.mutateAsync({ ...attachment, fileName: file.name });
        selectFile(undefined);
      } catch {
        setFileFailed(true);
      }
    }
  };

  const items = [...(query.data ?? [])].sort((left, right) =>
    left.createdAt.localeCompare(right.createdAt),
  );
  const isSubmitting = createNote.isPending || createFile.isPending;

  return (
    <section
      className="flex min-h-0 flex-1 flex-col"
      aria-labelledby="clinical-evidence-title"
    >
      <div className="mb-2 shrink-0">
        <h2 id="clinical-evidence-title" className="m-0 text-lg font-semibold">
          {t("clinic.procedures.items.title")}
        </h2>
        <p className="text-muted m-0 mt-1 text-sm">
          {t("clinic.procedures.items.conversationDescription")}
        </p>
      </div>

      <ChatConversation className="min-h-0 flex-1">
        <ChatConversation.Content
          aria-label={t("clinic.procedures.items.title")}
          className="max-w-4xl flex-col gap-6 px-2 py-6 sm:px-4"
          role="feed"
        >
          {query.isLoading ? (
            <p className="text-muted text-center text-sm">{t("common.states.loading")}</p>
          ) : query.error ? (
            <div className="flex flex-col items-center gap-3 py-10" role="alert">
              <p className="text-danger m-0 text-sm">
                {t("clinic.procedures.items.loadFailed")}
              </p>
              <Button size="sm" variant="secondary" onPress={() => void query.refetch()}>
                {t("common.actions.retry")}
              </Button>
            </div>
          ) : items.length === 0 ? (
            <p className="text-muted py-10 text-center text-sm">
              {t("clinic.procedures.items.empty")}
            </p>
          ) : (
            items.map((item) => (
              <PatientProcedureEventItemMessage
                key={item.id}
                canWrite={canWrite}
                isDeleting={remove.isPending}
                item={item}
                viewer={viewer}
                onDelete={() => remove.mutate(item.id)}
              />
            ))
          )}
        </ChatConversation.Content>
        <ChatConversation.ScrollButton
          aria-label={t("clinic.procedures.items.scrollToLatest")}
          tooltip={t("clinic.procedures.items.scrollToLatest")}
        />
      </ChatConversation>

      {remove.isError ? (
        <p className="text-danger mx-auto mb-2 w-full max-w-4xl text-sm" role="alert">
          {t("clinic.procedures.items.deleteFailed")}
        </p>
      ) : null}

      {canWrite ? (
        <div className="bg-background shrink-0 pt-3">
          <PromptInput
            className="mx-auto w-full max-w-4xl"
            status={isSubmitting ? "submitted" : "ready"}
            value={note}
            variant="secondary"
            onSubmit={() => void submit()}
            onValueChange={setNote}
          >
            <ChatAttachmentInput multiple={false} onFilesSelected={(files) => selectFile(files[0])}>
              <ChatAttachmentInput.Dropzone
                render={(dropzoneProps) => (
                  <PromptInput.Shell {...dropzoneProps}>
                    <PromptInput.Content>
                      {file ? (
                        <PromptInput.Attachments>
                          <ChatAttachmentGroup>
                            <ChatAttachment
                              mimeType={file.type}
                              name={file.name}
                              src={previewUrl}
                            >
                              <ChatAttachment.Preview />
                              <ChatAttachment.Remove
                                aria-label={t("common.actions.remove")}
                                onPress={() => selectFile(undefined)}
                              />
                            </ChatAttachment>
                          </ChatAttachmentGroup>
                        </PromptInput.Attachments>
                      ) : null}
                      <PromptInput.TextArea
                        aria-label={t("clinic.procedures.items.noteLabel")}
                        placeholder={t("clinic.procedures.items.composerPlaceholder")}
                      />
                    </PromptInput.Content>
                    <PromptInput.Toolbar>
                      <PromptInput.ToolbarStart>
                        <ChatAttachmentInput.Trigger
                          render={(triggerProps) => (
                            <PromptInput.Action
                              {...triggerProps}
                              aria-label={t("clinic.procedures.items.chooseFile")}
                              tooltip={t("clinic.procedures.items.chooseFile")}
                            >
                              <i aria-hidden="true" className="fa-solid fa-paperclip size-4" />
                            </PromptInput.Action>
                          )}
                        />
                      </PromptInput.ToolbarStart>
                      <PromptInput.ToolbarEnd>
                        <PromptInput.Send
                          aria-label={t("clinic.procedures.items.send")}
                          isDisabled={isSubmitting || (!note.trim() && !file)}
                        />
                      </PromptInput.ToolbarEnd>
                    </PromptInput.Toolbar>
                  </PromptInput.Shell>
                )}
              />
            </ChatAttachmentInput>
            <PromptInput.Footer>{t("clinic.procedures.items.oneFile")}</PromptInput.Footer>
          </PromptInput>

          {createNote.isError ? (
            <p className="text-danger mx-auto mt-2 w-full max-w-4xl text-sm" role="alert">
              {t("clinic.procedures.items.saveFailed")}
            </p>
          ) : null}
          {createFile.isError || fileFailed ? (
            <p className="text-danger mx-auto mt-2 w-full max-w-4xl text-sm" role="alert">
              {t("clinic.procedures.items.uploadFailed")}
            </p>
          ) : null}
        </div>
      ) : null}
    </section>
  );
}
