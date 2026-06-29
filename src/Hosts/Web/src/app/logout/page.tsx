"use client";

import { Card, Button } from "@heroui/react";
import { useLogout } from "@/hooks/auth/useLogout";
import { StatusMessage } from "@/components/auth/StatusMessage";

export default function LogoutPage() {
  const { logout, isLoading, errorMessage } = useLogout();

  return (
    <div className="auth-bg flex min-h-screen items-center justify-center p-4">
      <Card className="glass-card w-full max-w-md">
        <Card.Header className="flex flex-col items-center gap-2 pb-2">
          <h1 className="text-2xl font-bold">Sign Out</h1>
          <p className="text-sm text-muted">
            Are you sure you want to sign out?
          </p>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          {errorMessage && <StatusMessage message={errorMessage} />}
          <Button
            variant="danger"
            isPending={isLoading}
            onPress={() => logout()}
            className="w-full"
          >
            Sign Out
          </Button>
          <Button
            variant="tertiary"
            onPress={() => window.history.back()}
            isDisabled={isLoading}
            className="w-full"
          >
            Cancel
          </Button>
        </Card.Content>
      </Card>
    </div>
  );
}
