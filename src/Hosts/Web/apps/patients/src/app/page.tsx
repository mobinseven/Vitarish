"use client";

import { Routes } from "@/app/routes";
import { formatDateTime, useLocalizedText } from "@/localization";
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

export default function PatientsHomePage() {
  const { locale, t } = useLocalizedText();
  const copy = (key: string) => t(`patientPortal.${key}`);
  const signIn = { label: copy("signIn"), href: Routes.Login.Index, tone: "primary" as const };
  const portalFeatures = [
    { icon: "fa-light fa-calendar-days", title: copy("appointments"), description: copy("appointmentsDescription") },
    { icon: "fa-light fa-house-medical", title: copy("practices"), description: copy("practicesDescription") },
    { icon: "fa-light fa-file-lines", title: copy("records"), description: copy("recordsDescription") },
    { icon: "fa-light fa-user", title: copy("profile"), description: copy("profileDescription") },
  ];
  const sampleAppointment = formatDateTime("2026-08-11T10:30:00", locale);
  return (
    <PublicDocumentShell
      className="bg-background text-foreground"
      header={
        <MarketingHeader
          brand={<BrandLogotipo className="h-8 w-auto" />}
          navigation={[{ label: copy("navPortal"), href: "#portal" }, { label: copy("navPrivacy"), href: "#privacy" }]}
          actions={[signIn]}
        />
      }
      footer={
        <MarketingFooter
          brand={<BrandLogotipo className="h-7 w-auto" />}
          description={copy("footerDescription")}
          links={[{ label: copy("signIn"), href: Routes.Login.Index }]}
        />
      }
    >
      <StandaloneDocumentPage>
        <HeroHeader variant="split">
          <HeroHeader.Content>
            <HeroHeader.Eyebrow>{copy("eyebrow")}</HeroHeader.Eyebrow>
            <HeroHeader.Title>{copy("title")}</HeroHeader.Title>
            <HeroHeader.Description>{copy("description")}</HeroHeader.Description>
            <HeroHeader.Actions>
              <MarketingActionLink action={signIn} />
            </HeroHeader.Actions>
          </HeroHeader.Content>
          <HeroHeader.Visual>
            <div className="rounded-3xl border border-default bg-surface p-6 shadow-sm sm:p-8">
              <p className="text-sm font-semibold">{copy("previewTitle")}</p>
              <div className="mt-5 space-y-3">
                <div className="rounded-2xl bg-surface-secondary p-4"><p className="text-xs text-muted">{copy("nextAppointment")}</p><p className="mt-1 font-medium">{sampleAppointment}</p></div>
                <div className="rounded-2xl bg-surface-secondary p-4"><p className="text-xs text-muted">{copy("recentRecord")}</p><p className="mt-1 font-medium">{copy("latestUpdate")}</p></div>
                <div className="rounded-2xl border border-default p-4"><p className="text-xs text-muted">{copy("profile")}</p><p className="mt-1 font-medium">{copy("currentInformation")}</p></div>
              </div>
            </div>
          </HeroHeader.Visual>
        </HeroHeader>

        <MarketingSection id="portal">
          <MarketingSectionHeading eyebrow={copy("featuresEyebrow")} title={copy("featuresTitle")} />
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
          <MarketingSectionHeading eyebrow={copy("privacyEyebrow")} title={copy("privacyTitle")} description={copy("privacyDescription")} />
        </MarketingSection>
        <MarketingFinalCta title={copy("ctaTitle")} description={copy("ctaDescription")} actions={[signIn]} />
      </StandaloneDocumentPage>
    </PublicDocumentShell>
  );
}
