"use client";

import { useState } from "react";
import { Alert, Button, Card, Spinner } from "@heroui/react";
import { useJourneyWorkspaceOptions } from "../hooks/useJourneyWorkspaceOptions";

export function WorkspaceSelector() {
  const { options, isLoading, unavailableReason } = useJourneyWorkspaceOptions();
  const [selectedWorkspaceId, setSelectedWorkspaceId] = useState<string>();

  if (isLoading) {
    return <Spinner aria-label="Loading workspace options" />;
  }

  if (options.length === 0) {
    return (
      <Alert status="warning">
        <Alert.Indicator />
        <Alert.Content>{unavailableReason}</Alert.Content>
      </Alert>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="grid gap-3">
        {options.map((option) => (
          <Card key={option.id} variant="transparent" className="border border-[var(--border)] p-0">
            <Card.Content className="p-4">
              <Button
                variant={selectedWorkspaceId === option.id ? "primary" : "secondary"}
                className="w-full justify-start"
                onPress={() => setSelectedWorkspaceId(option.id)}
              >
                <span className="flex flex-col items-start gap-1 text-start">
                  <span>{option.name}</span>
                  <span className="text-sm font-normal text-foreground/70">{option.description}</span>
                </span>
              </Button>
            </Card.Content>
          </Card>
        ))}
      </div>
      <Button isDisabled={!selectedWorkspaceId} className="w-full">Continue</Button>
    </div>
  );
}
