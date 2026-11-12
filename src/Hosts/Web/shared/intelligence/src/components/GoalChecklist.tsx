"use client";

import { Button, Chip, Tooltip } from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";

export interface ChecklistGoal {
  key: string;
  label: string;
  done: boolean;
}

export interface GoalChecklistProps {
  goals: readonly ChecklistGoal[];
  /** The goal to work on next, highlighted. */
  nextGoal?: string | null;
  /** Lets a goal be picked, for example to ask the assistant about it or open its screen. */
  onSelect?: (key: string) => void;
}

/** What a guided surface still has to do, in order, with the next step highlighted (ADR-0022). */
export function GoalChecklist({ goals, nextGoal = null, onSelect }: GoalChecklistProps) {
  const { t } = useLocalizedText();
  return (
    <nav aria-label={t("intelligence.guidedSurface.goals.title")}>
      <h2 className="mb-3 font-semibold">{t("intelligence.guidedSurface.goals.title")}</h2>
      <ol className="flex flex-col gap-2">
        {goals.map((goal) => {
          const isNext = goal.key === nextGoal;
          const content = (
            <>
              <FaIcon icon={goal.done ? "circle-check" : "circle"} className={goal.done ? "text-success" : "text-muted"} aria-hidden="true" />
              <span className="min-w-0 flex-1 text-start">{goal.label}</span>
              <Chip className="shrink-0" variant="soft" color={goal.done ? "success" : isNext ? "accent" : "default"}>
                {goal.done
                  ? t("intelligence.guidedSurface.goals.done")
                  : isNext ? t("intelligence.guidedSurface.goals.next") : t("intelligence.guidedSurface.goals.open")}
              </Chip>
            </>
          );
          return (
            <li key={goal.key} aria-current={isNext ? "step" : undefined}>
              {onSelect && !goal.done ? (
                <Tooltip>
                  <Button variant="outline" fullWidth className="h-auto min-w-0 justify-start gap-3 whitespace-normal px-3 py-2 text-base font-normal" onPress={() => onSelect(goal.key)}>
                    {content}
                  </Button>
                  <Tooltip.Content>{t("intelligence.guidedSurface.goals.openTooltip")}</Tooltip.Content>
                </Tooltip>
              ) : (
                <div className="flex items-center gap-3 rounded-lg border border-border px-3 py-2">{content}</div>
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
