"use client";

import { useState, useCallback } from "react";
import { Card, CardBody, CardHeader, Input, Button, Link } from "@heroui/react";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { useUserProfileQuery, useUpdateUserProfileMutation } from "@/api/generated/users/users";
import { StatusMessage } from "@/components/auth/StatusMessage";

export default function NamePage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const { data: profile } = useUserProfileQuery(userId);
  const updateProfile = useUpdateUserProfileMutation(userId);

  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [initialized, setInitialized] = useState(false);
  const [success, setSuccess] = useState(false);

  if (profile && !initialized) {
    setFirstName(profile.firstName ?? "");
    setLastName(profile.lastName ?? "");
    setInitialized(true);
  }

  const handleSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      setSuccess(false);
      await updateProfile.mutateAsync({ firstName, lastName });
      setSuccess(true);
    },
    [firstName, lastName, updateProfile]
  );

  return (
    <div className="max-w-md mx-auto">
      <Card>
        <CardHeader>
          <h1 className="text-2xl font-bold">Edit Name</h1>
        </CardHeader>
        <CardBody className="flex flex-col gap-4">
          {updateProfile.isError && (
            <StatusMessage message="Failed to update name" />
          )}
          {success && (
            <StatusMessage message="Name updated successfully" />
          )}
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <Input
              label="First Name"
              value={firstName}
              onValueChange={setFirstName}
              isRequired
            />
            <Input
              label="Last Name"
              value={lastName}
              onValueChange={setLastName}
              isRequired
            />
            <Button
              type="submit"
              color="primary"
              isLoading={updateProfile.isPending}
              className="w-full"
            >
              Save
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
