"use client";

import { LandingFooter } from "@/features/landing/LandingFooter";
import { LandingHero } from "@/features/landing/LandingHero";
import { LandingNavbar } from "@/features/landing/LandingNavbar";
import { PracticePrinciplesShowcase } from "@/features/landing/PracticePrinciplesShowcase";
import { useLocalizedText } from "@/localization";
import { StandaloneDocumentPage } from "@vitarish/ui";

export default function RootPage() {
  const { t } = useLocalizedText();

  return (
    <>
      <LandingNavbar />
      <StandaloneDocumentPage
        aria-label={t("practiceLanding.mainContent")}
        className="bg-background"
      >
        <LandingHero />
        <PracticePrinciplesShowcase />
        <LandingFooter />
      </StandaloneDocumentPage>
    </>
  );
}
