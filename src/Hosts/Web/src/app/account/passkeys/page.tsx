"use client";

import { useState, useCallback, useEffect } from "react";
import { Card, CardBody, CardHeader, Button, Link } from "@heroui/react";
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
      <Card>
        <CardHeader>
          <h1 className="text-2xl font-bold">Passkeys</h1>
        </CardHeader>
        <CardBody className="flex flex-col gap-4">
          {error && <StatusMessage message={error} />}
          {success && <StatusMessage message={success} />}

          {isSupported === false && (
            <StatusMessage message="Passkeys are not supported in this browser" />
          )}

          {isLoadingCredentials ? (
            <p className="text-sm text-default-500">Loading passkeys...</p>
          ) : credentials && credentials.length > 0 ? (
            <div className="flex flex-col gap-2">
              {credentials.map((cred: { id: string; friendlyName: string | null }) => (
                <div
                  key={cred.id}
                  className="flex items-center justify-between p-3 border border-divider rounded-lg"
                >
                  <span className="text-sm">
                    {cred.friendlyName ?? "Unnamed passkey"}
                  </span>
                  <Button
                    color="danger"
                    size="sm"
                    variant="flat"
                    onPress={() => handleDelete(cred.id)}
                    isLoading={deleteCredential.isPending}
                  >
                    Delete
                  </Button>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-sm text-default-500">No passkeys registered</p>
          )}

          {isSupported && (
            <Button
              color="primary"
              onPress={handleRegister}
              isLoading={beginRegister.isPending || completeRegister.isPending}
              startContent={<i className="fa-solid fa-plus" />}
              className="w-full"
            >
              Register New Passkey
            </Button>
          )}

          <Link href="/account" className="text-sm">
            Back to settings
          </Link>
        </CardBody>
      </Card>
    </div>
  );
}
