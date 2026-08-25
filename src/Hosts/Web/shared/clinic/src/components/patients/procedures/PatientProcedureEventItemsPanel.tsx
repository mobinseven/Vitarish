"use client";

import { useEffect, useId, useState } from "react";
import { Button, Tooltip } from "@heroui/react";
import { ChatConversation, EmptyState } from "@heroui-pro/react";
import { FaIcon } from "@vitarish/ui";
import { useDeletePatientProcedureEventItem } from "../../../hooks/patients/procedures/usePatientProcedureEventItemMutations";
import { usePatientProcedureEventItems } from "../../../hooks/patients/procedures/usePatientProcedureEventItems";
import { useLocalizedText } from "../../../localization";
import type {
  EventItemSummary,
  ProcedureEventViewer,
} from "../../../models/patients/procedures/PatientProcedureEventItemModels";
import { PatientProcedureEventItemComposer } from "./PatientProcedureEventItemComposer";
import { PatientProcedureEventItemMessage } from "./PatientProcedureEventItemMessage";
import {
  patientProcedureEventFeedPlugins,
  type PatientProcedureEventContext,
  type ProcedureEventPluginItem,
} from "./eventFeedPlugins";

interface PatientProcedureEventItemsPanelProps {
  context?: PatientProcedureEventContext;
  /** Compatibility for existing SEA-only consumers. New feature surfaces pass context. */
  eventId?: string;
  canWrite: boolean;
  viewer?: ProcedureEventViewer;
}

type FeedItem =
  | { kind: "sea"; id: string; createdAt: string; payload: EventItemSummary }
  | {
      kind: "plugin";
      id: string;
      createdAt: string;
      payload: ProcedureEventPluginItem;
    };

export function mergeProcedureEventFeed(
  seaItems: EventItemSummary[],
  pluginItems: ProcedureEventPluginItem[],
) {
  return [
    ...seaItems.map((payload): FeedItem => ({
      kind: "sea",
      id: `sea:${payload.id}`,
      createdAt: payload.createdAt,
      payload,
    })),
    ...pluginItems.map((payload): FeedItem => ({
      kind: "plugin",
      id: `${payload.pluginId}:${payload.id}`,
      createdAt: payload.createdAt,
      payload,
    })),
  ].sort(
    (left, right) =>
      left.createdAt.localeCompare(right.createdAt) ||
      left.id.localeCompare(right.id),
  );
}

export function PatientProcedureEventItemsPanel({
  context,
  eventId,
  canWrite,
  viewer,
}: PatientProcedureEventItemsPanelProps) {
  const { t } = useLocalizedText();
  const eventContext = context ?? {
    patientId: "",
    procedureId: "",
    eventId: eventId ?? "",
  };
  const [pluginItems, setPluginItems] = useState<ProcedureEventPluginItem[]>(
    [],
  );
  const [pluginState, setPluginState] = useState<
    "idle" | "loading" | "success" | "error"
  >("idle");
  const [pluginAttempt, setPluginAttempt] = useState(0);
  const headingId = useId();
  const query = usePatientProcedureEventItems(eventContext.eventId);
  useEffect(() => {
    if (!eventContext.patientId || !eventContext.procedureId) {
      setPluginItems([]);
      setPluginState("idle");
      return;
    }

    const controller = new AbortController();
    setPluginState("loading");
    void Promise.all(
      patientProcedureEventFeedPlugins.map((plugin) =>
        plugin.load(eventContext, controller.signal),
      ),
    ).then(
      (contributions) => {
        if (controller.signal.aborted) return;
        setPluginItems(contributions.flat());
        setPluginState("success");
      },
      () => {
        if (controller.signal.aborted) return;
        setPluginState("error");
      },
    );
    return () => controller.abort();
  }, [
    eventContext.eventId,
    eventContext.patientId,
    eventContext.procedureId,
    pluginAttempt,
  ]);
  const remove = useDeletePatientProcedureEventItem();
  const feed = mergeProcedureEventFeed(query.data ?? [], pluginItems);
  const isLoading = query.isLoading || pluginState === "loading";
  const pluginFailed = pluginState === "error";

  return (
    <section
      className="flex min-h-0 flex-1 flex-col"
      aria-labelledby={headingId}
    >
      <h2 id={headingId} className="sr-only">
        {t("clinic.procedures.items.title")}
      </h2>
      <ChatConversation className="min-h-0 flex-1">
        <ChatConversation.Content
          aria-label={t("clinic.procedures.items.title")}
          className="max-w-4xl flex-col gap-6 px-2 py-6 sm:px-4"
          role="feed"
        >
          {isLoading && feed.length === 0 ? (
            <p className="text-muted text-center text-sm">
              {t("common.states.loading")}
            </p>
          ) : query.error ? (
            <div
              className="flex flex-col items-center gap-3 py-10"
              role="alert"
            >
              <p className="text-danger m-0 text-sm">
                {t("clinic.procedures.items.loadFailed")}
              </p>
              <Tooltip>
                <Button
                  size="sm"
                  variant="secondary"
                  onPress={() => void query.refetch()}
                >
                  <FaIcon icon="arrow-rotate-right" aria-hidden="true" />
                  {t("common.actions.retry")}
                </Button>
                <Tooltip.Content>{t("common.actions.retry")}</Tooltip.Content>
              </Tooltip>
            </div>
          ) : feed.length === 0 && !pluginFailed ? (
            <EmptyState size="sm">
              <EmptyState.Header>
                <EmptyState.Media variant="icon">
                  <FaIcon icon="messages" />
                </EmptyState.Media>
                <EmptyState.Title>
                  {t("clinic.procedures.items.empty")}
                </EmptyState.Title>
              </EmptyState.Header>
            </EmptyState>
          ) : (
            feed.map((item, index) => {
              if (item.kind === "plugin") {
                const plugin = patientProcedureEventFeedPlugins.find(
                  (candidate) => candidate.id === item.payload.pluginId,
                );
                if (!plugin) return null;
                const Renderer = plugin.Renderer;
                return <Renderer key={item.id} item={item.payload as never} />;
              }
              const previous = feed[index - 1];
              const showAuthor =
                previous?.kind !== "sea" ||
                (previous.payload.authorId ?? previous.payload.id) !==
                  (item.payload.authorId ?? item.payload.id);
              return (
                <PatientProcedureEventItemMessage
                  key={item.id}
                  canWrite={canWrite}
                  isDeleting={remove.isPending}
                  item={item.payload}
                  showAuthor={showAuthor}
                  viewer={viewer}
                  onDelete={() => remove.mutate(item.payload.id)}
                />
              );
            })
          )}
          {pluginFailed ? (
            <div
              className="flex items-center justify-between gap-3 rounded-xl bg-danger-soft p-3"
              role="alert"
            >
              <p className="text-sm">
                {t("clinic.prescriptionFeed.loadFailed")}
              </p>
              <Tooltip>
                <Button
                  isIconOnly
                  aria-label={t("clinic.prescriptionFeed.retry")}
                  size="sm"
                  variant="ghost"
                  onPress={() => setPluginAttempt((attempt) => attempt + 1)}
                >
                  <FaIcon icon="arrow-rotate-right" aria-hidden="true" />
                </Button>
                <Tooltip.Content>
                  {t("clinic.prescriptionFeed.retry")}
                </Tooltip.Content>
              </Tooltip>
            </div>
          ) : null}
        </ChatConversation.Content>
        <ChatConversation.ScrollButton
          aria-label={t("clinic.procedures.items.scrollToLatest")}
          tooltip={t("clinic.procedures.items.scrollToLatest")}
        />
      </ChatConversation>

      {remove.isError ? (
        <p
          className="text-danger mx-auto mb-2 w-full max-w-4xl text-sm"
          role="alert"
        >
          {t("clinic.procedures.items.deleteFailed")}
        </p>
      ) : null}

      {canWrite ? (
        <PatientProcedureEventItemComposer
          key={eventContext.eventId}
          eventId={eventContext.eventId}
        />
      ) : null}
    </section>
  );
}
