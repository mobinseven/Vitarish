"use client";

import * as React from "react";
import { Button, Chip, Disclosure, Tooltip } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { useRouter } from "next/navigation";
import { useLocalizedText } from "@vitarish/localization";
import { Routes } from "@/app/routes";
import {
  SeaErrorState,
  SeaLoadingState,
  useCommitSeaEvent,
  useSeaEventAttachmentCount,
  useSeaEventAttachments,
  useSeaEvents,
  useSeaProcedureDetail,
  useUncommitSeaEvent,
} from "@vitarish/sea";
import type { SeaEventReadDto } from "@vitarish/sea";
import { FaIcon } from "@vitarish/ui";

export function PmProcedureWorkspace({
  projectId,
  procedureId,
  focusedEventId,
}: {
  projectId: string;
  procedureId: string;
  focusedEventId?: string;
}) {
  const router = useRouter();
  const { t } = useLocalizedText();
  const procedureQuery = useSeaProcedureDetail(procedureId);
  const eventsQuery = useSeaEvents(procedureId);
  const commit = useCommitSeaEvent();
  const uncommit = useUncommitSeaEvent();
  const [reopen, setReopen] = React.useState(false);

  if (procedureQuery.isLoading || eventsQuery.isLoading)
    return <SeaLoadingState label={t("sea.events.loading")} />;
  if (procedureQuery.error || !procedureQuery.data)
    return <SeaErrorState title={t("sea.procedures.notFound")} />;

  const events = eventsQuery.data ?? [];
  const focused = events.find((event) => event.id === focusedEventId) ?? null;
  const focus = (id: string) =>
    router.replace(
      `${Routes.Procedure(projectId, procedureId)}?event=${encodeURIComponent(id)}`,
    );

  return (
    <div className="flex flex-col gap-5">
      <header className="flex flex-col gap-3 border-b border-border/70 pb-4 sm:flex-row sm:items-end sm:justify-between">
        <div className="min-w-0">
          <Tooltip>
            <Button
              size="sm"
              variant="ghost"
              onPress={() => router.push(Routes.Project(projectId))}
            >
              <FaIcon icon="arrow-left" aria-hidden="true" />
              {t("sea.procedures.backToProject")}
            </Button>
            <Tooltip.Content>
              {t("sea.procedures.backToProject")}
            </Tooltip.Content>
          </Tooltip>
          <p className="mt-3 text-xs font-semibold uppercase tracking-[0.16em] text-muted">
            {t("pm.procedureWorkspace")}
          </p>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight">
            {procedureQuery.data.name}
          </h1>
          {procedureQuery.data.description && (
            <p className="mt-1 max-w-3xl text-sm leading-6 text-muted">
              {procedureQuery.data.description}
            </p>
          )}
        </div>
        <Chip variant="soft">
          {events.length} {t("sea.stats.events")}
        </Chip>
      </header>
      <div className="grid min-w-0 gap-5 xl:grid-cols-[minmax(0,1fr)_minmax(18rem,0.36fr)] xl:items-start">
        <Widget>
          <Widget.Header>
            <Widget.Title>{t("pm.eventTimeline")}</Widget.Title>
          </Widget.Header>
          <Widget.Content className="flex flex-col gap-2">
            {events.length === 0 ? (
              <p className="text-sm text-muted">{t("sea.events.emptyState")}</p>
            ) : (
              events.map((event) => (
                <EventRow
                  key={event.id}
                  event={event}
                  focused={event.id === focusedEventId}
                  onFocus={() => focus(event.id)}
                  onView={() =>
                    router.push(Routes.Event(projectId, procedureId, event.id))
                  }
                  t={t}
                />
              ))
            )}
          </Widget.Content>
        </Widget>
        <Widget
          className={
            focused
              ? "border-primary/25 bg-primary/[0.045] lg:sticky lg:top-4"
              : "lg:sticky lg:top-4"
          }
        >
          <Widget.Header>
            <Widget.Title>{t("pm.currentStep")}</Widget.Title>
          </Widget.Header>
          <Widget.Content>
            {focused ? (
              <FocusedEvent
                event={focused}
                projectId={projectId}
                procedureId={procedureId}
                reopen={reopen}
                setReopen={setReopen}
                commit={() => commit.mutate(focused.id)}
                uncommit={() => {
                  uncommit.mutate(focused.id);
                  setReopen(false);
                }}
                t={t}
              />
            ) : (
              <p className="text-sm leading-6 text-muted">
                {t("pm.selectEvent")}
              </p>
            )}
          </Widget.Content>
        </Widget>
      </div>
    </div>
  );
}

function EventRow({
  event,
  focused,
  onFocus,
  onView,
  t,
}: {
  event: SeaEventReadDto;
  focused: boolean;
  onFocus: () => void;
  onView: () => void;
  t: (key: string) => string;
}) {
  return (
    <div
      className={`grid min-w-0 gap-3 overflow-hidden rounded-lg border p-3 md:grid-cols-[minmax(0,1fr)_auto] md:items-center ${focused ? "border-primary/30 bg-primary/[0.08]" : event.isCommitted ? "border-success/20 bg-success/[0.045]" : "border-border/70 bg-content1"}`}
    >
      <div className="min-w-0 overflow-hidden">
        <Tooltip>
          <Button
            variant="ghost"
            className="h-auto w-full max-w-full min-w-0 justify-start overflow-hidden whitespace-normal px-0 py-0 text-start"
            onPress={onFocus}
          >
            <span
              className={`mt-1.5 h-2 w-2 shrink-0 rounded-full ${event.isCommitted ? "bg-success" : "bg-default-400"}`}
            />
            <span className="w-full min-w-0 overflow-hidden">
              <span className="flex min-w-0 flex-wrap items-center gap-2">
                <span className="line-clamp-2 text-sm font-semibold leading-5">
                  {event.title}
                </span>
                {focused && (
                  <Chip
                    size="sm"
                    color="accent"
                    variant="soft"
                    className="shrink-0"
                  >
                    {t("pm.focused")}
                  </Chip>
                )}
              </span>
              <span className="mt-1 block line-clamp-2 whitespace-normal text-xs leading-5 text-muted">
                {event.description || t("pm.noDescription")}
              </span>
            </span>
          </Button>
          <Tooltip.Content>{t("pm.focusEvent")}</Tooltip.Content>
        </Tooltip>
      </div>
      <div className="flex min-w-0 items-center justify-between gap-3 md:justify-end">
        <div className="flex min-w-0 flex-wrap items-center gap-2">
          <Chip
            size="sm"
            color={event.isCommitted ? "success" : "default"}
            variant="soft"
          >
            {event.isCommitted
              ? t("sea.events.committed")
              : t("sea.events.draft")}
          </Chip>
          <AttachmentCount eventId={event.id} t={t} />
        </div>
        <Tooltip>
          <Button
            size="sm"
            variant="secondary"
            aria-label={`${t("common.actions.view")} ${event.title}`}
            onPress={onView}
          >
            <FaIcon icon="arrow-right" aria-hidden="true" />
            <span className="hidden sm:inline">{t("common.actions.view")}</span>
          </Button>
          <Tooltip.Content>{t("common.actions.view")}</Tooltip.Content>
        </Tooltip>
      </div>
    </div>
  );
}

function FocusedEvent({
  event,
  projectId,
  procedureId,
  reopen,
  setReopen,
  commit,
  uncommit,
  t,
}: {
  event: SeaEventReadDto;
  projectId: string;
  procedureId: string;
  reopen: boolean;
  setReopen: (value: boolean) => void;
  commit: () => void;
  uncommit: () => void;
  t: (key: string) => string;
}) {
  const router = useRouter();
  const attachmentsQuery = useSeaEventAttachments(event.id);
  const attachments = attachmentsQuery.data ?? [];
  return (
    <div className="flex flex-col gap-4">
      <div>
        <p className="text-lg font-semibold leading-6">{event.title}</p>
        <p className="mt-1 text-sm leading-6 text-muted">
          {event.description || t("pm.noDescription")}
        </p>
      </div>
      <div className="flex flex-wrap items-center gap-2">
        <Chip color={event.isCommitted ? "success" : "default"} variant="soft">
          {event.isCommitted
            ? t("sea.events.committed")
            : t("sea.events.draft")}
        </Chip>
        <AttachmentCount eventId={event.id} t={t} />
      </div>
      <Disclosure className="rounded-lg border border-border/70">
        <Disclosure.Heading>
          <Tooltip>
            <Disclosure.Trigger className="w-full justify-between px-3 py-2 text-sm font-medium">
              <span className="flex items-center gap-2">
                <FaIcon icon="paperclip" aria-hidden="true" />
                {t("pm.reviewAttachments")}
              </span>
              <Disclosure.Indicator />
            </Disclosure.Trigger>
            <Tooltip.Content>{t("pm.reviewAttachments")}</Tooltip.Content>
          </Tooltip>
        </Disclosure.Heading>
        <Disclosure.Content>
          <Disclosure.Body className="flex flex-col gap-2 border-t border-border/70 px-3 py-3">
            {attachmentsQuery.isLoading ? (
              <p className="text-xs text-muted">
                {t("sea.attachments.loading")}
              </p>
            ) : attachments.length === 0 ? (
              <p className="text-xs text-muted">
                {t("sea.attachments.emptyState")}
              </p>
            ) : (
              attachments.map((attachment) => (
                <div
                  key={attachment.id}
                  className="rounded-md bg-default-100/60 px-2 py-2 text-xs"
                >
                  <p className="line-clamp-2">
                    {attachment.contentType.startsWith("text/")
                      ? attachment.content
                      : attachment.fileName || t("sea.attachments.openFile")}
                  </p>
                  <p className="mt-1 text-muted">
                    {attachment.size} {t("sea.attachments.byteUnit")}
                  </p>
                </div>
              ))
            )}
          </Disclosure.Body>
        </Disclosure.Content>
      </Disclosure>
      {event.isCommitted ? (
        reopen ? (
          <Confirm
            onCancel={() => setReopen(false)}
            onConfirm={uncommit}
            t={t}
          />
        ) : (
          <Tooltip>
            <Button variant="secondary" onPress={() => setReopen(true)}>
              <FaIcon icon="rotate-left" aria-hidden="true" />
              {t("sea.events.uncommit")}
            </Button>
            <Tooltip.Content>{t("sea.events.uncommit")}</Tooltip.Content>
          </Tooltip>
        )
      ) : (
        <Tooltip>
          <Button
            variant="primary"
            className="bg-success text-white"
            onPress={commit}
          >
            <FaIcon icon="check" aria-hidden="true" />
            {t("pm.markCommitted")}
          </Button>
          <Tooltip.Content>{t("pm.markCommitted")}</Tooltip.Content>
        </Tooltip>
      )}
      <Tooltip>
        <Button
          variant="ghost"
          onPress={() =>
            router.push(Routes.Event(projectId, procedureId, event.id))
          }
        >
          <FaIcon icon="arrow-up-right-from-square" aria-hidden="true" />
          {t("pm.openEventWorkspace")}
        </Button>
        <Tooltip.Content>{t("pm.openEventWorkspace")}</Tooltip.Content>
      </Tooltip>
    </div>
  );
}

function AttachmentCount({
  eventId,
  t,
}: {
  eventId: string;
  t: (key: string) => string;
}) {
  const query = useSeaEventAttachmentCount(eventId);
  return (
    <span className="text-xs text-muted">
      {query.data ?? 0} {t("pm.attachments")}
    </span>
  );
}
function Confirm({
  onCancel,
  onConfirm,
  t,
}: {
  onCancel: () => void;
  onConfirm: () => void;
  t: (key: string) => string;
}) {
  return (
    <div className="rounded-lg border border-danger/20 bg-danger/[0.05] p-3">
      <p className="text-sm">{t("sea.events.confirmUncommit")}</p>
      <div className="mt-3 flex gap-2">
        <Tooltip>
          <Button size="sm" variant="ghost" onPress={onCancel}>
            <FaIcon icon="xmark" aria-hidden="true" />
            {t("common.actions.cancel")}
          </Button>
          <Tooltip.Content>{t("common.actions.cancel")}</Tooltip.Content>
        </Tooltip>
        <Tooltip>
          <Button size="sm" variant="danger" onPress={onConfirm}>
            <FaIcon icon="rotate-left" aria-hidden="true" />
            {t("sea.events.uncommit")}
          </Button>
          <Tooltip.Content>{t("sea.events.uncommit")}</Tooltip.Content>
        </Tooltip>
      </div>
    </div>
  );
}
