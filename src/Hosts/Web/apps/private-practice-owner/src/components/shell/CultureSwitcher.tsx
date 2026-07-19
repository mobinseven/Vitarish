"use client";

import { useCallback } from "react";
import { Dropdown, Label } from "@heroui/react";
import { useLocalizedText, setLocaleCookie, type SupportedLocale, supportedLocales } from "@/localization";

const localeLabels: Record<SupportedLocale, string> = {
  fa: "فارسی",
  en: "English",
};

export function CultureSwitcher() {
  const { t, locale } = useLocalizedText();

  const handleLocaleChange = useCallback((key: React.Key) => {
    if (key === locale) return;
    setLocaleCookie(key as SupportedLocale);
    window.location.reload();
  }, [locale]);

  return (
    <Dropdown>
      <Dropdown.Trigger aria-label={t("appShell.culture.label")}>
        <span className="inline-flex size-9 items-center justify-center rounded-medium text-default-500 transition-colors hover:bg-default-100 hover:text-foreground">
          <i aria-hidden="true" className="fa-solid fa-globe size-4" />
        </span>
      </Dropdown.Trigger>
      <Dropdown.Popover>
        <Dropdown.Menu
          aria-label={t("appShell.culture.label")}
          selectionMode="single"
          selectedKeys={[locale]}
          onAction={handleLocaleChange}
        >
          {supportedLocales.map((loc) => (
            <Dropdown.Item key={loc} id={loc} textValue={localeLabels[loc]}>
              <Label>{localeLabels[loc]}</Label>
            </Dropdown.Item>
          ))}
        </Dropdown.Menu>
      </Dropdown.Popover>
    </Dropdown>
  );
}
