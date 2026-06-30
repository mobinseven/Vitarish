"use client";

import { Button } from "@heroui/react";
import { CenteredSurface } from "@/design-system";
import { useLogout } from "@/hooks/auth/useLogout";
import { StatusMessage } from "@/components/auth/StatusMessage";

export default function LogoutPage() {
  const { logout, isLoading, errorMessage } = useLogout();

  return (
    <CenteredSurface title="Sign Out" description="Are you sure you want to sign out?">
      <div className="flex flex-col gap-4">
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
      </div>
    </CenteredSurface>
  );
}
