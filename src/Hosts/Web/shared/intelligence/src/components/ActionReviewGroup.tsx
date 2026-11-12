"use client";

import { useState, type ReactNode } from "react";
import { Button, Card, Tooltip } from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";

/** Where a prepared item stands, in the terms the group needs; each app maps its own statuses. */
export type ActionReviewItemState = "pending" | "done" | "failed" | "closed";

export interface ActionReviewItem {
  id: string;
  state: ActionReviewItemState;
  /** True when a failed item may be confirmed again. */
  retryable: boolean;
  /** The app's own review card for the item, with its details, status and edit form. */
  content: ReactNode;
}

export interface ActionReviewGroupProps {
  items: readonly ActionReviewItem[];
  /** Confirms one item through the app's route; rejects when the request itself fails. */
  confirm: (id: string) => Promise<void>;
}

/**
 * Everything an assistant prepared in one reply, reviewed together (ADR-0022). Each item keeps its own card;
 * "Confirm all" confirms the pending items one by one in order, and "Retry failed" confirms only the failures that
 * may be retried. A group is never all-or-nothing: each item shows its own outcome.
 */
export function ActionReviewGroup({ items, confirm }: ActionReviewGroupProps) {
  const { t } = useLocalizedText();
  const [isRunning, setRunning] = useState(false);
  const pending = items.filter((item) => item.state === "pending");
  const retryable = items.filter((item) => item.state === "failed" && item.retryable);
  const done = items.filter((item) => item.state === "done").length;
  const settled = pending.length === 0 && items.some((item) => item.state !== "done");

  const run = async (ids: readonly string[]) => {
    if (isRunning) return;
    setRunning(true);
    try {
      for (const id of ids) {
        // One item's failure never stops the rest; its card shows what happened.
        await confirm(id).catch(() => undefined);
      }
    } finally {
      setRunning(false);
    }
  };

  if (items.length === 1) return <>{items[0]!.content}</>;

  return (
    <Card>
      <Card.Header className="flex flex-wrap items-center justify-between gap-3">
        <Card.Title>{t("intelligence.guidedSurface.actionGroup.title")}</Card.Title>
        <span className="text-muted" aria-live="polite">
          {t("intelligence.guidedSurface.actionGroup.progress", { done, total: items.length })}
        </span>
      </Card.Header>
      <Card.Content className="flex flex-col gap-3">
        <ul className="flex flex-col gap-3">
          {items.map((item) => <li key={item.id}>{item.content}</li>)}
        </ul>
        {settled ? <p role="status">{t("intelligence.guidedSurface.actionGroup.partial")}</p> : null}
        <div className="flex flex-wrap gap-2">
          {pending.length > 0 ? (
            <Tooltip>
              <Button variant="primary" isDisabled={isRunning} isPending={isRunning} onPress={() => void run(pending.map((item) => item.id))}>
                <FaIcon icon="check-double" aria-hidden="true" />
                {t("intelligence.guidedSurface.actionGroup.confirmAll")}
              </Button>
              <Tooltip.Content>{t("intelligence.guidedSurface.actionGroup.confirmAllTooltip")}</Tooltip.Content>
            </Tooltip>
          ) : null}
          {retryable.length > 0 ? (
            <Tooltip>
              <Button variant="outline" isDisabled={isRunning} onPress={() => void run(retryable.map((item) => item.id))}>
                <FaIcon icon="rotate-right" aria-hidden="true" />
                {t("intelligence.guidedSurface.actionGroup.retryFailed")}
              </Button>
              <Tooltip.Content>{t("intelligence.guidedSurface.actionGroup.retryFailedTooltip")}</Tooltip.Content>
            </Tooltip>
          ) : null}
        </div>
      </Card.Content>
    </Card>
  );
}
