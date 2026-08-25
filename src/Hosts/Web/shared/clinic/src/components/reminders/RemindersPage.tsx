"use client";
import { useState } from "react";
import {
  ErrorStateSurface as ClinicErrorState,
  LoadingStateSurface as ClinicLoadingState,
} from "@vitarish/ui";
import { useLocalizedText } from "../../localization";
import { ReminderDeleteDialog } from "./ReminderDeleteDialog";
import { ReminderList } from "./ReminderList";
import {
  useOwnerReminders,
  useDeleteReminder,
  useMarkReminderSent,
} from "../../hooks/reminders/useOwnerReminders";
export function RemindersPage() {
  const { t } = useLocalizedText();
  const query = useOwnerReminders();
  const deletion = useDeleteReminder();
  const markSent = useMarkReminderSent();
  const [deleteId, setDeleteId] = useState<string | null>(null);
  return (
    <section className="flex flex-col gap-6">
      <header>
        <h1 className="text-2xl font-bold">{t("clinic.reminders.title")}</h1>
        <p className="text-sm text-muted">
          {t("clinic.reminders.description")}
        </p>
      </header>
      {query.isLoading ? (
        <ClinicLoadingState label={t("common.states.loading")} />
      ) : null}
      {query.error ? (
        <ClinicErrorState title={t("clinic.reminders.failedToLoad")} />
      ) : null}
      {!query.isLoading && !query.error ? (
        <ReminderList
          items={query.data ?? []}
          onDelete={setDeleteId}
          onMarkSent={(id) =>
            markSent.mutate({ id, sentAt: new Date().toISOString() })
          }
        />
      ) : null}
      {deletion.isError ? (
        <ClinicErrorState title={t("clinic.reminders.deleteFailed")} />
      ) : null}
      <ReminderDeleteDialog
        isOpen={deleteId !== null}
        isPending={deletion.isPending}
        onCancel={() => setDeleteId(null)}
        onConfirm={() => {
          if (deleteId)
            deletion.mutate(deleteId, { onSuccess: () => setDeleteId(null) });
        }}
      />
    </section>
  );
}
