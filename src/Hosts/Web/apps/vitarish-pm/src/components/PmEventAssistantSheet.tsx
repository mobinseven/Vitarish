"use client";

import * as React from "react";
import { Button, Tooltip } from "@heroui/react";
import { Sheet } from "@heroui/react";
import { ChatConversation, ChatMessage } from "@heroui-pro/react";
import { useQueryClient } from "@tanstack/react-query";
import {
  AgentConversationComposer,
  intelligenceRunAgentConversation,
  useConversationController,
} from "@vitarish/intelligence";
import {
  checkFileForUpload,
  fileUploadAccept,
  FileUploadRejectedError,
  toSeaFileAttachment,
  useCreateSeaFileAttachment,
  useFileUploadPolicy,
} from "@vitarish/sea";
import {
  formatFileSize,
  formatList,
  useLocalizedText,
} from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import {
  ProjectManagementMessageParts,
  type ProjectManagementDisplayPart,
} from "./ProjectManagementMessageParts";
import {
  projectManagementConfirmEventAction,
  projectManagementDeleteEventConversation,
  projectManagementGetEventAction,
  projectManagementGetEventConversation,
  projectManagementRejectEventAction,
} from "../api/generated/project-management/projectManagement";
import type { ProjectManagementActionResponse } from "../api/generated/project-management/model";

type Action = ProjectManagementActionResponse;
type AgentResult = {
  assistant?: { parts?: unknown[]; messages?: unknown[] };
  preparedActions?: { actionId: string; operation: string }[];
};

export function PmEventAssistantSheet({
  eventId,
  onDeleted,
}: {
  eventId: string;
  onDeleted: () => void;
}) {
  const { direction, locale, t } = useLocalizedText();
  const queryClient = useQueryClient();
  const [open, setOpen] = React.useState(false);
  const [draft, setDraft] = React.useState("");
  const [messages, setMessages] = React.useState<
    {
      role: "user" | "assistant";
      text: string;
      parts?: ProjectManagementDisplayPart[];
      actionIds?: string[];
    }[]
  >([]);
  const [actions, setActions] = React.useState<Action[]>([]);
  const [files, setFiles] = React.useState<File[]>([]);
  const [rejection, setRejection] =
    React.useState<FileUploadRejectedError | null>(null);
  const [resetPending, setResetPending] = React.useState(false);
  const [conversationId, setConversationId] = React.useState<string | null>(
    null,
  );
  const restoredOnOpen = React.useRef(false);
  const fileInput = React.useRef<HTMLInputElement>(null);
  const upload = useCreateSeaFileAttachment();
  const { data: uploadPolicy } = useFileUploadPolicy();
  const deleteConversation = React.useCallback(
    (conversationId: string) =>
      projectManagementDeleteEventConversation(eventId, conversationId),
    [eventId],
  );
  const handleConversationIdChange = React.useCallback(
    (nextId: string | null) => {
      setConversationId(nextId);
      if (!nextId) {
        setMessages([]);
        setActions([]);
      }
    },
    [],
  );
  const conversation = useConversationController<AgentResult>({
    ownerKey: eventId,
    isOpen: open,
    run: async (message, conversationId, signal) => {
      const response = await intelligenceRunAgentConversation(
        {
          agentKey: "project-management.event",
          subjectKind: "event",
          subjectId: eventId,
          message,
          conversationId,
        },
        undefined,
        signal,
      );
      return {
        conversationId: response.conversationId,
        result: response.result as AgentResult,
      };
    },
    deleteConversation,
    resetOnClose: false,
    onConversationIdChange: handleConversationIdChange,
    isNotFound: (error) =>
      typeof error === "object" &&
      error !== null &&
      "status" in error &&
      error.status === 404,
  });
  React.useEffect(() => {
    if (!open) {
      restoredOnOpen.current = false;
      return;
    }
    if (restoredOnOpen.current) return;
    restoredOnOpen.current = true;
    if (!conversationId) return;
    let cancelled = false;
    void projectManagementGetEventConversation(eventId, conversationId)
      .then(async (display) => {
        if (cancelled || !display.turns) return;
        const restored = display.turns.flatMap((turn) => [
          { role: "user" as const, text: turn.user },
          {
            role: "assistant" as const,
            text: turn.assistant,
            parts: turn.parts as ProjectManagementDisplayPart[],
            actionIds: turn.preparedActionIds,
          },
        ]);
        setMessages(restored);
        const restoredActions = await Promise.all(
          turnsActionIds(display.turns).map((actionId) =>
            projectManagementGetEventAction(eventId, actionId),
          ),
        );
        if (!cancelled) setActions(restoredActions);
      })
      .catch(() => undefined);
    return () => {
      cancelled = true;
    };
  }, [conversationId, eventId, open]);
  const send = async (nextDraft = draft) => {
    const message = nextDraft.trim();
    if (!message) return;
    setDraft("");
    setMessages((current) => [...current, { role: "user", text: message }]);
    const result = await conversation.submit(message);
    if (!result) return;
    const suppliedParts = normalizeParts(result.assistant);
    const text =
      suppliedParts
        .filter(
          (
            item,
          ): item is Extract<ProjectManagementDisplayPart, { type: "text" }> =>
            item.type === "text",
        )
        .map((item) => item.text)
        .join("\n") || t("pm.assistantNoResponse");
    const actionIds = (result.preparedActions ?? []).map(
      (action) => action.actionId,
    );
    setMessages((current) => [
      ...current,
      { role: "assistant", text, parts: suppliedParts, actionIds },
    ]);
    for (const proposed of result.preparedActions ?? []) {
      const action = await projectManagementGetEventAction(
        eventId,
        proposed.actionId,
      );
      setActions((current) =>
        current.some((item) => item.actionId === action.actionId)
          ? current
          : [...current, action],
      );
    }
  };
  const confirm = async (actionId: string) => {
    const action = await projectManagementConfirmEventAction(eventId, actionId);
    setActions((current) =>
      current.map((item) => (item.actionId === actionId ? action : item)),
    );
    await queryClient.invalidateQueries({ queryKey: ["sea"] });
    if (
      action.review.operation === "delete_event" &&
      action.status === "Completed"
    )
      onDeleted();
  };
  const reject = async (actionId: string) => {
    const action = await projectManagementRejectEventAction(
      eventId,
      actionId,
      {},
    );
    setActions((current) =>
      current.map((item) => (item.actionId === actionId ? action : item)),
    );
  };
  const selectFiles = (selected: File[]) => {
    const accepted: File[] = [];
    let firstRejection: FileUploadRejectedError | null = null;
    for (const candidate of selected) {
      const rejected = uploadPolicy
        ? checkFileForUpload(uploadPolicy, candidate)
        : undefined;
      if (rejected) {
        firstRejection ??= rejected;
        continue;
      }
      accepted.push(candidate);
    }
    setRejection(firstRejection);
    setFiles(accepted);
  };
  const confirmUpload = async () => {
    for (const file of files) {
      try {
        await upload.mutateAsync(
          await toSeaFileAttachment(eventId, file, uploadPolicy),
        );
        setFiles((current) => current.filter((selected) => selected !== file));
      } catch (error) {
        if (!(error instanceof FileUploadRejectedError)) throw error;
        setRejection(error);
        setFiles((current) => current.filter((selected) => selected !== file));
      }
    }
  };
  return (
    <Sheet isOpen={open} onOpenChange={setOpen}>
      <Tooltip>
        <Button size="sm" variant="secondary" onPress={() => setOpen(true)}>
          <FaIcon icon="sparkles" aria-hidden="true" />
          {t("pm.assistant")}
        </Button>
        <Tooltip.Content>{t("pm.assistant")}</Tooltip.Content>
      </Tooltip>
      <Sheet.Backdrop variant="blur">
        <Sheet.Content className="mx-auto max-h-[92vh] w-full max-w-2xl">
          <Sheet.Dialog className="flex max-h-[92vh] flex-col">
            <Sheet.Handle />
            <Sheet.CloseTrigger />
            <Sheet.Header>
              <Sheet.Heading>{t("pm.assistant")}</Sheet.Heading>
            </Sheet.Header>
            <Sheet.Body className="min-h-0 space-y-4 overflow-y-auto">
              <ChatConversation className="min-h-48">
                <ChatConversation.Content className="space-y-3">
                  {messages.map((message, index) =>
                    message.role === "user" ? (
                      <ChatMessage.User key={index}>
                        <ChatMessage.Bubble>
                          <ChatMessage.Content>
                            {message.text}
                          </ChatMessage.Content>
                        </ChatMessage.Bubble>
                      </ChatMessage.User>
                    ) : (
                      <React.Fragment key={index}>
                        <ChatMessage.Assistant>
                          <ChatMessage.Bubble>
                            <ChatMessage.Content>
                              {message.parts?.length ? (
                                <ProjectManagementMessageParts
                                  parts={message.parts}
                                  onOpen={(route) =>
                                    window.location.assign(route)
                                  }
                                />
                              ) : (
                                message.text
                              )}
                            </ChatMessage.Content>
                          </ChatMessage.Bubble>
                        </ChatMessage.Assistant>
                        {message.actionIds?.map((actionId) => {
                          const action = actions.find(
                            (item) =>
                              item.actionId.toLowerCase() ===
                              actionId.toLowerCase(),
                          );
                          return action ? (
                            <div
                              key={actionId}
                              className="rounded-2xl bg-surface-secondary p-4"
                            >
                              <p className="font-medium">
                                {action.review.operation}
                              </p>
                              <pre className="mt-2 max-h-28 overflow-auto text-xs text-muted">
                                {action.review.proposedValuesJson}
                              </pre>
                              {action.status === "Proposed" ? (
                                <div className="mt-3 flex gap-2">
                                  <Tooltip>
                                    <Button
                                      size="sm"
                                      onPress={() =>
                                        void confirm(action.actionId)
                                      }
                                    >
                                      <FaIcon icon="check" aria-hidden="true" />
                                      {t("pm.assistantConfirm")}
                                    </Button>
                                    <Tooltip.Content>
                                      {t("pm.assistantConfirm")}
                                    </Tooltip.Content>
                                  </Tooltip>
                                  <Tooltip>
                                    <Button
                                      size="sm"
                                      variant="secondary"
                                      onPress={() =>
                                        void reject(action.actionId)
                                      }
                                    >
                                      <FaIcon icon="xmark" aria-hidden="true" />
                                      {t("pm.assistantReject")}
                                    </Button>
                                    <Tooltip.Content>
                                      {t("pm.assistantReject")}
                                    </Tooltip.Content>
                                  </Tooltip>
                                </div>
                              ) : null}
                            </div>
                          ) : null;
                        })}
                      </React.Fragment>
                    ),
                  )}
                </ChatConversation.Content>
              </ChatConversation>
              {conversation.isPending && (
                <p className="text-sm text-muted">{t("pm.assistantWorking")}</p>
              )}
              {conversation.isError && (
                <Tooltip>
                  <Button
                    variant="secondary"
                    onPress={() => void conversation.retry()}
                  >
                    <FaIcon icon="rotate-right" aria-hidden="true" />
                    {t("pm.assistantRetry")}
                  </Button>
                  <Tooltip.Content>{t("pm.assistantRetry")}</Tooltip.Content>
                </Tooltip>
              )}
              {files.length > 0 && (
                <div className="rounded-2xl bg-surface-secondary p-3">
                  <p className="text-sm">
                    {files
                      .map(
                        (file) =>
                          `${file.name} · ${file.type || t("pm.fileUnknownType")} · ${file.size}`,
                      )
                      .join("\n")}
                  </p>
                  {upload.isError && !rejection && (
                    <p className="mt-2 text-sm text-danger" role="alert">
                      {t("common.states.error")}
                    </p>
                  )}
                  <Tooltip>
                    <Button
                      className="mt-3"
                      size="sm"
                      onPress={() => void confirmUpload()}
                      isPending={upload.isPending}
                    >
                      <FaIcon icon="upload" aria-hidden="true" />
                      {t("pm.assistantUpload")}
                    </Button>
                    <Tooltip.Content>{t("pm.assistantUpload")}</Tooltip.Content>
                  </Tooltip>
                </div>
              )}
              {rejection && (
                <p className="text-sm text-danger" role="alert">
                  {rejection.reason === "tooLarge"
                    ? t("pm.fileTooLarge", {
                        size: formatFileSize(
                          rejection.maximumContentBytes,
                          locale,
                        ),
                      })
                    : t("pm.fileTypeNotAllowed", {
                        types: uploadPolicy
                          ? formatList(
                              uploadPolicy.formats.flatMap((format) =>
                                format.extensions.map((extension) =>
                                  extension.slice(1).toUpperCase(),
                                ),
                              ),
                              locale,
                            )
                          : "",
                      })}
                </p>
              )}
            </Sheet.Body>
            <Sheet.Footer className="flex-col items-stretch gap-2">
              <input
                ref={fileInput}
                className="sr-only"
                type="file"
                multiple
                accept={
                  uploadPolicy ? fileUploadAccept(uploadPolicy) : undefined
                }
                onChange={(event) =>
                  selectFiles(Array.from(event.target.files ?? []))
                }
              />
              <AgentConversationComposer
                direction={direction}
                enabled={!conversation.isPending}
                isPending={conversation.isPending}
                label={t("pm.assistantComposer")}
                placeholder={t("pm.assistantComposer")}
                preferredLanguage={locale === "fa" ? "fa-IR" : "en-US"}
                sessionKey={`pm.event:${eventId}`}
                submitLabel={t("pm.assistantSend")}
                value={draft}
                toolbarStart={
                  <Tooltip>
                    <Button
                      aria-label={t("pm.assistantFiles")}
                      size="sm"
                      variant="ghost"
                      onPress={() => fileInput.current?.click()}
                    >
                      <FaIcon icon="paperclip" aria-hidden="true" />
                    </Button>
                    <Tooltip.Content>{t("pm.assistantFiles")}</Tooltip.Content>
                  </Tooltip>
                }
                onChange={setDraft}
                onSubmit={send}
              />
              {resetPending ? (
                <div className="flex gap-2">
                  <Tooltip>
                    <Button
                      variant="danger"
                      onPress={() => {
                        void conversation.deleteCurrent();
                        setResetPending(false);
                      }}
                    >
                      <FaIcon icon="rotate-left" aria-hidden="true" />
                      {t("pm.assistantResetConfirm")}
                    </Button>
                    <Tooltip.Content>
                      {t("pm.assistantResetConfirm")}
                    </Tooltip.Content>
                  </Tooltip>
                  <Tooltip>
                    <Button
                      variant="secondary"
                      onPress={() => setResetPending(false)}
                    >
                      <FaIcon icon="xmark" aria-hidden="true" />
                      {t("common.actions.cancel")}
                    </Button>
                    <Tooltip.Content>
                      {t("common.actions.cancel")}
                    </Tooltip.Content>
                  </Tooltip>
                </div>
              ) : (
                <Tooltip>
                  <Button variant="ghost" onPress={() => setResetPending(true)}>
                    <FaIcon icon="rotate-left" aria-hidden="true" />
                    {t("pm.assistantReset")}
                  </Button>
                  <Tooltip.Content>{t("pm.assistantReset")}</Tooltip.Content>
                </Tooltip>
              )}
            </Sheet.Footer>
          </Sheet.Dialog>
        </Sheet.Content>
      </Sheet.Backdrop>
    </Sheet>
  );
}

function isDisplayPart(value: unknown): value is ProjectManagementDisplayPart {
  if (!value || typeof value !== "object") return false;
  const part = value as { type?: unknown };
  return part.type === "text" || part.type === "resource-card";
}

function normalizeParts(
  assistant: AgentResult["assistant"],
): ProjectManagementDisplayPart[] {
  const raw = Array.isArray(assistant?.parts)
    ? assistant.parts
    : assistant?.messages;
  if (!Array.isArray(raw)) return [];
  return raw.flatMap((value) => {
    if (isDisplayPart(value)) return [value];
    if (typeof value === "string")
      return [
        { type: "text", text: value } satisfies ProjectManagementDisplayPart,
      ];
    if (value && typeof value === "object") {
      const item = value as {
        text?: unknown;
        content?: unknown;
        message?: unknown;
      };
      const text = [item.text, item.content, item.message].find(
        (candidate): candidate is string =>
          typeof candidate === "string" && candidate.trim().length > 0,
      );
      if (text)
        return [{ type: "text", text } satisfies ProjectManagementDisplayPart];
    }
    return [];
  });
}

function turnsActionIds(turns: { preparedActionIds: string[] }[]): string[] {
  return Array.from(new Set(turns.flatMap((turn) => turn.preparedActionIds)));
}
