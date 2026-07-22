"use client";

import { Header } from "./sections/header";
import { Hero } from "./sections/hero";
import { TrustStrip } from "./sections/trust-strip";
import { Pillars } from "./sections/pillars";
import { Architecture } from "./sections/architecture";
import { Metrics } from "./sections/metrics";
import { CodePreview } from "./sections/code-preview";
import { FeatureRows } from "./sections/feature-rows";
import { Pricing } from "./sections/pricing";
import { FinalCta } from "./sections/final-cta";
import { Footer } from "./sections/footer";
import {
  PublicDocumentShell,
  StandaloneDocumentPage,
} from "@vitarish/web-ui/layout";

export function LandingPage() {
  return (
    <PublicDocumentShell
      className="bg-background text-foreground"
      header={<Header />}
      footer={<Footer />}
    >
      <StandaloneDocumentPage>
        <Hero />
        <TrustStrip />
        <Pillars />
        <Architecture />
        <Metrics />
        <CodePreview />
        <FeatureRows />
        <Pricing />
        <FinalCta />
      </StandaloneDocumentPage>
    </PublicDocumentShell>
  );
}
