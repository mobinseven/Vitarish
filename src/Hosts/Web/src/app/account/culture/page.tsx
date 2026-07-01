"use client";

import { useState, useCallback } from "react";
import { Button, Link } from "@heroui/react";
import { useRouter } from "next/navigation";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { Routes } from "@/app/routes";
import {
  useUsersGetCulture,
  useUsersSetCulture,
} from "@/api/generated/users/users";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { OptionSelector } from "@/components/account/OptionSelector";
import { useLocalizedText } from "@/localization";

export default function CulturePage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const router = useRouter();
  const { data: currentCulture } = useUsersGetCulture(userId);
  const { t } = useLocalizedText();
  const setCulture = useUsersSetCulture();

  const [selected, setSelected] = useState(currentCulture ?? "en-US");
  const [success, setSuccess] = useState(false);

  const handleSave = useCallback(async () => {
    setSuccess(false);
    await setCulture.mutateAsync({ userId, data: { culture: selected } });
    setSuccess(true);
    router.refresh();
  }, [selected, setCulture, router, userId]);

  const cultures = [
    { value: "en-US", label: t("account.culture.english") },
    { value: "fa-IR", label: t("account.culture.persian") },
  ];

  return (
    <div className="max-w-md mx-auto flex flex-col gap-4">
      <OptionSelector
        title={t("account.culture.title")}
        options={cultures}
        selected={selected}
        onSelect={setSelected}
      />
      {setCulture.isError && (
        <StatusMessage message={t("account.culture.updateFailed")} />
      )}
      {success && (
        <StatusMessage message={t("account.culture.updateSucceeded")} type="success" />
      )}
      <Button
        onPress={handleSave}
        isPending={setCulture.isPending}
        className="w-full"
      >
        {t("common.actions.save")}
      </Button>
      <Link href={Routes.Account.Index} className="text-sm">
        {t("common.actions.backToSettings")}
      </Link>
    </div>
  );
}
