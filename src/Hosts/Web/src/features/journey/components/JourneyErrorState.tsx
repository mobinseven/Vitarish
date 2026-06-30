import { Alert, Card, Button } from "@heroui/react";
import { SurfaceCard } from "@/design-system";
import { journeyUiText } from "../types/journeyUiText";

interface JourneyErrorStateProps {
  title?: string;
  description?: string;
  onRetry?: () => void;
}

export function JourneyErrorState({
  title = journeyUiText.genericErrorTitle,
  description = journeyUiText.genericErrorDescription,
  onRetry,
}: JourneyErrorStateProps) {
  return (
    <main className="ds-app-shell ds-centered-shell">
      <SurfaceCard className="w-full max-w-md">
        <Card.Content className="flex flex-col gap-4 p-6">
          <Alert status="danger">
            <Alert.Indicator />
            <Alert.Content>
              <strong>{title}</strong>
              <span className="block text-sm">{description}</span>
            </Alert.Content>
          </Alert>
          {onRetry ? (
            <Button variant="primary" onPress={onRetry}>
              Try again
            </Button>
          ) : null}
        </Card.Content>
      </SurfaceCard>
    </main>
  );
}
