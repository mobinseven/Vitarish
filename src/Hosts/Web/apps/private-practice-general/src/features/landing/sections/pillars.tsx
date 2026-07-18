import Image from "next/image";
import {Card} from "@heroui/react";

import {FaIcon} from "@vitarish/web-ui";
import {PageContainer} from "@vitarish/web-ui/layout";
import {figures, formatFigureLabel, formatFigureRangeLabel} from "@/features/landing/data/figures";
import {useLocalizedText} from "@/localization";

const pillarKeys = [
  {fig: formatFigureLabel("02"), figure: figures.deploy, icon: "rocket", titleKey: "landing.pillars.items.0.title" as const, bodyKey: "landing.pillars.items.0.body" as const, tagKeys: ["landing.pillars.items.0.tag0" as const, "landing.pillars.items.0.tag1" as const]},
  {fig: formatFigureLabel("03"), figure: figures.scale, icon: "boxes-3", titleKey: "landing.pillars.items.1.title" as const, bodyKey: "landing.pillars.items.1.body" as const, tagKeys: ["landing.pillars.items.1.tag0" as const, "landing.pillars.items.1.tag1" as const]},
  {fig: formatFigureLabel("04"), figure: figures.observe, icon: "pulse", titleKey: "landing.pillars.items.2.title" as const, bodyKey: "landing.pillars.items.2.body" as const, tagKeys: ["landing.pillars.items.2.tag0" as const, "landing.pillars.items.2.tag1" as const]},
  {fig: formatFigureLabel("05"), figure: figures.secure, icon: "shield-check", titleKey: "landing.pillars.items.3.title" as const, bodyKey: "landing.pillars.items.3.body" as const, tagKeys: ["landing.pillars.items.3.tag0" as const, "landing.pillars.items.3.tag1" as const]},
];

export function Pillars() {
  const {t} = useLocalizedText();

  return (
    <section className="relative scroll-mt-24 py-20 lg:py-28" id="pillars">
      <PageContainer>
        <div className="min-w-0 max-w-2xl">
          <div className="flex min-w-0 flex-wrap items-center gap-3">
            <span className="bg-accent inline-block size-1.5" />
            <span className="min-w-0 break-words font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-accent">
              <span className="text-muted mr-2">{formatFigureRangeLabel("02", "05")}</span>
              {t("landing.pillars.sectionLabel")}
            </span>
          </div>
          <h2 className="mt-6 min-w-0 break-words text-4xl font-semibold tracking-tight sm:text-5xl">
            {t("landing.pillars.heading")}
          </h2>
          <p className="text-muted mt-5 min-w-0 max-w-xl break-words leading-relaxed">
            {t("landing.pillars.subheading")}
          </p>
        </div>

        <div className="mt-14 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
          {pillarKeys.map((p) => (
            <Card key={p.titleKey} className="group h-full transition-colors duration-200 hover:border-accent/50">
              <div className="relative overflow-hidden border-b border-border">
                <div className="absolute left-3 top-3 z-10">
                  <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-accent">{p.fig}</span>
                </div>
                <Image
                  alt={p.figure.alt}
                  className="relative z-[1] w-full aspect-square object-cover"
                  src={p.figure.src}
                  width={400}
                  height={400}
                  unoptimized
                />
              </div>
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
                  <span key={tagKey} className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-muted/88 flex items-center gap-1.5">
                    <span className="bg-accent/70 size-1 rounded-full" />
                    {t(tagKey)}
                  </span>
                ))}
              </Card.Footer>
            </Card>
          ))}
        </div>
      </PageContainer>
    </section>
  );
}
