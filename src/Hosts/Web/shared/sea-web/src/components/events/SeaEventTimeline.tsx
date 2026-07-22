"use client";

import { useCallback, useState } from "react";
import { Button, Chip } from "@heroui/react";
import { Timeline, Widget } from "@heroui-pro/react";
import type { SeaNavigationIntent } from "../../navigation";
import { SeaLoadingState, SeaEmptyState } from "../state/SeaStateSurface";
import { useSeaEvents } from "../../hooks/events/useSeaEvents";
import { useSeaEventCapabilities } from "../../hooks/events/useSeaEventCapabilities";
import { useCommitSeaEvent } from "../../hooks/events/useCommitSeaEvent";
import { useUncommitSeaEvent } from "../../hooks/events/useUncommitSeaEvent";
import { useMoveSeaEventUp } from "../../hooks/events/useMoveSeaEvent";
import { useMoveSeaEventDown } from "../../hooks/events/useMoveSeaEvent";
import { useDeleteSeaEvent } from "../../hooks/events/useDeleteSeaEvent";
import { useLocalizedText, formatDateTime } from "../../localization";
import type { SeaEventReadDto } from "../../models/seaStatModels";

interface SeaEventTimelineProps {
  procedureId: string;
  projectId: string;
  onNavigate?: (intent: SeaNavigationIntent) => void;
}

export function SeaEventTimeline({
  procedureId,
  projectId,
  onNavigate,
}: SeaEventTimelineProps) {
  const { t, locale } = useLocalizedText();
  const { data: events, isLoading } = useSeaEvents(procedureId);
  const commitMutation = useCommitSeaEvent();
  const uncommitMutation = useUncommitSeaEvent();
  const moveUpMutation = useMoveSeaEventUp();
  const moveDownMutation = useMoveSeaEventDown();
  const deleteMutation = useDeleteSeaEvent();

  const [confirmDeleteId, setConfirmDeleteId] = useState<string | null>(null);
  const [confirmUncommitId, setConfirmUncommitId] = useState<string | null>(
    null,
  );

  const handleToggleCommit = useCallback(
    (event: SeaEventReadDto) => {
      if (event.isCommitted) {
        setConfirmUncommitId(event.id);
      } else {
        commitMutation.mutate(event.id);
      }
    },
    [commitMutation],
  );

  const handleConfirmUncommit = useCallback(() => {
    if (confirmUncommitId) {
      uncommitMutation.mutate(confirmUncommitId, {
        onSuccess: () => setConfirmUncommitId(null),
      });
    }
  }, [confirmUncommitId, uncommitMutation]);

  const handleConfirmDelete = useCallback(() => {
    if (confirmDeleteId) {
      deleteMutation.mutate(confirmDeleteId, {
        onSuccess: () => setConfirmDeleteId(null),
      });
    }
  }, [confirmDeleteId, deleteMutation]);

  if (isLoading) {
    return <SeaLoadingState label={t("sea.events.loading")} />;
  }

  const eventList = events ?? [];

  if (eventList.length === 0) {
    return <SeaEmptyState title={t("sea.events.emptyState")} />;
  }

  return (
    <Widget>
      <Widget.Header>
        <Widget.Title>{t("sea.events.title")}</Widget.Title>
      </Widget.Header>
      <Widget.Content>
        <Timeline
          aria-label={t("sea.events.title")}
          density="compact"
          size="sm"
        >
          {eventList.map((event, index) => (
            <SeaEventTimelineItem
              key={event.id}
              event={event}
              eventCount={eventList.length}
              index={index}
              locale={locale}
              projectId={projectId}
              procedureId={procedureId}
              onDelete={setConfirmDeleteId}
              onMoveDown={(id) => moveDownMutation.mutate(id)}
              onMoveUp={(id) => moveUpMutation.mutate(id)}
              onToggleCommit={handleToggleCommit}
              onNavigate={onNavigate}
              isCommitPending={
                commitMutation.isPending || uncommitMutation.isPending
              }
              isMoveDownPending={moveDownMutation.isPending}
              isMoveUpPending={moveUpMutation.isPending}
            />
          ))}
        </Timeline>

        {confirmUncommitId && (
          <div className="mt-4 rounded-lg border border-warning/20 bg-warning/5 p-4">
            <p className="text-sm mb-2">{t("sea.events.confirmUncommit")}</p>
            <div className="flex gap-2">
              <Button
                size="sm"
                variant="ghost"
                onPress={() => setConfirmUncommitId(null)}
                isDisabled={uncommitMutation.isPending}
              >
                {t("common.actions.cancel")}
              </Button>
              <Button
                size="sm"
                variant="danger"
                onPress={handleConfirmUncommit}
                isDisabled={uncommitMutation.isPending}
              >
                {uncommitMutation.isPending
                  ? t("common.states.saving")
                  : t("sea.events.uncommit")}
              </Button>
            </div>
          </div>
        )}

        {confirmDeleteId && (
          <div className="mt-4 rounded-lg border border-danger/20 bg-danger/5 p-4">
            <p className="text-sm mb-2">{t("sea.events.confirmDelete")}</p>
            <div className="flex gap-2">
              <Button
                size="sm"
                variant="ghost"
                onPress={() => setConfirmDeleteId(null)}
                isDisabled={deleteMutation.isPending}
              >
                {t("common.actions.cancel")}
              </Button>
              <Button
                size="sm"
                variant="danger"
                onPress={handleConfirmDelete}
                isDisabled={deleteMutation.isPending}
              >
                {deleteMutation.isPending
                  ? t("common.states.saving")
                  : t("common.actions.delete")}
              </Button>
            </div>
          </div>
        )}
      </Widget.Content>
    </Widget>
  );
}

interface SeaEventTimelineItemProps {
  event: SeaEventReadDto;
  eventCount: number;
  index: number;
  locale: Parameters<typeof formatDateTime>[1];
  projectId: string;
  procedureId: string;
  isCommitPending: boolean;
  isMoveDownPending: boolean;
  isMoveUpPending: boolean;
  onDelete: (eventId: string) => void;
  onMoveDown: (eventId: string) => void;
  onMoveUp: (eventId: string) => void;
  onToggleCommit: (event: SeaEventReadDto) => void;
  onNavigate?: (intent: SeaNavigationIntent) => void;
}

function SeaEventTimelineItem({
  event,
  eventCount,
  index,
  locale,
  projectId,
  procedureId,
  isCommitPending,
  isMoveDownPending,
  isMoveUpPending,
  onDelete,
  onMoveDown,
  onMoveUp,
  onToggleCommit,
  onNavigate,
}: SeaEventTimelineItemProps) {
  const { t } = useLocalizedText();
  const { data: capabilities } = useSeaEventCapabilities(event.id);

  return (
    <Timeline.Item
      id={event.id}
      status={event.isCommitted ? "success" : "default"}
    >
      <Timeline.Marker aria-hidden="true" />
      <Timeline.Content>
        <div className="flex min-w-0 flex-col gap-2">
          <div className="flex min-w-0 flex-wrap items-center gap-2">
            <span className="text-sm font-medium">{event.title}</span>
            <Chip
              size="sm"
              variant="soft"
              color={event.isCommitted ? "success" : "default"}
            >
              {event.isCommitted
                ? t("sea.events.committed")
                : t("sea.events.draft")}
            </Chip>
          </div>
          {event.description && (
            <p className="text-sm text-muted">{event.description}</p>
          )}
          <span className="text-xs text-muted">
            {formatDateTime(event.createdAt, locale)}
          </span>
          <div className="flex gap-1">
            <Button
              size="sm"
              variant="ghost"
              onPress={() =>
                onNavigate?.({
                  kind: "eventDetail",
                  projectId,
                  procedureId,
                  eventId: event.id,
                })
              }
            >
              {capabilities?.isElevated
                ? t("common.actions.edit")
                : t("common.actions.view")}
            </Button>
            {capabilities?.canWrite && (
              <Button
                size="sm"
                variant="ghost"
                onPress={() => onToggleCommit(event)}
                isDisabled={isCommitPending}
              >
                {event.isCommitted
                  ? t("sea.events.uncommit")
                  : t("sea.events.commit")}
              </Button>
            )}
            {capabilities?.isElevated && (
              <>
                {index > 0 && (
                  <Button
                    size="sm"
                    variant="ghost"
                    onPress={() => onMoveUp(event.id)}
                    isDisabled={isMoveUpPending}
                  >
                    {t("sea.events.moveUp")}
                  </Button>
                )}
                {index < eventCount - 1 && (
                  <Button
                    size="sm"
                    variant="ghost"
                    onPress={() => onMoveDown(event.id)}
                    isDisabled={isMoveDownPending}
                  >
                    {t("sea.events.moveDown")}
                  </Button>
                )}
                <Button
                  size="sm"
                  variant="danger"
                  onPress={() => onDelete(event.id)}
                >
                  {t("sea.events.deleteEvent")}
                </Button>
              </>
            )}
          </div>
        </div>
      </Timeline.Content>
    </Timeline.Item>
  );
}
