"use client";

import { Alert, Card, Button, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/ui";
import { useLocalizedText } from "@vitarish/localization";
import { CenteredRegion } from "@vitarish/ui/layout";
import { navigationTransitionStates, useNavigationTransitionParticipant } from "@vitarish/ui/navigation-runtime";

interface JourneyErrorStateProps {
  title?: string;
  description?: string;
  onRetry?: () => void;
}

export function JourneyErrorState({
  title,
  description,
  onRetry,
}: JourneyErrorStateProps) {
  useNavigationTransitionParticipant(false, navigationTransitionStates.Failed);
  const { t } = useLocalizedText();
  const resolvedTitle = title ?? t("journey.error.title");
  const resolvedDescription = description ?? t("journey.error.description");

  return (
    <CenteredRegion>
      <Card className="w-full max-w-md">
        <Card.Content className="flex flex-col gap-4 p-6">
          <Alert status="danger">
            <Alert.Indicator />
            <Alert.Content>
              <strong>{resolvedTitle}</strong>
              <span className="block text-sm">{resolvedDescription}</span>
            </Alert.Content>
          </Alert>
          {onRetry ? (
            <Tooltip><Button variant="tertiary" onPress={onRetry}>
              <FaIcon icon="rotate-right" aria-hidden="true" />
              {t("journey.error.retryAction")}
            </Button><Tooltip.Content>{t("journey.error.retryAction")}</Tooltip.Content></Tooltip>
          ) : null}
        </Card.Content>
      </Card>
    </CenteredRegion>
  );
}
