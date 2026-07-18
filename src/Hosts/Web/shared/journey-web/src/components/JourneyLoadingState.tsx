"use client";

import { Card, Spinner } from "@heroui/react";
import { useLocalizedText } from "@vitarish/web-localization";
import { CenteredRegion } from "@vitarish/web-ui/layout";

interface JourneyLoadingStateProps {
  title?: string;
  description?: string;
}

export function JourneyLoadingState({
  title,
  description,
}: JourneyLoadingStateProps) {
  const { t } = useLocalizedText();
  const resolvedTitle = title ?? t("journey.loading.title");
  const resolvedDescription = description ?? t("journey.loading.description");

  return (
    <CenteredRegion>
      <Card className="w-full max-w-md">
        <Card.Content className="flex flex-col items-center gap-4 p-8 text-center">
          <Spinner aria-label={resolvedTitle} />
          <div className="flex flex-col gap-2">
            <h1 className="text-xl font-semibold text-foreground">{resolvedTitle}</h1>
            <p className="text-sm leading-6 text-foreground/70">{resolvedDescription}</p>
          </div>
        </Card.Content>
      </Card>
    </CenteredRegion>
  );
}
