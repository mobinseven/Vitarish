"use client";

import { Routes } from "@/app/routes";
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
import { BrandLogotipo } from "./brand";
import { useLandingCopy } from "./copy";

const login = { label: "Sign in", href: Routes.Login.Index, tone: "quiet" as const };
const start = { label: "Get started", href: Routes.Login.Index, tone: "primary" as const };

export function LandingPage() {
  const c = useLandingCopy();
  const features = [
    ["fa-light fa-user-doctor", c("featurePatientContext"), c("featurePatientContextDescription")],
    ["fa-light fa-calendar-check", c("featureAppointments"), c("featureAppointmentsDescription")],
    ["fa-light fa-file-prescription", c("featureTemplates"), c("featureTemplatesDescription")],
    ["fa-light fa-clipboard-check", c("featureCheckin"), c("featureCheckinDescription")],
    ["fa-light fa-chart-line", c("featureOversight"), c("featureOversightDescription")],
    ["fa-light fa-sparkles", c("featureAi"), c("featureAiDescription")],
  ];
  return (
    <PublicDocumentShell
      className="bg-background text-foreground"
      header={
        <MarketingHeader
          navigationLabel={c("navLabel")}
          brand={<BrandLogotipo className="h-8 w-auto" />}
          navigation={[{ label: c("capabilitiesEyebrow"), href: "#capabilities" }, { label: c("workflowEyebrow"), href: "#workflow" }, { label: c("aiEyebrow"), href: "#ai" }]}
          actions={[{ ...start, label: c("getStarted") }, { ...login, label: c("signIn") }]}
        />
      }
      footer={
        <MarketingFooter
          navigationLabel={c("footerLabel")}
          brand={<BrandLogotipo className="h-7 w-auto" />}
          description={c("description")}
          links={[{ label: c("capabilitiesEyebrow"), href: "#capabilities" }, { ...login, label: c("signIn") }]}
        />
      }
    >
      <StandaloneDocumentPage>
        <HeroHeader variant="split">
          <HeroHeader.Content>
            <HeroHeader.Eyebrow>{c("eyebrow")}</HeroHeader.Eyebrow>
            <HeroHeader.Title>{c("title")}</HeroHeader.Title>
            <HeroHeader.Description>{c("description")}</HeroHeader.Description>
            <HeroHeader.Actions>
              <MarketingActionLink action={{ ...start, label: c("getStarted") }} />
              <MarketingActionLink action={{ label: c("seeCapabilities"), href: "#capabilities", tone: "outline" }} />
            </HeroHeader.Actions>
          </HeroHeader.Content>
          <HeroHeader.Visual>
            <div className="rounded-3xl border border-default bg-surface p-5 shadow-sm sm:p-8">
              <div className="flex items-center justify-between border-b border-default pb-4"><span className="text-sm font-semibold">{c("today")}</span><span className="rounded-full bg-primary/10 px-3 py-1 text-xs text-primary">{c("live")}</span></div>
              <div className="mt-5 grid gap-3 sm:grid-cols-2">
                <div className="rounded-2xl bg-surface-secondary p-4"><p className="text-xs text-muted">{c("progress")}</p><p className="mt-2 text-2xl font-semibold">{c("activeRoutines")}</p><p className="mt-1 text-xs text-muted">{c("needsReview")}</p></div>
                <div className="rounded-2xl bg-surface-secondary p-4"><p className="text-xs text-muted">{c("appointments")}</p><p className="mt-2 text-2xl font-semibold">{c("todayAppointments")}</p><p className="mt-1 text-xs text-muted">{c("providers")}</p></div>
              </div>
              <div className="mt-3 rounded-2xl border border-default p-4"><p className="text-xs text-muted">{c("next")}</p><p className="mt-2 font-medium">{c("review")}</p></div>
            </div>
          </HeroHeader.Visual>
        </HeroHeader>

        <MarketingSection id="capabilities">
          <MarketingSectionHeading eyebrow={c("capabilitiesEyebrow")} title={c("capabilitiesTitle")} description={c("capabilitiesDescription")} />
          <FeatureItemGroup className="mt-10" columns={3} layout="grid" variant="transparent">
            {features.map((feature) => (
              <FeatureItemCard key={feature[1]} variant="secondary">
                <FeatureItemCard.Icon><i aria-hidden="true" className={`${feature[0]} text-lg text-primary`} /></FeatureItemCard.Icon>
                <FeatureItemCard.Content><FeatureItemCard.Title>{feature[1]}</FeatureItemCard.Title><FeatureItemCard.Description>{feature[2]}</FeatureItemCard.Description></FeatureItemCard.Content>
              </FeatureItemCard>
            ))}
          </FeatureItemGroup>
        </MarketingSection>

        <MarketingSection id="workflow" className="bg-surface/40">
          <MarketingSectionHeading eyebrow={c("workflowEyebrow")} title={c("workflowTitle")} />
          <FeatureItemGroup className="mt-10" variant="transparent">
            {[["fa-light fa-inbox", c("gather"), c("gatherDescription")], ["fa-light fa-eye", c("understand"), c("understandDescription")], ["fa-light fa-arrow-right-arrow-left", c("coordinate"), c("coordinateDescription")]].map(([icon, title, description]) => (
              <FeatureItemCard key={title} variant="outline">
                <FeatureItemCard.Icon><i aria-hidden="true" className={`${icon} text-lg text-primary`} /></FeatureItemCard.Icon>
                <FeatureItemCard.Content><FeatureItemCard.Title>{title}</FeatureItemCard.Title><FeatureItemCard.Description>{description}</FeatureItemCard.Description></FeatureItemCard.Content>
              </FeatureItemCard>
            ))}
          </FeatureItemGroup>
        </MarketingSection>

        <MarketingSection id="ai"><MarketingSectionHeading eyebrow={c("aiEyebrow")} title={c("aiTitle")} description={c("aiDescription")} /></MarketingSection>
        <MarketingFinalCta title={c("ctaTitle")} description={c("ctaDescription")} actions={[{ ...start, label: c("getStarted") }, { ...login, label: c("signIn") }]} />
      </StandaloneDocumentPage>
    </PublicDocumentShell>
  );
}
