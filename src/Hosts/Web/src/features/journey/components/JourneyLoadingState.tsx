import { Card, Spinner } from "@heroui/react";
import { SurfaceCard } from "@/design-system";
import { journeyUiText } from "../types/journeyUiText";

interface JourneyLoadingStateProps {
  title?: string;
  description?: string;
}

export function JourneyLoadingState({
  title = journeyUiText.loadingTitle,
  description = journeyUiText.loadingDescription,
}: JourneyLoadingStateProps) {
  return (
    <main className="ds-app-shell ds-centered-shell">
      <SurfaceCard className="w-full max-w-md">
        <Card.Content className="flex flex-col items-center gap-4 p-8 text-center">
          <Spinner aria-label={title} />
          <div className="flex flex-col gap-2">
            <h1 className="text-xl font-semibold text-foreground">{title}</h1>
            <p className="text-sm leading-6 text-foreground/70">{description}</p>
          </div>
        </Card.Content>
      </SurfaceCard>
    </main>
  );
}
