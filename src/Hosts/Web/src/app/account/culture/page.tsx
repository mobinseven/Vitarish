"use client";

import { useState, useCallback } from "react";
import { Card, CardBody, CardHeader, Button, Link } from "@heroui/react";
import { useRouter } from "next/navigation";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import {
  useUserCultureQuery,
  useSetUserCultureMutation,
} from "@/api/generated/users/users";
import { StatusMessage } from "@/components/auth/StatusMessage";

const cultures = [
  { value: "en-US", label: "English", nativeName: "English" },
  { value: "fa-IR", label: "Persian", nativeName: "فارسی" },
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
    <div className="max-w-md mx-auto">
      <Card>
        <CardHeader>
          <h1 className="text-2xl font-bold">Language</h1>
        </CardHeader>
        <CardBody className="flex flex-col gap-4">
          {setCulture.isError && (
            <StatusMessage message="Failed to update language" />
          )}
          {success && (
            <StatusMessage message="Language updated successfully" />
          )}

          <div className="flex flex-col gap-2">
            {cultures.map((culture) => (
              <Button
                key={culture.value}
                variant={selected === culture.value ? "solid" : "flat"}
                color={selected === culture.value ? "primary" : "default"}
                onPress={() => setSelected(culture.value)}
                className="w-full justify-start"
              >
                {culture.nativeName}
              </Button>
            ))}
          </div>

          <Button
            color="primary"
            onPress={handleSave}
            isLoading={setCulture.isPending}
            className="w-full"
          >
            Save
          </Button>

          <Link href="/account" className="text-sm">
            Back to settings
          </Link>
        </CardBody>
      </Card>
    </div>
  );
}
