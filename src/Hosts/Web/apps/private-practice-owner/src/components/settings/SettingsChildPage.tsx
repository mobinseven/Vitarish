"use client";

import type { ReactNode } from "react";
import { useRouter } from "next/navigation";
import { PageBackNavigation } from "@vitarish/ui";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";

/** Puts the Settings back trail above a settings page whose own header comes from a shared package. */
export function SettingsChildPage({ children }: { children: ReactNode }) {
  const { t } = useLocalizedText();
  const router = useRouter();
  const settingsLabel = t("clinic.dashboard.sections.settings.title");

  return (
    <div className="flex flex-col gap-3">
      <PageBackNavigation
        crumbs={[{ id: Routes.Clinic.Settings.Index, label: settingsLabel, onPress: () => router.push(Routes.Clinic.Settings.Index) }]}
        navigationLabel={t("clinic.navigation.pageLocation")}
        backLabel={t("clinic.navigation.backTo", { name: settingsLabel })}
      />
      {children}
    </div>
  );
}
