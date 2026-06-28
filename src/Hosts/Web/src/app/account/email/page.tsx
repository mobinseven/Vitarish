"use client";

import { useState, useCallback } from "react";
import { Card, CardBody, CardHeader, Input, Button, Link } from "@heroui/react";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { useUserProfileQuery } from "@/api/generated/users/users";
import { apiFetch } from "@/api/client/apiFetch";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { OtpCodeInput } from "@/components/auth/OtpCodeInput";

export default function EmailChangePage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const { data: profile } = useUserProfileQuery(userId);
  const [step, setStep] = useState<"email" | "code">("email");
  const [email, setEmail] = useState("");
  const [code, setCode] = useState("");
  const [challengeId, setChallengeId] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const handleEmailSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      setError(null);
      setIsLoading(true);
      try {
        const result = await apiFetch<{ challengeId: string }>(
          "/api/modules/authentication/account/email/begin",
          {
            method: "POST",
            body: JSON.stringify({ email }),
          }
        );
        setChallengeId(result.challengeId);
        setStep("code");
      } catch (err: any) {
        setError(err.message ?? "Failed to send code");
      } finally {
        setIsLoading(false);
      }
    },
    [email]
  );

  const handleCodeSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      setError(null);
      setIsLoading(true);
      try {
        await apiFetch<{ succeeded: boolean }>(
          "/api/modules/authentication/account/email/complete",
          {
            method: "POST",
            body: JSON.stringify({ challengeId, code }),
          }
        );
        setSuccess(true);
        setStep("email");
        setEmail("");
        setCode("");
      } catch (err: any) {
        setError(err.message ?? "Failed to verify code");
      } finally {
        setIsLoading(false);
      }
    },
    [challengeId, code]
  );

  return (
    <div className="max-w-md mx-auto">
      <Card>
        <CardHeader>
          <h1 className="text-2xl font-bold">Change Email</h1>
        </CardHeader>
        <CardBody className="flex flex-col gap-4">
          {profile?.email && (
            <p className="text-sm text-default-500">
              Current: {profile.email}
            </p>
          )}
          {error && <StatusMessage message={error} />}
          {success && (
            <StatusMessage message="Email updated successfully" />
          )}

          {step === "email" ? (
            <form onSubmit={handleEmailSubmit} className="flex flex-col gap-4">
              <Input
                label="New Email"
                type="email"
                value={email}
                onValueChange={setEmail}
                isRequired
                autoComplete="email"
              />
              <Button
                type="submit"
                color="primary"
                isLoading={isLoading}
                className="w-full"
              >
                Send Code
              </Button>
            </form>
          ) : (
            <form onSubmit={handleCodeSubmit} className="flex flex-col gap-4">
              <p className="text-sm text-default-500">
                Code sent to {email}
              </p>
              <OtpCodeInput
                value={code}
                onChange={setCode}
                isDisabled={isLoading}
              />
              <Button
                type="submit"
                color="primary"
                isLoading={isLoading}
                isDisabled={code.length !== 6}
                className="w-full"
              >
                Verify Code
              </Button>
              <Button
                variant="flat"
                onPress={() => {
                  setStep("email");
                  setCode("");
                }}
                isDisabled={isLoading}
                className="w-full"
              >
                Use a different email
              </Button>
            </form>
          )}

          <Link href="/account" className="text-sm">
            Back to settings
          </Link>
        </CardBody>
      </Card>
    </div>
  );
}
