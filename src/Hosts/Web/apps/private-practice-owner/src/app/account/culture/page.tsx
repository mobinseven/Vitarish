"use client";

import { useCallback } from "react";
import { Button } from "@heroui/react";
import { useRouter } from "next/navigation";
import { useAuthSession } from "@vitarish/auth";
import {
  AccountBackLink,
  AccountSurface,
} from "@/components/account/AccountSurface";
import { useAccountCulture } from "@vitarish/account";
import { StatusAlert } from "@vitarish/ui";
import { OptionSelector } from "@/components/account/OptionSelector";
import {
  toSupportedLocale,
  useLocalizedText,
  useSetLocaleCookie,
} from "@/localization";

export default function CulturePage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const router = useRouter();
  const { t } = useLocalizedText();
  const setLocaleCookie = useSetLocaleCookie();
  const { selected, setSelected, success, isError, isPending, save } =
    useAccountCulture(userId);

  const handleSave = useCallback(async () => {
    await save(selected);
    const locale = toSupportedLocale(selected);
    if (locale) setLocaleCookie(locale);
    router.refresh();
  }, [save, selected, router, setLocaleCookie]);

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
        <StatusAlert
          className="mb-4"
          message={t("account.culture.updateFailed")}
        />
      )}
      {success && (
        <StatusAlert
          className="mb-4"
          message={t("account.culture.updateSucceeded")}
          type="success"
        />
      )}
      <Button onPress={handleSave} isPending={isPending} className="w-full">
        {t("common.actions.save")}
      </Button>
      <AccountBackLink />
    </AccountSurface>
  );
}
