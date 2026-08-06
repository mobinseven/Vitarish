"use client";

import { Routes } from "@/app/routes";
import { BrandLogotipo } from "@/features/landing/brand";
import { PublicDocumentShell, StandaloneDocumentPage } from "@vitarish/web-ui/layout";
import {
  FeatureItemCard,
  FeatureItemGroup,
  HeroHeader,
  MarketingActionLink,
  MarketingFinalCta,
  MarketingFooter,
  MarketingHeader,
  MarketingSection,
  MarketingSectionHeading,
} from "@vitarish/web-ui/marketing";

const signIn = { label: "Sign in", href: Routes.Login.Index, tone: "primary" as const };

const portalFeatures = [
  { icon: "fa-light fa-calendar-days", title: "Appointments", description: "Review upcoming visits and the practices caring for you." },
  { icon: "fa-light fa-house-medical", title: "Practices", description: "Keep the people and locations connected to your care easy to find." },
  { icon: "fa-light fa-file-lines", title: "Records", description: "Access the information your practice makes available to you." },
  { icon: "fa-light fa-user", title: "Profile", description: "Check and update your personal details when they change." },
];

export default function PatientsHomePage() {
  return (
    <PublicDocumentShell
      className="bg-background text-foreground"
      header={
        <MarketingHeader
          brand={<BrandLogotipo className="h-8 w-auto" />}
          navigation={[{ label: "Your portal", href: "#portal" }, { label: "Privacy", href: "#privacy" }]}
          actions={[signIn]}
        />
      }
      footer={
        <MarketingFooter
          brand={<BrandLogotipo className="h-7 w-auto" />}
          description="Your place to stay connected with your practice."
          links={[{ label: "Sign in", href: Routes.Login.Index }]}
        />
      }
    >
      <StandaloneDocumentPage>
        <HeroHeader variant="split">
          <HeroHeader.Content>
            <HeroHeader.Eyebrow>Your Vitarish patient portal</HeroHeader.Eyebrow>
            <HeroHeader.Title>The details of your care, in one place.</HeroHeader.Title>
            <HeroHeader.Description>Sign in to keep up with appointments, practices, records, and your profile information.</HeroHeader.Description>
            <HeroHeader.Actions>
              <MarketingActionLink action={signIn} />
            </HeroHeader.Actions>
          </HeroHeader.Content>
          <HeroHeader.Visual>
            <div className="rounded-3xl border border-default bg-surface p-6 shadow-sm sm:p-8">
              <p className="text-sm font-semibold">Your portal</p>
              <div className="mt-5 space-y-3">
                <div className="rounded-2xl bg-surface-secondary p-4"><p className="text-xs text-muted">Next appointment</p><p className="mt-1 font-medium">Tuesday · 10:30 AM</p></div>
                <div className="rounded-2xl bg-surface-secondary p-4"><p className="text-xs text-muted">Recent record</p><p className="mt-1 font-medium">Your latest practice update</p></div>
                <div className="rounded-2xl border border-default p-4"><p className="text-xs text-muted">Profile</p><p className="mt-1 font-medium">Your information, kept current</p></div>
              </div>
            </div>
          </HeroHeader.Visual>
        </HeroHeader>

        <MarketingSection id="portal">
          <MarketingSectionHeading eyebrow="At a glance" title="Everything you need between visits." />
          <FeatureItemGroup className="mt-10" columns={2} layout="grid" variant="transparent">
            {portalFeatures.map((feature) => (
              <FeatureItemCard key={feature.title} variant="secondary">
                <FeatureItemCard.Icon><i aria-hidden="true" className={`${feature.icon} text-lg text-primary`} /></FeatureItemCard.Icon>
                <FeatureItemCard.Content><FeatureItemCard.Title>{feature.title}</FeatureItemCard.Title><FeatureItemCard.Description>{feature.description}</FeatureItemCard.Description></FeatureItemCard.Content>
              </FeatureItemCard>
            ))}
          </FeatureItemGroup>
        </MarketingSection>

        <MarketingSection id="privacy" className="bg-surface/40">
          <MarketingSectionHeading eyebrow="Trust and privacy" title="Your information belongs in a clear, careful portal." description="Vitarish shows the information made available by your practice. Sign-in protects access, and your practice remains responsible for the care records it manages." />
        </MarketingSection>
        <MarketingFinalCta title="Ready to see your portal?" description="Sign in to continue to your appointments, practices, records, and profile." actions={[signIn]} />
      </StandaloneDocumentPage>
    </PublicDocumentShell>
  );
}
