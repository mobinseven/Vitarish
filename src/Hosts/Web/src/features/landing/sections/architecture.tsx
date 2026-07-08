import Image from "next/image";

import {figures} from "@/features/landing/data/figures";
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
      <div className="relative mx-auto max-w-[1200px] px-6">
        <div className="flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
          <div className="max-w-2xl">
            <div className="flex items-center gap-3">
              <span className="bg-accent inline-block size-1.5" />
              <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-accent">
                <span className="text-muted mr-2">FIG. 06</span>
                {t("landing.architecture.sectionLabel")}
              </span>
            </div>
            <h2 className="mt-6 text-4xl font-semibold tracking-tight sm:text-5xl">
              {t("landing.architecture.heading")}
            </h2>
          </div>
          <p className="text-muted max-w-sm leading-relaxed lg:text-right">
            {t("landing.architecture.subheading")}
          </p>
        </div>

        <div className="relative mt-12 overflow-hidden border border-border bg-background">
          <div className="absolute left-4 top-4 z-10">
            <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-accent">FIG. 06 — TOPOLOGY</span>
          </div>
          <Image
            alt={figures.architecture.alt}
            className="relative z-[1] mx-auto w-full max-w-5xl object-cover"
            src={figures.architecture.src}
            width={1200}
            height={600}
            unoptimized
          />
        </div>

        <div className="mt-12 grid gap-x-8 gap-y-10 sm:grid-cols-2 lg:grid-cols-4">
          {nodeKeys.map((n) => (
            <div key={n.id}>
              <div className="flex items-center gap-3">
                <span className="text-accent font-mono text-sm font-semibold">{n.id}</span>
              </div>
              <h3 className="mt-4 text-base font-semibold tracking-tight">{t(n.titleKey)}</h3>
              <p className="text-muted mt-2 text-sm leading-relaxed">{t(n.bodyKey)}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
