"use client";

import { useCallback } from "react";
import { Button } from "@heroui/react";
import { useTheme } from "next-themes";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { AccountBackLink, AccountSurface } from "@/components/account/AccountSurface";
import { useAccountTheme } from "@/features/account/hooks/useAccountTheme";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { OptionSelector } from "@/components/account/OptionSelector";
import { useLocalizedText } from "@/localization";

export default function ThemePage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const { setTheme: setNextTheme } = useTheme();
  const { t } = useLocalizedText();
  const {
    selected,
    setSelected,
    success,
    isError,
    isPending,
    save,
  } = useAccountTheme(userId);

  const handleSave = useCallback(async () => {
    await save(selected);
    setNextTheme(selected);
  }, [save, selected, setNextTheme]);

  const themes = [
    { value: "light", label: t("account.theme.light") },
    { value: "dark", label: t("account.theme.dark") },
    { value: "system", label: t("account.theme.system") },
  ];

  return (
    <AccountSurface>
      <OptionSelector
        title={t("account.theme.title")}
        options={themes}
        selected={selected}
        onSelect={setSelected}
      />
      {isError && (
        <StatusMessage message={t("account.theme.updateFailed")} />
      )}
      {success && (
        <StatusMessage message={t("account.theme.updateSucceeded")} type="success" />
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
