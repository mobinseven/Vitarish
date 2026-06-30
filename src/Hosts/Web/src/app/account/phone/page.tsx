"use client";

import { useState, useCallback } from "react";
import { Card, TextField, Label, Input, Button, Link } from "@heroui/react";
import { SurfaceCard } from "@/design-system";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { Routes } from "@/app/routes";
import { useUsersGetProfile } from "@/api/generated/users/users";
import { apiFetch } from "@/api/client/apiFetch";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { OtpCodeInput } from "@/components/auth/OtpCodeInput";

export default function PhoneChangePage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const { data: profile } = useUsersGetProfile(userId);
  const [step, setStep] = useState<"phone" | "code">("phone");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [code, setCode] = useState("");
  const [challengeId, setChallengeId] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const handlePhoneSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      setError(null);
      setIsLoading(true);
      try {
        const result = await apiFetch<{ challengeId: string }>(
          "/api/modules/authentication/account/phone/begin",
          {
            method: "POST",
            body: JSON.stringify({ phoneNumber }),
          }
        );
        setChallengeId(result.challengeId);
        setStep("code");
      } catch (err: unknown) {
        setError(err instanceof Error ? err.message : "Failed to send code");
      } finally {
        setIsLoading(false);
      }
    },
    [phoneNumber]
  );

  const handleCodeSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      setError(null);
      setIsLoading(true);
      try {
        await apiFetch<{ succeeded: boolean }>(
          "/api/modules/authentication/account/phone/complete",
          {
            method: "POST",
            body: JSON.stringify({ challengeId, code }),
          }
        );
        setSuccess(true);
        setStep("phone");
        setPhoneNumber("");
        setCode("");
      } catch (err: unknown) {
        setError(err instanceof Error ? err.message : "Failed to verify code");
      } finally {
        setIsLoading(false);
      }
    },
    [challengeId, code]
  );

  return (
    <div className="max-w-md mx-auto">
      <SurfaceCard>
        <Card.Header>
          <h1 className="text-2xl font-bold">Change Phone Number</h1>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          {profile?.phoneNumber && (
            <p className="text-sm text-muted">
              Current: {profile.phoneNumber}
            </p>
          )}
          {error && <StatusMessage message={error} />}
          {success && (
            <StatusMessage message="Phone number updated successfully" type="success" />
          )}

          {step === "phone" ? (
            <form onSubmit={handlePhoneSubmit} className="flex flex-col gap-4">
              <TextField name="phone" isRequired>
                <Label>New Phone Number</Label>
                <Input
                  value={phoneNumber}
                  onChange={(e) => setPhoneNumber(e.target.value)}
                  autoComplete="tel"
                  inputMode="tel"
                />
              </TextField>
              <Button
                type="submit"
                isPending={isLoading}
                className="w-full"
              >
                Send Code
              </Button>
            </form>
          ) : (
            <form onSubmit={handleCodeSubmit} className="flex flex-col gap-4">
              <p className="text-sm text-muted">
                Code sent to {phoneNumber}
              </p>
              <OtpCodeInput
                value={code}
                onChange={setCode}
                isDisabled={isLoading}
              />
              <Button
                type="submit"
                isPending={isLoading}
                isDisabled={code.length !== 6}
                className="w-full"
              >
                Verify Code
              </Button>
              <Button
                variant="tertiary"
                onPress={() => {
                  setStep("phone");
                  setCode("");
                }}
                isDisabled={isLoading}
                className="w-full"
              >
                Use a different number
              </Button>
            </form>
          )}

          <Link href={Routes.Account.Index} className="text-sm">
            Back to settings
          </Link>
        </Card.Content>
      </SurfaceCard>
    </div>
  );
}
