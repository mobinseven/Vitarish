"use client";

import { LandingFooter } from "@/features/landing/LandingFooter";
import { LandingHero } from "@/features/landing/LandingHero";
import { LandingNavbar } from "@/features/landing/LandingNavbar";
import { PracticePrinciplesShowcase } from "@/features/landing/PracticePrinciplesShowcase";
import { useLocalizedText } from "@/localization";

export default function RootPage() {
  const { t } = useLocalizedText();

  return (
    <div className="min-h-dvh bg-background">
      <LandingNavbar />
      <main aria-label={t("practiceLanding.mainContent")}>
        <LandingHero />
        <PracticePrinciplesShowcase />
        <LandingFooter />
      </main>
    </div>
  );
}
