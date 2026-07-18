"use client";

import { Alert, Card, Button } from "@heroui/react";
import { useLocalizedText } from "@vitarish/web-localization";
import { CenteredRegion } from "@vitarish/web-ui/layout";

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
            <Button variant="primary" onPress={onRetry}>
              {t("journey.error.retryAction")}
            </Button>
          ) : null}
        </Card.Content>
      </Card>
    </CenteredRegion>
  );
}
