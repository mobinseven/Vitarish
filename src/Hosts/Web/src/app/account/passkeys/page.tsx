"use client";

import { useState, useCallback, useEffect } from "react";
import { Card, Button, Link } from "@heroui/react";
import { SurfaceCard } from "@/design-system";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { Routes } from "@/app/routes";
import {
  useUsersListWebAuthnCredentials,
  useUsersBeginWebAuthnRegister,
  useUsersCompleteWebAuthnRegister,
  useUsersDeleteWebAuthnCredential,
} from "@/api/generated/users/users";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { startRegistration } from "@/lib/webauthn";

export default function PasskeysPage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const { data: credentials, isLoading: isLoadingCredentials } =
    useUsersListWebAuthnCredentials(userId);
  const beginRegister = useUsersBeginWebAuthnRegister();
  const completeRegister = useUsersCompleteWebAuthnRegister();
  const deleteCredential = useUsersDeleteWebAuthnCredential();

  const [isSupported, setIsSupported] = useState<boolean | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    setIsSupported(
      typeof window !== "undefined" &&
        window.PublicKeyCredential !== undefined
    );
  }, []);

  const handleRegister = useCallback(async () => {
    setError(null);
    setSuccess(null);
    try {
      const optionsJson = await beginRegister.mutateAsync({ userId });
      const options = JSON.parse(optionsJson);
      const credential = await startRegistration(options);
      await completeRegister.mutateAsync({ userId, data: { attestationResponseJson: JSON.stringify(credential) } });
      setSuccess("Passkey registered successfully");
    } catch (err: unknown) {
      if (err instanceof Error && err.name === "NotAllowedError") {
        setError("Registration cancelled");
      } else {
        setError(err instanceof Error ? err.message : "Failed to register passkey");
      }
    }
  }, [beginRegister, completeRegister, userId]);

  const handleDelete = useCallback(
    async (credentialId: string) => {
      setError(null);
      setSuccess(null);
      try {
        await deleteCredential.mutateAsync({ userId, credentialId });
        setSuccess("Passkey deleted");
      } catch (err: unknown) {
        setError(err instanceof Error ? err.message : "Failed to delete passkey");
      }
    },
    [deleteCredential, userId]
  );

  return (
    <div className="max-w-md mx-auto">
      <SurfaceCard>
        <Card.Header>
          <h1 className="text-2xl font-bold">Passkeys</h1>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          {error && <StatusMessage message={error} />}
          {success && <StatusMessage message={success} type="success" />}

          {isSupported === false && (
            <StatusMessage message="Passkeys are not supported in this browser" />
          )}

          {isLoadingCredentials ? (
            <p className="text-sm text-muted">Loading passkeys...</p>
          ) : credentials && credentials.length > 0 ? (
            <div className="flex flex-col gap-2">
              {credentials.map((cred) => (
                <div
                  key={cred.id ?? `cred-${cred.friendlyName ?? "unknown"}`}
                  className="flex items-center justify-between p-3 border border-border rounded-lg"
                >
                  <span className="text-sm">
                    {cred.friendlyName ?? "Unnamed passkey"}
                  </span>
                  <Button
                    variant="danger-soft"
                    size="sm"
                    onPress={() => cred.id && handleDelete(cred.id)}
                    isPending={deleteCredential.isPending}
                  >
                    Delete
                  </Button>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-sm text-muted">No passkeys registered</p>
          )}

          {isSupported && (
            <Button
              onPress={handleRegister}
              isPending={beginRegister.isPending || completeRegister.isPending}
              className="w-full"
            >
              <i className="fa-solid fa-plus" />
              Register New Passkey
            </Button>
          )}

          <Link href={Routes.Account.Index} className="text-sm">
            Back to settings
          </Link>
        </Card.Content>
      </SurfaceCard>
    </div>
  );
}
