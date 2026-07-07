import Image from "next/image";

import {figures} from "@/features/landing/data/figures";
import {Annotation, CornerMarks, DrawLineX, Reveal, SectionLabel} from "@/components/blueprint";
import {useLocalizedText} from "@/localization";

const nodeKeys = [
  {id: "01", titleKey: "landing.architecture.nodes.0.title" as const, bodyKey: "landing.architecture.nodes.0.body" as const},
  {id: "02", titleKey: "landing.architecture.nodes.1.title" as const, bodyKey: "landing.architecture.nodes.1.body" as const},
  {id: "03", titleKey: "landing.architecture.nodes.2.title" as const, bodyKey: "landing.architecture.nodes.2.body" as const},
  {id: "04", titleKey: "landing.architecture.nodes.3.title" as const, bodyKey: "landing.architecture.nodes.3.body" as const},
];

export function Architecture() {
  const {t} = useLocalizedText();

  return (
    <section className="bg-surface relative scroll-mt-24 border-y border-border py-20 lg:py-28" id="architecture">
      <div className="bp-grid absolute inset-0 opacity-40" aria-hidden="true" />
      <div className="relative mx-auto max-w-[1200px] px-6">
        <div className="flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
          <div className="max-w-2xl">
            <Reveal>
              <SectionLabel index="FIG. 06">{t("landing.architecture.sectionLabel")}</SectionLabel>
            </Reveal>
            <Reveal delay={0.05}>
              <h2 className="mt-6 text-4xl font-semibold tracking-tight sm:text-5xl">
                {t("landing.architecture.heading")}
              </h2>
            </Reveal>
          </div>
          <Reveal delay={0.1}>
            <p className="text-muted max-w-sm leading-relaxed lg:text-right">
              {t("landing.architecture.subheading")}
            </p>
          </Reveal>
        </div>

        <Reveal delay={0.1} y={24}>
          <div className="relative mt-12 overflow-hidden border border-border bg-background">
            <div className="bp-grid-fine absolute inset-0 opacity-50" aria-hidden="true" />
            <div className="absolute left-4 top-4 z-10">
              <Annotation className="bp-label-accent">FIG. 06 — TOPOLOGY</Annotation>
            </div>
            <Image
              alt={figures.architecture.alt}
              className="relative z-[1] mx-auto w-full max-w-5xl object-cover"
              src={figures.architecture.src}
              width={1200}
              height={600}
              unoptimized
            />
            <CornerMarks />
          </div>
        </Reveal>

        <div className="mt-12 grid gap-x-8 gap-y-10 sm:grid-cols-2 lg:grid-cols-4">
          {nodeKeys.map((n, i) => (
            <Reveal key={n.id} delay={0.05 * i}>
              <div>
                <div className="flex items-center gap-3">
                  <span className="text-accent font-mono text-sm font-semibold">{n.id}</span>
                  <DrawLineX className="flex-1" delay={0.1 * i} />
                </div>
                <h3 className="mt-4 text-base font-semibold tracking-tight">{t(n.titleKey)}</h3>
                <p className="text-muted mt-2 text-sm leading-relaxed">{t(n.bodyKey)}</p>
              </div>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  );
}
