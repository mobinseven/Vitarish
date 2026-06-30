"use client";

import { Button } from "@heroui/react";
import { CenteredSurface } from "@/design-system";
import { useLogout } from "@/hooks/auth/useLogout";
import { StatusMessage } from "@/components/auth/StatusMessage";

export default function LogoutPage() {
  const { logout, isLoading, isContextLoading, contextStatus, errorMessage } = useLogout();

  return (
    <CenteredSurface title="Sign Out" description="Are you sure you want to sign out?">
      <div className="flex flex-col gap-4">
        {contextStatus === "already-signed-out" && (
          <StatusMessage message="Your session has already ended. You can return to sign in." />
        )}
        {contextStatus === "unavailable" && (
          <StatusMessage message="We could not load logout details, but you can still try signing out." />
        )}
        {errorMessage && <StatusMessage message={errorMessage} />}
        <Button
          variant="danger"
          isPending={isLoading || isContextLoading}
          onPress={() => logout()}
          className="w-full"
        >
          Sign Out
        </Button>
        <Button
          variant="tertiary"
          onPress={() => window.history.back()}
          isDisabled={isLoading || isContextLoading}
          className="w-full"
        >
          Cancel
        </Button>
      </div>
    </CenteredSurface>
  );
}
