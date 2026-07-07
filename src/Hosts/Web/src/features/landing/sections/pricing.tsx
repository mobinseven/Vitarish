import {Button, Card} from "@heroui/react";

import {FaIcon} from "@/features/landing/icons/fa-icon";
import {Annotation, Reveal, RulerLine, SectionLabel} from "@/components/blueprint";
import {useLocalizedText} from "@/localization";

const planKeys = [
  {tag: "PLAN.01", price: "$0", unit: "/mo", nameKey: "landing.pricing.plans.0.name" as const, descKey: "landing.pricing.plans.0.desc" as const, featureKeys: ["landing.pricing.plans.0.feature0" as const, "landing.pricing.plans.0.feature1" as const, "landing.pricing.plans.0.feature2" as const, "landing.pricing.plans.0.feature3" as const], ctaKey: "landing.pricing.plans.0.cta" as const, featured: false},
  {tag: "PLAN.02", price: "$2,400", unit: "/mo", nameKey: "landing.pricing.plans.1.name" as const, descKey: "landing.pricing.plans.1.desc" as const, featureKeys: ["landing.pricing.plans.1.feature0" as const, "landing.pricing.plans.1.feature1" as const, "landing.pricing.plans.1.feature2" as const, "landing.pricing.plans.1.feature3" as const, "landing.pricing.plans.1.feature4" as const], ctaKey: "landing.pricing.plans.1.cta" as const, featured: true},
  {tag: "PLAN.03", price: "Custom", unit: "", nameKey: "landing.pricing.plans.2.name" as const, descKey: "landing.pricing.plans.2.desc" as const, featureKeys: ["landing.pricing.plans.2.feature0" as const, "landing.pricing.plans.2.feature1" as const, "landing.pricing.plans.2.feature2" as const, "landing.pricing.plans.2.feature3" as const, "landing.pricing.plans.2.feature4" as const], ctaKey: "landing.pricing.plans.2.cta" as const, featured: false},
];

export function Pricing() {
  const {t} = useLocalizedText();

  return (
    <section className="relative scroll-mt-24 py-20 lg:py-28" id="pricing">
      <div className="mx-auto max-w-[1200px] px-6">
        <div className="flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
          <div className="max-w-2xl">
            <Reveal>
              <SectionLabel index="FIG. 12">{t("landing.pricing.sectionLabel")}</SectionLabel>
            </Reveal>
            <Reveal delay={0.05}>
              <h2 className="mt-6 text-4xl font-semibold tracking-tight sm:text-5xl">
                {t("landing.pricing.heading")}
              </h2>
            </Reveal>
          </div>
          <Reveal delay={0.1}>
            <p className="text-muted max-w-sm leading-relaxed lg:text-right">
              {t("landing.pricing.subheading")}
            </p>
          </Reveal>
        </div>

        <Reveal delay={0.08}>
          <div className="relative mt-10">
            <RulerLine accent />
            <div className="absolute -top-2 left-0">
              <Annotation className="bp-label-accent">0</Annotation>
            </div>
            <div className="absolute -top-2 right-0">
              <Annotation className="bp-label-accent">{t("landing.pricing.scaleLabel")}</Annotation>
            </div>
          </div>
        </Reveal>

        <div className="mt-8 grid gap-5 lg:grid-cols-3">
          {planKeys.map((plan, i) => (
            <Reveal key={plan.nameKey} delay={0.05 * i}>
              <Card
                className={`relative flex h-full flex-col ${
                  plan.featured ? "border-accent ring-1 ring-accent/20" : ""
                }`}
              >
                {plan.featured ? (
                  <span className="bg-accent text-accent-foreground absolute right-4 top-4 px-2 py-1 font-mono text-[0.6rem] font-semibold uppercase tracking-widest">
                    {t("landing.pricing.recommended")}
                  </span>
                ) : null}
                <Card.Header>
                  <Annotation>{plan.tag}</Annotation>
                  <Card.Title className="mt-3 text-lg">{t(plan.nameKey)}</Card.Title>
                  <div className="mt-4 flex items-baseline gap-1">
                    <span className="text-4xl font-semibold tracking-tight">{plan.price}</span>
                    {plan.unit ? (
                      <span className="text-muted font-mono text-sm">{plan.unit}</span>
                    ) : null}
                  </div>
                  <Card.Description className="text-muted mt-3 text-sm">
                    {t(plan.descKey)}
                  </Card.Description>
                </Card.Header>
                <Card.Content className="mt-6 flex-1">
                  <ul className="flex flex-col gap-3">
                    {plan.featureKeys.map((fk) => (
                      <li key={fk} className="flex items-center gap-2.5">
                        <FaIcon icon="circle-check" className="text-accent size-4 shrink-0" />
                        <span className="text-foreground/90 text-sm">{t(fk)}</span>
                      </li>
                    ))}
                  </ul>
                </Card.Content>
                <Card.Footer className="mt-8">
                  <Button
                    fullWidth
                    // eslint-disable-next-line @typescript-eslint/no-explicit-any
                    render={(props: any) => <a href="#cta" {...props} />}
                    variant={plan.featured ? "primary" : "outline"}
                  >
                    {t(plan.ctaKey)}
                  </Button>
                </Card.Footer>
              </Card>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  );
}
