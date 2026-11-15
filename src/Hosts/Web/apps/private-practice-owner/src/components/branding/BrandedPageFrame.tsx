"use client";

import type { ReactNode } from "react";
import { useLocalizedText } from "@/localization";
import { darmanLogotypes } from "./darmanLogotypes";

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

/** The owner app's shared page frame, optionally carrying its application logotype. */
export function BrandedPageFrame({
  children,
  showApplicationLogotype = true,
}: {
  children: ReactNode;
  showApplicationLogotype?: boolean;
}) {
  const { t } = useLocalizedText();

  return (
    <div className="bg-anchor">
      {showApplicationLogotype && <Logotype src={darmanLogotypes.onDark} alt={t("practiceLanding.subBrand")} />}
      {children}
      <Logotype src="/Master-Hel-Logotype-©.svg" alt="" />
    </div>
  );
}
