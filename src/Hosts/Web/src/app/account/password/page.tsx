"use client";

import { useState, useCallback } from "react";
import { Card, CardBody, CardHeader, Input, Button, Link } from "@heroui/react";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { apiFetch } from "@/api/client/apiFetch";
import { StatusMessage } from "@/components/auth/StatusMessage";

export default function PasswordChangePage() {
  useAuthSession();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const handleSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      setError(null);
      setSuccess(false);

      if (newPassword !== confirmPassword) {
        setError("Passwords do not match");
        return;
      }

      if (newPassword.length < 6) {
        setError("Password must be at least 6 characters");
        return;
      }

      setIsLoading(true);
      try {
        await apiFetch<{ succeeded: boolean }>(
          "/api/modules/authentication/account/password",
          {
            method: "POST",
            body: JSON.stringify({ currentPassword, newPassword }),
          }
        );
        setSuccess(true);
        setCurrentPassword("");
        setNewPassword("");
        setConfirmPassword("");
      } catch (err: unknown) {
        setError(err instanceof Error ? err.message : "Failed to change password");
      } finally {
        setIsLoading(false);
      }
    },
    [currentPassword, newPassword, confirmPassword]
  );

  return (
    <div className="max-w-md mx-auto">
      <Card>
        <CardHeader>
          <h1 className="text-2xl font-bold">Change Password</h1>
        </CardHeader>
        <CardBody className="flex flex-col gap-4">
          {error && <StatusMessage message={error} />}
          {success && <StatusMessage message="Password changed successfully" />}
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <Input
              label="Current Password"
              type="password"
              value={currentPassword}
              onValueChange={setCurrentPassword}
              autoComplete="current-password"
            />
            <Input
              label="New Password"
              type="password"
              value={newPassword}
              onValueChange={setNewPassword}
              isRequired
              autoComplete="new-password"
            />
            <Input
              label="Confirm New Password"
              type="password"
              value={confirmPassword}
              onValueChange={setConfirmPassword}
              isRequired
              autoComplete="new-password"
            />
            <Button
              type="submit"
              color="primary"
              isLoading={isLoading}
              className="w-full"
            >
              Change Password
            </Button>
          </form>
          <Link href="/account" className="text-sm">
            Back to settings
          </Link>
        </CardBody>
      </Card>
    </div>
  );
}
