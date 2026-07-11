"use client";

import { useCallback } from "react";
import { Button } from "@heroui/react";
import { ListView } from "@heroui-pro/react";
import { ClinicEmptyState, ClinicErrorState, ClinicLoadingState } from "../../../../features/clinic/components/ClinicStateSurface";
import { useClinicOwnerReminders, useDeleteReminder, useMarkReminderSent } from "../hooks/useClinicOwnerReminders";
import type { ReminderReadDto } from "../api/remindersApi";
import { useLocalizedText, formatDateTime, type MessageKey } from "../../../../localization";

const reminderKindKeys: Record<number, MessageKey> = {
  0: "clinic.reminders.kindEmail",
  1: "clinic.reminders.kindSms",
};

export function ClinicOwnerRemindersGrid() {
  const { t } = useLocalizedText();
  const { data: reminders, isLoading, error } = useClinicOwnerReminders();
  const deleteReminder = useDeleteReminder();
  const markSent = useMarkReminderSent();

  const handleDelete = useCallback(
    (id: string) => {
      if (confirm(t("clinic.reminders.confirmDelete"))) {
        deleteReminder.mutate(id);
      }
    },
    [deleteReminder, t]
  );

  const handleMarkSent = useCallback(
    (id: string) => {
      markSent.mutate({ id, sentAt: new Date().toISOString() });
    },
    [markSent]
  );

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.reminders.title")}</h1>
        <p className="text-sm text-muted">{t("clinic.reminders.description")}</p>
      </div>

      {isLoading && (
        <ClinicLoadingState label={t("common.states.loading")} />
      )}

      {error && (
        <ClinicErrorState title={t("clinic.reminders.failedToLoad")} />
      )}

      {!isLoading && !error && (
        <ListView
          aria-label={t("clinic.reminders.title")}
          renderEmptyState={() => <ClinicEmptyState title={t("clinic.reminders.empty")} />}
        >
          {reminders?.map((reminder) => (
            <ReminderRow
              key={reminder.id}
              reminder={reminder}
              onDelete={handleDelete}
              onMarkSent={handleMarkSent}
            />
          ))}
        </ListView>
      )}
    </div>
  );
}

function ReminderRow({
  reminder,
  onDelete,
  onMarkSent,
}: {
  reminder: ReminderReadDto;
  onDelete: (id: string) => void;
  onMarkSent: (id: string) => void;
}) {
  const { t, locale } = useLocalizedText();
  const subject = reminder.subject ?? t("clinic.reminders.noSubject");

  return (
    <ListView.Item id={reminder.id} textValue={subject}>
      <ListView.ItemContent>
        <div className="flex flex-col gap-1">
          <ListView.Title>{subject}</ListView.Title>
          <ListView.Description>
            {t(reminderKindKeys[reminder.kind] ?? "common.values.unknown")} / {reminder.recipient ?? reminder.recipientUserId ?? t("clinic.reminders.noRecipient")}
          </ListView.Description>
          <ListView.Description>
            {t("clinic.reminders.remindAt", { value: formatDateTime(reminder.remindAt, locale) })}
          </ListView.Description>
        </div>
      </ListView.ItemContent>
      <ListView.ItemAction className="flex gap-1">
        {!reminder.isSent && (
          <Button variant="ghost" size="sm" onPress={() => onMarkSent(reminder.id)}>
            {t("clinic.reminders.markSent")}
          </Button>
        )}
        <Button variant="ghost" className="text-danger" size="sm" onPress={() => onDelete(reminder.id)}>
          {t("common.actions.delete")}
        </Button>
      </ListView.ItemAction>
    </ListView.Item>
  );
}
