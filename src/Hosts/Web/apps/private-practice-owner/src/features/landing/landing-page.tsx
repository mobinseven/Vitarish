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
import { landingCopy as c } from "./copy";

const login = { label: "Sign in", href: Routes.Login.Index, tone: "quiet" as const };
const start = { label: "Get started", href: Routes.Login.Index, tone: "primary" as const };

const features = [
  { icon: "fa-light fa-user-doctor", title: "Patient context", description: "See routines, notes, progress, and relevant history together so handoffs start with understanding." },
  { icon: "fa-light fa-calendar-check", title: "Appointments and shifts", description: "Coordinate agendas, providers, services, and shift coverage in one current view." },
  { icon: "fa-light fa-file-prescription", title: "Templates and insurance", description: "Prepare prescriptions and procedures from reusable templates, then keep submission work traceable." },
  { icon: "fa-light fa-clipboard-check", title: "Check-in and follow-up", description: "Keep arrivals, next steps, and outstanding work visible to the people responsible." },
  { icon: "fa-light fa-chart-line", title: "Practice oversight", description: "Spot what needs attention across patients and teams without losing the detail behind it." },
  { icon: "fa-light fa-sparkles", title: "AI guidance", description: "Use focused summaries and prompts to reduce cognitive load while people remain in control." },
];

export function LandingPage() {
  return (
    <PublicDocumentShell
      className="bg-background text-foreground"
      header={
        <MarketingHeader
          navigationLabel={c.navLabel}
          brand={<BrandLogotipo className="h-8 w-auto" />}
          navigation={[{ label: "Capabilities", href: "#capabilities" }, { label: "Workflow", href: "#workflow" }, { label: "Responsible AI", href: "#ai" }]}
          actions={[start, login]}
        />
      }
      footer={
        <MarketingFooter
          navigationLabel={c.footerLabel}
          brand={<BrandLogotipo className="h-7 w-auto" />}
          description={c.description}
          links={[{ label: "Capabilities", href: "#capabilities" }, login]}
        />
      }
    >
      <StandaloneDocumentPage>
        <HeroHeader variant="split">
          <HeroHeader.Content>
            <HeroHeader.Eyebrow>{c.eyebrow}</HeroHeader.Eyebrow>
            <HeroHeader.Title>{c.title}</HeroHeader.Title>
            <HeroHeader.Description>{c.description}</HeroHeader.Description>
            <HeroHeader.Actions>
              <MarketingActionLink action={start} />
              <MarketingActionLink action={{ label: "See capabilities", href: "#capabilities", tone: "outline" }} />
            </HeroHeader.Actions>
          </HeroHeader.Content>
          <HeroHeader.Visual>
            <div className="rounded-3xl border border-default bg-surface p-5 shadow-sm sm:p-8">
              <div className="flex items-center justify-between border-b border-default pb-4"><span className="text-sm font-semibold">{c.today}</span><span className="rounded-full bg-primary/10 px-3 py-1 text-xs text-primary">{c.live}</span></div>
              <div className="mt-5 grid gap-3 sm:grid-cols-2">
                <div className="rounded-2xl bg-surface-secondary p-4"><p className="text-xs text-muted">{c.progress}</p><p className="mt-2 text-2xl font-semibold">{c.activeRoutines}</p><p className="mt-1 text-xs text-muted">{c.needsReview}</p></div>
                <div className="rounded-2xl bg-surface-secondary p-4"><p className="text-xs text-muted">{c.appointments}</p><p className="mt-2 text-2xl font-semibold">{c.todayAppointments}</p><p className="mt-1 text-xs text-muted">{c.providers}</p></div>
              </div>
              <div className="mt-3 rounded-2xl border border-default p-4"><p className="text-xs text-muted">{c.next}</p><p className="mt-2 font-medium">{c.review}</p></div>
            </div>
          </HeroHeader.Visual>
        </HeroHeader>

        <MarketingSection id="capabilities">
          <MarketingSectionHeading eyebrow={c.capabilitiesEyebrow} title={c.capabilitiesTitle} description={c.capabilitiesDescription} />
          <FeatureItemGroup className="mt-10" columns={3} layout="grid" variant="transparent">
            {features.map((feature) => (
              <FeatureItemCard key={feature.title} variant="secondary">
                <FeatureItemCard.Icon><i aria-hidden="true" className={`${feature.icon} text-lg text-primary`} /></FeatureItemCard.Icon>
                <FeatureItemCard.Content><FeatureItemCard.Title>{feature.title}</FeatureItemCard.Title><FeatureItemCard.Description>{feature.description}</FeatureItemCard.Description></FeatureItemCard.Content>
              </FeatureItemCard>
            ))}
          </FeatureItemGroup>
        </MarketingSection>

        <MarketingSection id="workflow" className="bg-surface/40">
          <MarketingSectionHeading eyebrow={c.workflowEyebrow} title={c.workflowTitle} />
          <FeatureItemGroup className="mt-10" variant="transparent">
            {[
              ["fa-light fa-inbox", "Gather", "Patient updates, appointments, provider activity, and office tasks arrive in a shared operating view."],
              ["fa-light fa-eye", "Understand", "Vitarish keeps the clinical story legible, surfacing progress and gaps without making autonomous decisions."],
              ["fa-light fa-arrow-right-arrow-left", "Coordinate", "Assign the next meaningful action to the right provider or team member and keep the agenda current."],
            ].map(([icon, title, description]) => (
              <FeatureItemCard key={title} variant="outline">
                <FeatureItemCard.Icon><i aria-hidden="true" className={`${icon} text-lg text-primary`} /></FeatureItemCard.Icon>
                <FeatureItemCard.Content><FeatureItemCard.Title>{title}</FeatureItemCard.Title><FeatureItemCard.Description>{description}</FeatureItemCard.Description></FeatureItemCard.Content>
              </FeatureItemCard>
            ))}
          </FeatureItemGroup>
        </MarketingSection>

        <MarketingSection id="ai"><MarketingSectionHeading eyebrow={c.aiEyebrow} title={c.aiTitle} description={c.aiDescription} /></MarketingSection>
        <MarketingFinalCta title={c.ctaTitle} description={c.ctaDescription} actions={[start, login]} />
      </StandaloneDocumentPage>
    </PublicDocumentShell>
  );
}
