"use client";

import { useState } from "react";
import { Alert, Button, Card, Checkbox } from "@heroui/react";

export function ConsentReviewPanel() {
  const [acceptedRequiredConsents, setAcceptedRequiredConsents] = useState(false);

  return (
    <div className="flex flex-col gap-5">
      <Card className="ds-surface-card ds-surface-card-default border border-[var(--border)] p-0">
        <Card.Content className="flex flex-col gap-4 p-4">
          <h2 className="text-base font-semibold text-foreground">Required terms and privacy review</h2>
          <p className="text-sm leading-6 text-foreground/70">
            Review the required terms and privacy notices before continuing. Persisting consent is not connected yet, so this page does not submit acceptance as final account consent.
          </p>
          <hr className="border-[var(--border)]" />
          <Checkbox
            isSelected={acceptedRequiredConsents}
            onChange={setAcceptedRequiredConsents}
          >
            <Checkbox.Content>
              <Checkbox.Control>
                <Checkbox.Indicator />
              </Checkbox.Control>
              I have reviewed the required terms and privacy notices.
            </Checkbox.Content>
          </Checkbox>
        </Card.Content>
      </Card>
      <Alert status="warning">
        <Alert.Indicator />
        <Alert.Content>Consent submission is pending a real consent API and is intentionally disabled.</Alert.Content>
      </Alert>
      <Button isDisabled={!acceptedRequiredConsents} variant="primary" className="w-full">
        Continue when consent API is connected
      </Button>
    </div>
  );
}
