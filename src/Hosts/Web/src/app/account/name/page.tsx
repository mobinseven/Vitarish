"use client";

import { useState, useCallback } from "react";
import { Card, TextField, Label, Input, Button, Link } from "@heroui/react";
import { SurfaceCard } from "@/design-system";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { Routes } from "@/app/routes";
import { useUsersGetProfile, useUsersUpdateProfile } from "@/api/generated/users/users";
import { StatusMessage } from "@/components/auth/StatusMessage";

export default function NamePage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const { data: profile } = useUsersGetProfile(userId);
  const updateProfile = useUsersUpdateProfile();

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
      await updateProfile.mutateAsync({ userId, data: { firstName, lastName } });
      setSuccess(true);
    },
    [firstName, lastName, updateProfile, userId]
  );

  return (
    <div className="max-w-md mx-auto">
      <SurfaceCard>
        <Card.Header>
          <h1 className="text-2xl font-bold">Edit Name</h1>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          {updateProfile.isError && (
            <StatusMessage message="Failed to update name" />
          )}
          {success && (
            <StatusMessage message="Name updated successfully" type="success" />
          )}
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <TextField name="firstName" isRequired>
              <Label>First Name</Label>
              <Input value={firstName} onChange={(e) => setFirstName(e.target.value)} />
            </TextField>
            <TextField name="lastName" isRequired>
              <Label>Last Name</Label>
              <Input value={lastName} onChange={(e) => setLastName(e.target.value)} />
            </TextField>
            <Button
              type="submit"
              isPending={updateProfile.isPending}
              className="w-full"
            >
              Save
            </Button>
          </form>
          <Link href={Routes.Account.Index} className="text-sm">
            Back to settings
          </Link>
        </Card.Content>
      </SurfaceCard>
    </div>
  );
}
