"use client";

import { useLocalizedText } from "@/localization";

export function Footer() {
  const { t } = useLocalizedText();

  return (
    <footer className="w-full flex items-center justify-center py-3 border-t border-separator">
      <p className="text-sm text-muted">
        {t("appShell.footer.poweredBy")} <span className="text-accent font-medium">HeroUI</span>
      </p>
    </footer>
  );
}
