"use client";

import { Button, Tooltip } from "@heroui/react";
import { GoalChecklist, InteractiveOptionPicker, type GuidedSurfaceActionGroup, type InteractiveQuestion } from "@vitarish/intelligence";
import { FaIcon } from "@vitarish/ui";
import { useClinicGetSetupProgress } from "../../api/generated/clinic/clinic";
import { useLocalizedText } from "../../localization";
import { OperationsActionGroup } from "./AssistantActions";

export interface OperationsGuidedReplyProps {
  actionGroups: readonly GuidedSurfaceActionGroup[];
  question: InteractiveQuestion | null;
  nextAction: string | null;
  /** Changes whenever a new message is sent, so an answered question resets. */
  turnKey: string;
  isPending: boolean;
  submit: (message: string) => void;
  replaceAction: (originalId: string, replacementId: string) => void;
  /** Opens the screen of a setup goal; without it, no next-step button or goal list shows. */
  onOpenGoal?: (goal: string) => void;
  /** Shows the setup goal list after an owner chose "Later", so it stays reachable from the assistant. */
  showDismissedSetupGoals?: boolean;
}

/** The operations assistant's guided surface for its replies (ADR-0022), shared by every place it appears. */
export function OperationsGuidedReply({
  actionGroups,
  question,
  nextAction,
  turnKey,
  isPending,
  submit,
  replaceAction,
  onOpenGoal,
  showDismissedSetupGoals = false,
}: OperationsGuidedReplyProps) {
  const { t, locale } = useLocalizedText();
  const setup = useClinicGetSetupProgress({ query: { retry: false, enabled: showDismissedSetupGoals } });
  const dismissedGoals = showDismissedSetupGoals && setup.data?.dismissed && setup.data.goals.some((goal) => !goal.done)
    ? setup.data
    : null;
  return (
    <>
      {dismissedGoals && onOpenGoal ? (
        <div className="px-4">
          <GoalChecklist
            goals={dismissedGoals.goals.map((goal) => ({ key: goal.goal, label: t(`clinic.setup.goals.${goal.goal}`), done: goal.done }))}
            nextGoal={dismissedGoals.nextGoal ?? null}
            onSelect={onOpenGoal}
          />
        </div>
      ) : null}
      {actionGroups.map((group) => (
        <OperationsActionGroup key={`${group.agentRunId}:${group.actionIds[0]}`} actionIds={group.actionIds} onReplaced={replaceAction} />
      ))}
      {question && !isPending ? (
        <InteractiveOptionPicker
          key={turnKey}
          question={question}
          onAnswer={(keys) => {
            const labels = question.options.filter((option) => keys.includes(option.key)).map((option) => option.label);
            submit(labels.join(locale === "fa" ? "، " : ", "));
          }}
        />
      ) : null}
      {nextAction && onOpenGoal && !isPending ? (
        <div>
          <Tooltip>
            <Button variant="outline" onPress={() => onOpenGoal(nextAction)}>
              <FaIcon icon="arrow-up-right-from-square" aria-hidden="true" />
              {t("clinic.operationsAssistant.nextStep", { goal: t(`clinic.setup.goals.${nextAction}`) })}
            </Button>
            <Tooltip.Content>{t("clinic.operationsAssistant.nextStepTooltip")}</Tooltip.Content>
          </Tooltip>
        </div>
      ) : null}
    </>
  );
}
