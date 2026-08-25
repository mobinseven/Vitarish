"use client";

import { useLocalizedText } from "@/localization";

export function LandingFooter() {
  const { t } = useLocalizedText();

  return (
    <footer className="flex flex-col gap-8 px-6 py-12 text-xs text-foreground/60 sm:flex-row sm:items-center sm:justify-between sm:px-10 lg:px-16">
      <span className="font-medium uppercase tracking-[0.3em] text-foreground">
        {t("appShell.brand")}
      </span>
      <nav aria-label={t("practiceLanding.footerLabel")} className="flex gap-6">
        <a className="transition-colors hover:text-foreground" href="#">
          {t("practiceLanding.footerAbout")}
        </a>
        <a className="transition-colors hover:text-foreground" href="#">
          {t("practiceLanding.footerContact")}
        </a>
        <a className="transition-colors hover:text-foreground" href="#">
          {t("practiceLanding.footerPrivacy")}
        </a>
      </nav>
      <span>© 2026 Vitarish</span>
    </footer>
  );
}
