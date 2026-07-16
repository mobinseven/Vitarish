import {Button, Card} from "@heroui/react";

import {FaIcon} from "@vitarish/web-ui";
import {formatFigureLabel} from "@/features/landing/data/figures";
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
            <div className="flex items-center gap-3">
              <span className="bg-accent inline-block size-1.5" />
              <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-accent">
                <span className="text-muted mr-2">{formatFigureLabel("12")}</span>
                {t("landing.pricing.sectionLabel")}
              </span>
            </div>
            <h2 className="mt-6 text-4xl font-semibold tracking-tight sm:text-5xl">
              {t("landing.pricing.heading")}
            </h2>
          </div>
          <p className="text-muted max-w-sm leading-relaxed lg:text-right">
            {t("landing.pricing.subheading")}
          </p>
        </div>

        <div className="mt-10 grid gap-5 lg:grid-cols-3">
          {planKeys.map((plan) => (
            <Card
              key={plan.nameKey}
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
                <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-muted/88">{plan.tag}</span>
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
          ))}
        </div>
      </div>
    </section>
  );
}
