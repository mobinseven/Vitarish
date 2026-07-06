"use client";

import { useCallback } from "react";
import { Button, Link } from "@heroui/react";
import { useTheme } from "next-themes";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { Routes } from "@/app/routes";
import { useAccountTheme } from "@/features/account/hooks/useAccountTheme";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { useLocalizedText } from "@/localization";
import { PaletteSwitch } from "@/features/account/theme/PaletteSwitch";
import {
  setVibrantPaletteAttribute,
  vibrantPaletteStorageKey,
} from "@/features/account/theme/themeOptions";

export default function ThemePage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const { setTheme: setNextTheme } = useTheme();
  const { t } = useLocalizedText();
  const {
    selected,
    setSelected,
    isVibrant,
    setIsVibrant,
    success,
    isError,
    isPending,
    save,
  } = useAccountTheme(userId);

  const handleSave = useCallback(async () => {
    await save({ theme: selected, isVibrant });
    setNextTheme(selected);
    window.localStorage.setItem(vibrantPaletteStorageKey, String(isVibrant));
    setVibrantPaletteAttribute(isVibrant, document.documentElement);
  }, [isVibrant, save, selected, setNextTheme]);

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-4">
      <PaletteSwitch
        selected={selected}
        isVibrant={isVibrant}
        onSelect={setSelected}
        onVibrantChange={setIsVibrant}
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
      <Link href={Routes.Account.Index} className="text-sm">
        {t("common.actions.backToSettings")}
      </Link>
    </div>
  );
}
