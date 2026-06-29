"use client";

import { Card } from "@heroui/react";
import { AuthMethodSelector } from "@/components/auth/AuthMethodSelector";

export default function LoginPage() {
  return (
    <div className="auth-bg flex min-h-screen items-center justify-center p-4">
      <Card className="glass-card w-full max-w-md">
        <Card.Header className="flex flex-col items-center gap-2 pb-2">
          <h1 className="text-2xl font-bold">Vitarish</h1>
          <p className="text-sm text-muted">
            Sign in to your account
          </p>
        </Card.Header>
        <Card.Content>
          <AuthMethodSelector />
        </Card.Content>
      </Card>
    </div>
  );
}
