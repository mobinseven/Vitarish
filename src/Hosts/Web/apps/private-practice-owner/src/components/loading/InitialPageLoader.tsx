"use client";

import { useEffect } from "react";
import { useLocalizedText } from "@/localization";
import { LoadingSpinner } from "./LoadingSpinner";

export function InitialPageLoader() {
  const { t } = useLocalizedText();
  useEffect(() => {
    document.getElementById("initial-page-loader")?.remove();
  }, []);

  return (
    <div
      id="initial-page-loader"
      className="fixed inset-0 z-[9999] flex items-center justify-center bg-background"
      aria-label={t("common.states.loading")}
      role="status"
    >
      <LoadingSpinner />
    </div>
  );
}
