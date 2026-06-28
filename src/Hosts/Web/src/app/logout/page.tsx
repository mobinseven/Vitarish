"use client";

import { Card, CardBody, CardHeader, Button } from "@heroui/react";
import { useLogout } from "@/hooks/auth/useLogout";
import { StatusMessage } from "@/components/auth/StatusMessage";

export default function LogoutPage() {
  const { logout, isLoading, errorMessage } = useLogout();

  return (
    <div className="flex min-h-screen items-center justify-center p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="flex flex-col items-center gap-2 pb-2">
          <h1 className="text-2xl font-bold">Sign Out</h1>
          <p className="text-sm text-default-500">
            Are you sure you want to sign out?
          </p>
        </CardHeader>
        <CardBody className="flex flex-col gap-4">
          {errorMessage && <StatusMessage message={errorMessage} />}
          <Button
            color="danger"
            isLoading={isLoading}
            onPress={() => logout()}
            className="w-full"
          >
            Sign Out
          </Button>
          <Button
            variant="flat"
            onPress={() => window.history.back()}
            isDisabled={isLoading}
            className="w-full"
          >
            Cancel
          </Button>
        </CardBody>
      </Card>
    </div>
  );
}
