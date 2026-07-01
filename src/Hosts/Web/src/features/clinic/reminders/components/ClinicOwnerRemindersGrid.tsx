"use client";

import { useCallback } from "react";
import { Card, Button, Spinner } from "@heroui/react";
import { useClinicOwnerReminders, useDeleteReminder, useMarkReminderSent } from "../hooks/useClinicOwnerReminders";
import type { ReminderReadDto } from "../api/remindersApi";
import { useLocalizedText } from "@/localization";

const KIND_LABELS: Record<number, string> = { 0: "Email", 1: "SMS" };

export function ClinicOwnerRemindersGrid() {
  const { t } = useLocalizedText();
  const { data: reminders, isLoading, error } = useClinicOwnerReminders();
  const deleteReminder = useDeleteReminder();
  const markSent = useMarkReminderSent();

  const handleDelete = useCallback(
    (id: string) => {
      if (confirm("Delete this reminder?")) {
        deleteReminder.mutate(id);
      }
    },
    [deleteReminder]
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
        <p className="text-sm text-default-500">{t("clinic.reminders.description")}</p>
      </div>

      {isLoading && (
        <Card className="p-8">
          <div className="flex items-center justify-center gap-2">
            <Spinner size="sm" />
          </div>
        </Card>
      )}

      {error && (
        <Card className="p-8">
          <p className="text-sm text-danger">Failed to load reminders.</p>
        </Card>
      )}

      {!isLoading && !error && (
        <Card>
          <Card.Content className="flex flex-col gap-1">
            {(!reminders || reminders.length === 0) && (
              <p className="py-8 text-center text-sm text-default-500">
                {t("clinic.reminders.empty")}
              </p>
            )}
            {reminders?.map((r) => (
              <ReminderRow
                key={r.id}
                reminder={r}
                onDelete={handleDelete}
                onMarkSent={handleMarkSent}
              />
            ))}
          </Card.Content>
        </Card>
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
  return (
    <div className="flex items-center justify-between rounded-lg px-4 py-3 transition-colors hover:bg-default-100">
      <div className="flex flex-col gap-1">
        <span className="text-sm font-medium">{reminder.subject ?? "No subject"}</span>
        <span className="text-xs text-default-500">
          {KIND_LABELS[reminder.kind] ?? "Unknown"} · {reminder.recipient ?? reminder.recipientUserId ?? "No recipient"}
        </span>
        <span className="text-xs text-default-500">
          Remind at: {new Date(reminder.remindAt).toLocaleString()}
        </span>
      </div>
      <div className="flex gap-1">
        {!reminder.isSent && (
          <Button variant="ghost" size="sm" onPress={() => onMarkSent(reminder.id)}>
            Mark Sent
          </Button>
        )}
        <Button variant="ghost" className="text-danger" size="sm" onPress={() => onDelete(reminder.id)}>
          Delete
        </Button>
      </div>
    </div>
  );
}
