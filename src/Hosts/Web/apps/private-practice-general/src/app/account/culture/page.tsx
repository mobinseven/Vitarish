"use client";

import { useCallback } from "react";
import { Button } from "@heroui/react";
import { useRouter } from "next/navigation";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { AccountBackLink, AccountSurface } from "@/components/account/AccountSurface";
import { useAccountCulture } from "@vitarish/account-web";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { OptionSelector } from "@/components/account/OptionSelector";
import { useLocalizedText } from "@/localization";

export default function CulturePage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const router = useRouter();
  const { t } = useLocalizedText();
  const {
    selected,
    setSelected,
    success,
    isError,
    isPending,
    save,
  } = useAccountCulture(userId);

  const handleSave = useCallback(async () => {
    await save(selected);
    router.refresh();
  }, [save, selected, router]);

  const cultures = [
    { value: "en-US", label: t("account.culture.english") },
    { value: "fa-IR", label: t("account.culture.persian") },
  ];

  return (
    <AccountSurface>
      <OptionSelector
        title={t("account.culture.title")}
        options={cultures}
        selected={selected}
        onSelect={setSelected}
      />
      {isError && (
        <StatusMessage message={t("account.culture.updateFailed")} />
      )}
      {success && (
        <StatusMessage message={t("account.culture.updateSucceeded")} type="success" />
      )}
      <Button
        onPress={handleSave}
        isPending={isPending}
        className="w-full"
      >
        {t("common.actions.save")}
      </Button>
      <AccountBackLink />
    </AccountSurface>
  );
}
