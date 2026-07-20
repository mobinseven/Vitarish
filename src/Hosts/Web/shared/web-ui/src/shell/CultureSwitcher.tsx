"use client";

import { useCallback } from "react";
import { Dropdown, Label, Tooltip } from "@heroui/react";
import { setLocaleCookie, supportedLocales, useLocalizedText, type SupportedLocale } from "@vitarish/web-localization";

const localeLabels: Record<SupportedLocale, string> = { fa: "فارسی", en: "English" };

export function CultureSwitcher() {
  const { locale, t } = useLocalizedText();
  const handleLocaleChange = useCallback((key: React.Key) => {
    if (key === locale) return;
    setLocaleCookie(key as SupportedLocale);
    window.location.reload();
  }, [locale]);

  return (
    <Dropdown>
      <Tooltip content={t("appShell.culture.label")}>
        <Dropdown.Trigger aria-label={t("appShell.culture.label")} className="w-full justify-start gap-3 px-3 text-sm text-muted">
            <i aria-hidden="true" className="fa-solid fa-globe size-4 shrink-0" />
            <span data-sidebar="label" className="truncate">{t("appShell.culture.label")}</span>
        </Dropdown.Trigger>
      </Tooltip>
      <Dropdown.Popover>
        <Dropdown.Menu aria-label={t("appShell.culture.label")} selectionMode="single" selectedKeys={[locale]} onAction={handleLocaleChange}>
          {supportedLocales.map((loc) => <Dropdown.Item key={loc} id={loc} textValue={localeLabels[loc]}><Label>{localeLabels[loc]}</Label></Dropdown.Item>)}
        </Dropdown.Menu>
      </Dropdown.Popover>
    </Dropdown>
  );
}
