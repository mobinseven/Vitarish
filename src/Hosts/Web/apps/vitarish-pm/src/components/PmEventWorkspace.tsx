"use client";

import * as React from "react";
import { Button, Chip, Tooltip } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { useRouter } from "next/navigation";
import { useLocalizedText } from "@vitarish/localization";
import { Routes } from "@/app/routes";
import {
  SeaEventAttachmentsPanel,
  SeaErrorState,
  SeaLoadingState,
  useCommitSeaEvent,
  useSeaEventDetail,
  useUncommitSeaEvent,
} from "@vitarish/sea";
import { FaIcon } from "@vitarish/ui";
import { PmEventAssistantSheet } from "./PmEventAssistantSheet";

export function PmEventWorkspace({
  projectId,
  procedureId,
  eventId,
}: {
  projectId: string;
  procedureId: string;
  eventId: string;
}) {
  const router = useRouter();
  const { t } = useLocalizedText();
  const query = useSeaEventDetail(eventId);
  const commit = useCommitSeaEvent();
  const uncommit = useUncommitSeaEvent();
  const [confirm, setConfirm] = React.useState(false);
  if (query.isLoading)
    return <SeaLoadingState label={t("sea.events.loading")} />;
  if (query.error || !query.data)
    return <SeaErrorState title={t("sea.events.notFound")} />;
  const event = query.data;
  return (
    <div className="flex flex-col gap-5">
      <header className="flex flex-col gap-4 border-b border-border/70 pb-5 sm:flex-row sm:items-end sm:justify-between">
        <div className="min-w-0">
          <Tooltip>
            <Button
              size="sm"
              variant="ghost"
              onPress={() =>
                router.push(Routes.Procedure(projectId, procedureId))
              }
            >
              <FaIcon icon="arrow-left" aria-hidden="true" />
              {t("pm.backToProcedure")}
            </Button>
            <Tooltip.Content>{t("pm.backToProcedure")}</Tooltip.Content>
          </Tooltip>
          <p className="mt-3 text-xs font-semibold uppercase tracking-[0.16em] text-muted">
            {t("pm.eventWorkspace")}
          </p>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight">
            {event.title}
          </h1>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Chip
            color={event.isCommitted ? "success" : "default"}
            variant="soft"
          >
            {event.isCommitted
              ? t("sea.events.committed")
              : t("sea.events.draft")}
          </Chip>
          {event.isCommitted ? (
            <Tooltip>
              <Button
                size="sm"
                variant="secondary"
                onPress={() => setConfirm(true)}
              >
                <FaIcon icon="rotate-left" aria-hidden="true" />
                {t("sea.events.uncommit")}
              </Button>
              <Tooltip.Content>{t("sea.events.uncommit")}</Tooltip.Content>
            </Tooltip>
          ) : (
            <Tooltip>
              <Button
                size="sm"
                variant="primary"
                className="bg-success text-white"
                onPress={() => commit.mutate(event.id)}
              >
                <FaIcon icon="check" aria-hidden="true" />
                {t("pm.markCommitted")}
              </Button>
              <Tooltip.Content>{t("pm.markCommitted")}</Tooltip.Content>
            </Tooltip>
          )}
        </div>
      </header>
      {confirm && (
        <div className="rounded-lg border border-danger/20 bg-danger/[0.05] p-4">
          <p className="text-sm">{t("sea.events.confirmUncommit")}</p>
          <div className="mt-3 flex gap-2">
            <Tooltip>
              <Button
                size="sm"
                variant="ghost"
                onPress={() => setConfirm(false)}
              >
                <FaIcon icon="xmark" aria-hidden="true" />
                {t("common.actions.cancel")}
              </Button>
              <Tooltip.Content>{t("common.actions.cancel")}</Tooltip.Content>
            </Tooltip>
            <Tooltip>
              <Button
                size="sm"
                variant="danger"
                onPress={() => {
                  uncommit.mutate(event.id);
                  setConfirm(false);
                }}
              >
                <FaIcon icon="rotate-left" aria-hidden="true" />
                {t("sea.events.uncommit")}
              </Button>
              <Tooltip.Content>{t("sea.events.uncommit")}</Tooltip.Content>
            </Tooltip>
          </div>
        </div>
      )}
      <PmEventAssistantSheet
        eventId={event.id}
        onDeleted={() => router.push(Routes.Procedure(projectId, procedureId))}
      />
      <div className="grid min-w-0 gap-5 xl:grid-cols-[minmax(0,1.7fr)_minmax(20rem,1fr)] xl:items-start">
        <Widget>
          <Widget.Header>
            <Widget.Title id="event-detail-title">{t("pm.event")}</Widget.Title>
          </Widget.Header>
          <Widget.Content>
            <section aria-labelledby="event-detail-title">
              <p className="max-w-[70ch] whitespace-pre-wrap text-sm leading-7">
                {event.description || t("pm.noDescription")}
              </p>
            </section>
          </Widget.Content>
        </Widget>
        <div className="min-w-0 xl:sticky xl:top-4">
          <SeaEventAttachmentsPanel eventId={event.id} />
        </div>
      </div>
    </div>
  );
}
