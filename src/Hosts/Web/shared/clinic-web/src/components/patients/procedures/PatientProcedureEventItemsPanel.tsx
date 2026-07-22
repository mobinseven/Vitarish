"use client";

import { useId } from "react";
import { Button } from "@heroui/react";
import { ChatConversation, EmptyState } from "@heroui-pro/react";
import { FaIcon } from "@vitarish/web-ui";
import { useDeletePatientProcedureEventItem } from "../../../hooks/patients/procedures/usePatientProcedureEventItemMutations";
import { usePatientProcedureEventItems } from "../../../hooks/patients/procedures/usePatientProcedureEventItems";
import { useLocalizedText } from "../../../localization";
import type {
  EventItemSummary,
  ProcedureEventViewer,
} from "../../../models/patients/procedures/PatientProcedureEventItemModels";
import { PatientProcedureEventItemComposer } from "./PatientProcedureEventItemComposer";
import { PatientProcedureEventItemMessage } from "./PatientProcedureEventItemMessage";

interface PatientProcedureEventItemsPanelProps {
  eventId: string;
  canWrite: boolean;
  viewer?: ProcedureEventViewer;
}

function groupItemsByCreator(items: EventItemSummary[]) {
  const groups = new Map<string, EventItemSummary[]>();

  for (const item of items) {
    // Entries without a creator ID must not be attributed to the same person.
    const creatorKey = item.authorId ?? item.id;
    const group = groups.get(creatorKey);
    if (group) group.push(item);
    else groups.set(creatorKey, [item]);
  }

  return [...groups.values()];
}

export function PatientProcedureEventItemsPanel({
  eventId,
  canWrite,
  viewer,
}: PatientProcedureEventItemsPanelProps) {
  const { t } = useLocalizedText();
  const headingId = useId();
  const query = usePatientProcedureEventItems(eventId);
  const remove = useDeletePatientProcedureEventItem();
  const items = [...(query.data ?? [])].sort((left, right) =>
    left.createdAt.localeCompare(right.createdAt),
  );
  const itemGroups = groupItemsByCreator(items);

  return (
    <section className="flex min-h-0 flex-1 flex-col" aria-labelledby={headingId}>
      <h2 id={headingId} className="sr-only">
        {t("clinic.procedures.items.title")}
      </h2>
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
              <p className="text-danger m-0 text-sm">{t("clinic.procedures.items.loadFailed")}</p>
              <Button size="sm" variant="secondary" onPress={() => void query.refetch()}>
                {t("common.actions.retry")}
              </Button>
            </div>
          ) : items.length === 0 ? (
              <EmptyState size="sm">
                <EmptyState.Header>
                  <EmptyState.Media variant="icon">
                    <FaIcon icon="messages" />
                  </EmptyState.Media>
                  <EmptyState.Title>
              {t("clinic.procedures.items.empty")}</EmptyState.Title>
                </EmptyState.Header>
              </EmptyState>
          ) : itemGroups.map((group) =>
            group.map((item, index) => (
              <PatientProcedureEventItemMessage
                key={item.id}
                canWrite={canWrite}
                isDeleting={remove.isPending}
                item={item}
                showAuthor={index === 0}
                viewer={viewer}
                onDelete={() => remove.mutate(item.id)}
              />
            )),
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

      {canWrite ? <PatientProcedureEventItemComposer key={eventId} eventId={eventId} /> : null}
    </section>
  );
}
