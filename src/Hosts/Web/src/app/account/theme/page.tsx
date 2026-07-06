"use client";

import { useCallback } from "react";
import { Button } from "@heroui/react";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { AccountBackLink, AccountSurface } from "@/components/account/AccountSurface";
import { usePalette } from "@/components/providers/PaletteProvider";
import { useAccountTheme } from "@/features/account/hooks/useAccountTheme";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { useLocalizedText } from "@/localization";
import { PaletteSwitch } from "@/features/account/theme/PaletteSwitch";
import {
  paletteStorageKey,
  setVibrantPaletteAttribute,
  vibrantPaletteStorageKey,
} from "@/features/account/theme/themeOptions";

export default function ThemePage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const { setPalette } = usePalette();
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
    window.localStorage.setItem(paletteStorageKey, selected);
    setPalette(selected);
    window.localStorage.setItem(vibrantPaletteStorageKey, String(isVibrant));
    setVibrantPaletteAttribute(isVibrant, document.documentElement);
  }, [isVibrant, save, selected, setPalette]);

  return (
    <AccountSurface>
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
      <AccountBackLink />
    </AccountSurface>
  );
}
