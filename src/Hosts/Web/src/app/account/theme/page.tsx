"use client";

import { useState, useCallback } from "react";
import { Button, Link } from "@heroui/react";
import { useTheme } from "next-themes";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { Routes } from "@/app/routes";
import {
  useUsersGetTheme,
  useUsersSetTheme,
} from "@/api/generated/users/users";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { OptionSelector } from "@/components/account/OptionSelector";
import { useLocalizedText } from "@/localization";

export default function ThemePage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const { setTheme: setNextTheme } = useTheme();
  const { data: currentTheme } = useUsersGetTheme(userId);
  const { t } = useLocalizedText();
  const setTheme = useUsersSetTheme();

  const [selected, setSelected] = useState(currentTheme ?? "system");
  const [success, setSuccess] = useState(false);

  const handleSave = useCallback(async () => {
    setSuccess(false);
    await setTheme.mutateAsync({ userId, data: { theme: selected } });
    setNextTheme(selected);
    setSuccess(true);
  }, [selected, setTheme, setNextTheme, userId]);

  const themes = [
    { value: "light", label: t("account.theme.light") },
    { value: "dark", label: t("account.theme.dark") },
    { value: "system", label: t("account.theme.system") },
  ];

  return (
    <div className="max-w-md mx-auto flex flex-col gap-4">
      <OptionSelector
        title={t("account.theme.title")}
        options={themes}
        selected={selected}
        onSelect={setSelected}
      />
      {setTheme.isError && (
        <StatusMessage message={t("account.theme.updateFailed")} />
      )}
      {success && (
        <StatusMessage message={t("account.theme.updateSucceeded")} type="success" />
      )}
      <Button
        onPress={handleSave}
        isPending={setTheme.isPending}
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
