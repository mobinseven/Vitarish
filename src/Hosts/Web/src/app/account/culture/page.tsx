"use client";

import { useState, useCallback } from "react";
import { Button, Link } from "@heroui/react";
import { useRouter } from "next/navigation";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { Routes } from "@/app/routes";
import {
  useUserCultureQuery,
  useSetUserCultureMutation,
} from "@/api/generated/users/users";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { OptionSelector } from "@/components/account/OptionSelector";

const cultures = [
  { value: "en-US", label: "English" },
  { value: "fa-IR", label: "فارسی" },
];

export default function CulturePage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const router = useRouter();
  const { data: currentCulture } = useUserCultureQuery(userId);
  const setCulture = useSetUserCultureMutation(userId);

  const [selected, setSelected] = useState(currentCulture ?? "en-US");
  const [success, setSuccess] = useState(false);

  const handleSave = useCallback(async () => {
    setSuccess(false);
    await setCulture.mutateAsync({ culture: selected });
    setSuccess(true);
    router.refresh();
  }, [selected, setCulture, router]);

  return (
    <div className="max-w-md mx-auto flex flex-col gap-4">
      <OptionSelector
        title="Language"
        options={cultures}
        selected={selected}
        onSelect={setSelected}
      />
      {setCulture.isError && (
        <StatusMessage message="Failed to update language" />
      )}
      {success && (
        <StatusMessage message="Language updated successfully" type="success" />
      )}
      <Button
        onPress={handleSave}
        isPending={setCulture.isPending}
        className="w-full"
      >
        Save
      </Button>
      <Link href={Routes.Account.Index} className="text-sm">
        Back to settings
      </Link>
    </div>
  );
}
