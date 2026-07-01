"use client";

import { useState, useCallback, useEffect, useMemo } from "react";
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
import { persistClientCulture, useLocalizer } from "@/i18n/client";
import {
  normalizeCulture,
  supportedCultureOptions,
  type SupportedCulture,
} from "@/i18n/cultures";
import type { MessageKey } from "@/i18n/messages";

const cultureLabelKeys: Record<SupportedCulture, MessageKey> = {
  "en-US": "Culture.English",
  "fa-IR": "Culture.Persian",
};

export default function CulturePage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const router = useRouter();
  const { data: currentCulture } = useUsersGetCulture(userId);
  const setCulture = useUsersSetCulture();
  const { culture, t } = useLocalizer();

  const [selected, setSelected] = useState<SupportedCulture>(culture);
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    if (currentCulture) {
      setSelected(normalizeCulture(currentCulture));
    }
  }, [currentCulture]);

  const cultures = useMemo(
    () =>
      supportedCultureOptions.map((option) => ({
        value: option.value,
        label: t(cultureLabelKeys[option.value]),
      })),
    [t],
  );

  const handleSave = useCallback(async () => {
    setSuccess(false);
    await setCulture.mutateAsync({ userId, data: { culture: selected } });
    persistClientCulture(selected);
    setSuccess(true);
    router.refresh();
  }, [selected, setCulture, router, userId]);

  return (
    <div className="max-w-md mx-auto flex flex-col gap-4">
      <OptionSelector
        title={t("Culture.Language")}
        options={cultures}
        selected={selected}
        onSelect={setSelected}
      />
      {setCulture.isError && (
        <StatusMessage message={t("Culture.UpdateFailed")} />
      )}
      {success && (
        <StatusMessage message={t("Culture.UpdateSucceeded")} type="success" />
      )}
      <Button
        onPress={handleSave}
        isPending={setCulture.isPending}
        className="w-full"
      >
        {t("Common.Save")}
      </Button>
      <Link href={Routes.Account.Index} className="text-sm">
        {t("Common.BackToSettings")}
      </Link>
    </div>
  );
}
