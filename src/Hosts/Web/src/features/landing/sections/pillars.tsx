import {Card} from "@heroui/react";

import {FaIcon} from "@/features/landing/icons/fa-icon";
import {figures} from "@/features/landing/data/figures";
import {Figure, Reveal, SectionLabel} from "@/components/blueprint";
import {useLocalizedText} from "@/localization";

const pillarKeys = [
  {fig: "FIG. 02", figure: figures.deploy, icon: "rocket", titleKey: "landing.pillars.items.0.title" as const, bodyKey: "landing.pillars.items.0.body" as const, tagKeys: ["landing.pillars.items.0.tag0" as const, "landing.pillars.items.0.tag1" as const]},
  {fig: "FIG. 03", figure: figures.scale, icon: "boxes-3", titleKey: "landing.pillars.items.1.title" as const, bodyKey: "landing.pillars.items.1.body" as const, tagKeys: ["landing.pillars.items.1.tag0" as const, "landing.pillars.items.1.tag1" as const]},
  {fig: "FIG. 04", figure: figures.observe, icon: "pulse", titleKey: "landing.pillars.items.2.title" as const, bodyKey: "landing.pillars.items.2.body" as const, tagKeys: ["landing.pillars.items.2.tag0" as const, "landing.pillars.items.2.tag1" as const]},
  {fig: "FIG. 05", figure: figures.secure, icon: "shield-check", titleKey: "landing.pillars.items.3.title" as const, bodyKey: "landing.pillars.items.3.body" as const, tagKeys: ["landing.pillars.items.3.tag0" as const, "landing.pillars.items.3.tag1" as const]},
];

export function Pillars() {
  const {t} = useLocalizedText();

  return (
    <section className="relative scroll-mt-24 py-20 lg:py-28" id="pillars">
      <div className="mx-auto max-w-[1200px] px-6">
        <div className="max-w-2xl">
          <Reveal>
            <SectionLabel index="FIG. 02—05">{t("landing.pillars.sectionLabel")}</SectionLabel>
          </Reveal>
          <Reveal delay={0.05}>
            <h2 className="mt-6 text-4xl font-semibold tracking-tight sm:text-5xl">
              {t("landing.pillars.heading")}
            </h2>
          </Reveal>
          <Reveal delay={0.1}>
            <p className="text-muted mt-5 max-w-xl leading-relaxed">
              {t("landing.pillars.subheading")}
            </p>
          </Reveal>
        </div>

        <div className="mt-14 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
          {pillarKeys.map((p, i) => (
            <Reveal key={p.titleKey} delay={0.05 * i}>
              <Card className="group h-full transition-colors duration-200 hover:border-accent/50">
                <Figure figure={p.figure} fig={p.fig} imgClassName="aspect-square" />
                <Card.Header className="mt-5">
                  <div className="flex items-center gap-2.5">
                    <span className="border-border text-accent flex size-8 items-center justify-center border">
                      <FaIcon icon={p.icon} className="size-4" />
                    </span>
                    <Card.Title className="text-lg">{t(p.titleKey)}</Card.Title>
                  </div>
                  <Card.Description className="text-muted mt-3 text-sm leading-relaxed">
                    {t(p.bodyKey)}
                  </Card.Description>
                </Card.Header>
                <Card.Footer className="mt-4 flex-wrap gap-x-4 gap-y-2 border-t border-border pt-4">
                  {p.tagKeys.map((tagKey) => (
                    <span key={tagKey} className="bp-label flex items-center gap-1.5">
                      <span className="bg-accent/70 size-1 rounded-full" />
                      {t(tagKey)}
                    </span>
                  ))}
                </Card.Footer>
              </Card>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  );
}
