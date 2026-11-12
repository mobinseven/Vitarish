"use client";

import { ChatMessage } from "@heroui-pro/react";
import { Button, Card, Tooltip } from "@heroui/react";
import { useQueryClient } from "@tanstack/react-query";
import { GoalChecklist } from "@vitarish/intelligence";
import { FaIcon } from "@vitarish/ui";
import { useState } from "react";
import {
  getClinicGetSetupProgressQueryKey,
  useClinicDismissSetupGuide,
} from "../../api/generated/clinic/clinic";
import { ClinicSetupGoal, type ClinicSetupProgressResponse } from "../../api/generated/clinic/model";
import { useLocalizedText } from "../../localization";
import type { NavigationIntent } from "../../navigation";
import { OperationsAssistantAside } from "../clinic-assistant/OperationsAssistantAside";

/** The existing screen each setup goal opens, for people who prefer the menus. */
const goalIntents: Record<ClinicSetupGoal, NavigationIntent> = {
  [ClinicSetupGoal.ServicesAndProviders]: { kind: "serviceIndex" },
  [ClinicSetupGoal.ShiftsAndBookingRules]: { kind: "ownerShiftIndex" },
  [ClinicSetupGoal.StaffUsers]: { kind: "clinicSettingsUsers" },
  [ClinicSetupGoal.ClinicProfile]: { kind: "clinicSettings" },
};

function isSetupGoal(value: string): value is ClinicSetupGoal {
  return Object.values(ClinicSetupGoal).includes(value as ClinicSetupGoal);
}

export interface ClinicSetupHomeProps {
  progress: ClinicSetupProgressResponse;
  onNavigate: (intent: NavigationIntent) => void;
}

/**
 * The owner's first-run home: the operations assistant in the main column guides setup, the goal list sits beside
 * it, and "Later" returns every owner of the clinic to today's dashboard. The welcome is built from setup progress
 * without a model call; tapping a suggestion sends it as an ordinary message.
 */
export function ClinicSetupHome({ progress, onNavigate }: ClinicSetupHomeProps) {
  const { t } = useLocalizedText();
  const queryClient = useQueryClient();
  const [laterError, setLaterError] = useState(false);
  const dismiss = useClinicDismissSetupGuide({
    mutation: { onSuccess: () => queryClient.invalidateQueries({ queryKey: getClinicGetSetupProgressQueryKey() }) },
  });
  const nextGoal = progress.nextGoal && isSetupGoal(progress.nextGoal) ? progress.nextGoal : null;
  const openGoals = progress.goals.filter((goal) => !goal.done).map((goal) => goal.goal);
  const navigateToGoal = (key: string) => { if (isSetupGoal(key)) onNavigate(goalIntents[key]); };

  const later = async () => {
    setLaterError(false);
    try { await dismiss.mutateAsync(); } catch { setLaterError(true); }
  };

  return (
    <section className="flex flex-col gap-4" aria-labelledby="clinic-setup-title">
      <header className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 id="clinic-setup-title" className="text-xl font-semibold">{t("clinic.setup.title")}</h2>
          <p className="text-muted">{t("clinic.setup.description")}</p>
        </div>
        <Tooltip>
          <Button variant="tertiary" isPending={dismiss.isPending} isDisabled={dismiss.isPending} onPress={() => void later()}>
            <FaIcon icon="clock" aria-hidden="true" />{t("clinic.setup.later")}
          </Button>
          <Tooltip.Content>{t("clinic.setup.laterTooltip")}</Tooltip.Content>
        </Tooltip>
      </header>
      {laterError ? <p role="alert" className="text-danger">{t("clinic.setup.laterError")}</p> : null}
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <Card className="lg:col-span-2 h-[36rem] overflow-hidden p-0">
          <OperationsAssistantAside
            isOpen
            onNextAction={navigateToGoal}
            welcome={(submit) => (
              <div className="flex flex-col gap-3">
                <ChatMessage.Assistant>
                  <ChatMessage.Avatar show alt={t("clinic.operationsAssistant.title")} fallback="AI" />
                  <ChatMessage.Body>{t(nextGoal ? `clinic.setup.welcome.${nextGoal}` : "clinic.setup.welcome.done")}</ChatMessage.Body>
                </ChatMessage.Assistant>
                {openGoals.length > 0 ? (
                  <div className="flex flex-wrap gap-2">
                    {openGoals.map((goal) => (
                      <Button key={goal} variant={goal === nextGoal ? "primary" : "outline"} onPress={() => submit(t(`clinic.setup.chips.${goal}`))}>
                        {t(`clinic.setup.chips.${goal}`)}
                      </Button>
                    ))}
                  </div>
                ) : null}
              </div>
            )}
          />
        </Card>
        <Card className="p-4">
          <GoalChecklist
            goals={progress.goals.map((goal) => ({ key: goal.goal, label: t(`clinic.setup.goals.${goal.goal}`), done: goal.done }))}
            nextGoal={nextGoal}
            onSelect={navigateToGoal}
          />
        </Card>
      </div>
    </section>
  );
}
