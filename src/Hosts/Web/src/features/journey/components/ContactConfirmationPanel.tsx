"use client";

import { Alert, Button } from "@heroui/react";

export function ContactConfirmationPanel() {
  return (
    <div className="flex flex-col gap-5">
      <Alert status="warning">
        <Alert.Indicator />
        <Alert.Content>
          Contact confirmation actions are not available yet. This gate will stay active until a real email or phone confirmation API is connected.
        </Alert.Content>
      </Alert>
      <div className="ds-inline-panel flex flex-col gap-4">
        <h2 className="text-base font-semibold text-foreground">Confirmation options</h2>
        <p className="text-sm leading-6 text-foreground/70">
          We will show email and phone confirmation actions here when the backend exposes supported methods for your account.
        </p>
        <hr className="border-[var(--border)]" />
        <div className="grid gap-4 sm:grid-cols-2">
          <Button variant="secondary" isDisabled>Email confirmation unavailable</Button>
          <Button variant="secondary" isDisabled>Phone confirmation unavailable</Button>
        </div>
      </div>
    </div>
  );
}
