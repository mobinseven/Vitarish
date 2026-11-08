"use client";

import { usePathname, useRouter } from "next/navigation";
import { Tabs } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useLocalizedText, type MessageKey } from "@/localization";

const serviceTabs = [
  { id: Routes.Clinic.Settings.Services.Index, labelKey: "clinic.services.title" },
  { id: Routes.Clinic.Settings.Services.Types.Index, labelKey: "clinic.serviceTypes.title" },
  { id: Routes.Clinic.Settings.Services.Providers.Index, labelKey: "clinic.serviceProviders.title" },
] as const satisfies readonly { id: string; labelKey: MessageKey }[];

/** Switches between the services, service types and providers lists, each of which keeps its own address. */
export function ServicesTabs() {
  const { t } = useLocalizedText();
  const router = useRouter();
  const pathname = usePathname();

  return (
    <Tabs
      variant="secondary"
      selectedKey={serviceTabs.find((tab) => tab.id === pathname)?.id}
      onSelectionChange={(key) => router.push(String(key))}
    >
      <Tabs.ListContainer>
        <Tabs.List aria-label={t("clinic.dashboard.sections.settings.title")}>
          {serviceTabs.map((tab) => (
            <Tabs.Tab key={tab.id} id={tab.id}>
              {t(tab.labelKey)}
              <Tabs.Indicator />
            </Tabs.Tab>
          ))}
        </Tabs.List>
      </Tabs.ListContainer>
    </Tabs>
  );
}
