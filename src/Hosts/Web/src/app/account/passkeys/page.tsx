"use client";

import { useState, useCallback, useEffect } from "react";
import { Card, Button, Link } from "@heroui/react";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import {
  useWebAuthnCredentialsQuery,
  useBeginWebAuthnRegisterMutation,
  useCompleteWebAuthnRegisterMutation,
  useDeleteWebAuthnCredentialMutation,
} from "@/api/generated/users/users";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { startRegistration } from "@/lib/webauthn";

export default function PasskeysPage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const { data: credentials, isLoading: isLoadingCredentials } =
    useWebAuthnCredentialsQuery(userId);
  const beginRegister = useBeginWebAuthnRegisterMutation(userId);
  const completeRegister = useCompleteWebAuthnRegisterMutation(userId);
  const deleteCredential = useDeleteWebAuthnCredentialMutation(userId);

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
      const optionsJson = await beginRegister.mutateAsync();
      const options = JSON.parse(optionsJson);
      const credential = await startRegistration(options);
      await completeRegister.mutateAsync(JSON.stringify(credential));
      setSuccess("Passkey registered successfully");
    } catch (err: unknown) {
      if (err instanceof Error && err.name === "NotAllowedError") {
        setError("Registration cancelled");
      } else {
        setError(err instanceof Error ? err.message : "Failed to register passkey");
      }
    }
  }, [beginRegister, completeRegister]);

  const handleDelete = useCallback(
    async (credentialId: string) => {
      setError(null);
      setSuccess(null);
      try {
        await deleteCredential.mutateAsync(credentialId);
        setSuccess("Passkey deleted");
      } catch (err: unknown) {
        setError(err instanceof Error ? err.message : "Failed to delete passkey");
      }
    },
    [deleteCredential]
  );

  return (
    <div className="max-w-md mx-auto">
      <Card className="glass-card">
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
              {credentials.map((cred: { id: string; friendlyName: string | null }) => (
                <div
                  key={cred.id}
                  className="flex items-center justify-between p-3 border border-border rounded-lg"
                >
                  <span className="text-sm">
                    {cred.friendlyName ?? "Unnamed passkey"}
                  </span>
                  <Button
                    variant="danger-soft"
                    size="sm"
                    onPress={() => handleDelete(cred.id)}
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

          <Link href="/account" className="text-sm">
            Back to settings
          </Link>
        </Card.Content>
      </Card>
    </div>
  );
}
