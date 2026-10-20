"use client";

import type { ReactNode } from "react";
import { useLocalizedText } from "@/localization";

function Logotype({ src, alt }: { src: string; alt: string }) {
  return (
    <header className="w-full">
      <img
        src={src}
        alt={alt}
        aria-hidden={alt === "" ? true : undefined}
        className="block h-auto w-full"
      />
    </header>
  );
}

/** The brand frame of the standalone pages (sign-in, clinic setup, handoff): logotypes above and below the content. */
export function BrandedPageFrame({ children }: { children: ReactNode }) {
  const { t } = useLocalizedText();

  return (
    <div className="bg-anchor">
      <Logotype src="/Master-Hel-Logotype.svg" alt={t("appShell.brand")} />
      {children}
      <Logotype src="/Master-Hel-Logotype-©.svg" alt="" />
    </div>
  );
}
