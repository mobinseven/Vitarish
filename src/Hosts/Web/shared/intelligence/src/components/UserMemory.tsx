"use client";

import { useState } from "react";
import { Alert, Button, Chip, Input, Label, Skeleton, Surface, TextField, Tooltip } from "@heroui/react";
import { formatDate, useLocalizedText, type SupportedLocale } from "@vitarish/localization";
import { FaIcon, HeaderIcon } from "@vitarish/ui";
import { useUserMemory } from "../hooks/useUserMemory";
import type { InteractiveRemembered } from "../interaction/interactiveReply";

/** The longest remembered text; the server enforces the same limit. */
const maxMemoryTextLength = 280;

/**
 * A quiet note for each fact a reply remembered, with an undo that deletes it right away. The note
 * stays readable; it is not a pop-up.
 */
export function RememberedNotes({ items }: { items: readonly InteractiveRemembered[] }) {
  const { t } = useLocalizedText();
  const { forget } = useUserMemory({ enabled: false });
  const [forgotten, setForgotten] = useState<readonly string[]>([]);
  if (items.length === 0) return null;

  return (
    <ul aria-label={t("intelligence.memory.rememberedLabel")} className="flex flex-col gap-1">
      {items.map((item) => (
        <li key={item.id} className="flex flex-wrap items-center gap-2 text-muted">
          <FaIcon aria-hidden="true" icon="brain" />
          {forgotten.includes(item.id) ? (
            <span>{t("intelligence.memory.forgotten")}</span>
          ) : (
            <>
              <span>{t("intelligence.memory.remembered", { text: item.text })}</span>
              <Tooltip>
                <Button
                  isPending={forget.isPending && forget.variables === item.id}
                  size="sm"
                  variant="ghost"
                  onPress={() => forget.mutate(item.id, { onSuccess: () => setForgotten((current) => [...current, item.id]) })}
                >
                  <FaIcon aria-hidden="true" icon="rotate-left" />
                  {t("intelligence.memory.undo")}
                </Button>
                <Tooltip.Content>{t("intelligence.memory.undoTooltip")}</Tooltip.Content>
              </Tooltip>
            </>
          )}
        </li>
      ))}
      {forget.isError ? <li className="text-danger">{t("intelligence.memory.changeFailed")}</li> : null}
    </ul>
  );
}

/** "What Vitarish knows about you": every remembered fact, which the person can correct or delete. */
export function UserMemorySection() {
  const { t, locale } = useLocalizedText();
  const { memory, update, forget, forgetAll } = useUserMemory();
  const [editing, setEditing] = useState<{ id: string; text: string } | null>(null);
  const [confirmingForgetAll, setConfirmingForgetAll] = useState(false);
  const items = memory.data?.items ?? [];
  const editedText = editing?.text.trim() ?? "";
  const isEditValid = editedText.length > 0 && editedText.length <= maxMemoryTextLength;

  return (
    <Surface className="flex flex-col gap-6 rounded-3xl p-6" render={(props) => <section {...props} aria-labelledby="user-memory-title" />}>
      <div className="flex flex-col items-start gap-5 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-3">
          <HeaderIcon icon="brain" />
          <div className="flex flex-col gap-1">
            <h2 className="text-lg font-semibold text-foreground" id="user-memory-title">{t("intelligence.memory.title")}</h2>
            <p className="text-sm text-muted">{t("intelligence.memory.description")}</p>
          </div>
        </div>
        {items.length > 0 ? (
          confirmingForgetAll ? (
            <div className="flex flex-wrap gap-2">
              <Tooltip>
                <Button isPending={forgetAll.isPending} variant="danger" onPress={() => forgetAll.mutate(undefined, { onSuccess: () => setConfirmingForgetAll(false) })}>
                  <FaIcon aria-hidden="true" icon="trash-can" />
                  {t("intelligence.memory.confirmForgetAll")}
                </Button>
                <Tooltip.Content>{t("intelligence.memory.confirmForgetAllTooltip")}</Tooltip.Content>
              </Tooltip>
              <Tooltip>
                <Button variant="ghost" onPress={() => setConfirmingForgetAll(false)}>
                  <FaIcon aria-hidden="true" icon="xmark" />
                  {t("intelligence.memory.cancel")}
                </Button>
                <Tooltip.Content>{t("intelligence.memory.cancelTooltip")}</Tooltip.Content>
              </Tooltip>
            </div>
          ) : (
            <Tooltip>
              <Button variant="ghost" onPress={() => setConfirmingForgetAll(true)}>
                <FaIcon aria-hidden="true" icon="trash-can" />
                {t("intelligence.memory.forgetAll")}
              </Button>
              <Tooltip.Content>{t("intelligence.memory.forgetAllTooltip")}</Tooltip.Content>
            </Tooltip>
          )
        ) : null}
      </div>

      {memory.isError ? (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Description>{t("intelligence.memory.loadFailed")}</Alert.Description>
          </Alert.Content>
        </Alert>
      ) : memory.isPending ? (
        <Skeleton aria-label={t("intelligence.memory.loading")} className="h-16 w-full" />
      ) : items.length === 0 ? (
        <p className="text-muted">{t("intelligence.memory.empty")}</p>
      ) : (
        <ul className="flex flex-col divide-y divide-default">
          {items.map((item) => (
            <li key={item.id} className="flex flex-col gap-3 py-3">
              {editing?.id === item.id ? (
                <>
                  <TextField isInvalid={!isEditValid} value={editing.text} onChange={(text) => setEditing({ id: item.id, text })}>
                    <Label>{t("intelligence.memory.editLabel")}</Label>
                    <Input maxLength={maxMemoryTextLength} />
                  </TextField>
                  <div className="flex flex-wrap gap-2">
                    <Tooltip>
                      <Button
                        isDisabled={!isEditValid}
                        isPending={update.isPending}
                        onPress={() => update.mutate({ itemId: item.id, text: editedText }, { onSuccess: () => setEditing(null) })}
                      >
                        <FaIcon aria-hidden="true" icon="check" />
                        {t("intelligence.memory.save")}
                      </Button>
                      <Tooltip.Content>{t("intelligence.memory.saveTooltip")}</Tooltip.Content>
                    </Tooltip>
                    <Tooltip>
                      <Button variant="ghost" onPress={() => setEditing(null)}>
                        <FaIcon aria-hidden="true" icon="xmark" />
                        {t("intelligence.memory.cancel")}
                      </Button>
                      <Tooltip.Content>{t("intelligence.memory.cancelTooltip")}</Tooltip.Content>
                    </Tooltip>
                  </div>
                </>
              ) : (
                <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                  <div className="flex flex-col gap-1">
                    <p className="text-foreground">{item.text}</p>
                    <div className="flex flex-wrap items-center gap-2 text-sm text-muted">
                      <Chip size="sm" variant="soft">{t(`intelligence.memory.sources.${item.source}`)}</Chip>
                      <span>{formatDate(item.rememberedAt, locale as SupportedLocale, { dateStyle: "medium" })}</span>
                    </div>
                  </div>
                  <div className="flex gap-2">
                    <Tooltip>
                      <Button size="sm" variant="ghost" onPress={() => setEditing({ id: item.id, text: item.text })}>
                        <FaIcon aria-hidden="true" icon="pen" />
                        {t("intelligence.memory.edit")}
                      </Button>
                      <Tooltip.Content>{t("intelligence.memory.editTooltip")}</Tooltip.Content>
                    </Tooltip>
                    <Tooltip>
                      <Button isPending={forget.isPending && forget.variables === item.id} size="sm" variant="ghost" onPress={() => forget.mutate(item.id)}>
                        <FaIcon aria-hidden="true" icon="trash-can" />
                        {t("intelligence.memory.delete")}
                      </Button>
                      <Tooltip.Content>{t("intelligence.memory.deleteTooltip")}</Tooltip.Content>
                    </Tooltip>
                  </div>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}
      {update.isError || forget.isError || forgetAll.isError ? (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Description>{t("intelligence.memory.changeFailed")}</Alert.Description>
          </Alert.Content>
        </Alert>
      ) : null}
    </Surface>
  );
}
