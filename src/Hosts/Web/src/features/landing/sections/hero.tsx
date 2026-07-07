import Image from "next/image";
import {Button} from "@heroui/react";

import {Iconify} from "@/features/landing/icons/iconify";
import {figures} from "@/features/landing/data/figures";
import {Annotation, CornerMarks, Reveal, SectionLabel} from "@/components/blueprint";
import {useLocalizedText} from "@/localization";

const callouts = [
  {id: "01", key: "landing.hero.callout1" as const, top: "14%", left: "8%"},
  {id: "02", key: "landing.hero.callout2" as const, top: "46%", left: "58%"},
  {id: "03", key: "landing.hero.callout3" as const, top: "78%", left: "20%"},
];

const specs = ["v2.4.0", "region: us-east-1", "x: 1440", "grid: 56px"];

export function Hero() {
  const {t} = useLocalizedText();

  return (
    <section className="relative overflow-hidden" id="top">
      <div className="bp-grid bp-grid-mask absolute inset-0" aria-hidden="true" />
      <div className="border-border/70 pointer-events-none absolute inset-x-0 top-0 border-t" aria-hidden="true" />

      <div className="relative mx-auto grid max-w-[1200px] items-center gap-14 px-6 pb-20 pt-16 lg:grid-cols-[1.05fr_1fr] lg:pb-28 lg:pt-24">
        <div>
          <Reveal>
            <SectionLabel index="FIG. 00">{t("landing.hero.sectionLabel")}</SectionLabel>
          </Reveal>

          <Reveal delay={0.05}>
            <h1 className="mt-7 text-[2.75rem] font-semibold leading-[1.04] tracking-tight sm:text-6xl lg:text-[4.25rem]">
              {t("landing.hero.headline")}
            </h1>
          </Reveal>

          <Reveal delay={0.1}>
            <p className="text-muted mt-6 max-w-xl text-base leading-relaxed sm:text-lg">
              {t("landing.hero.subheading")}
            </p>
          </Reveal>

          <Reveal delay={0.15}>
            <div className="mt-9 flex flex-wrap items-center gap-3">
              {/* eslint-disable-next-line @typescript-eslint/no-explicit-any */}
              <Button render={(props: any) => <a href="#pricing" {...props} />} size="lg" variant="primary">
                {t("landing.hero.ctaPrimary")}
                <Iconify icon="arrow-right" className="size-4" />
              </Button>
              <Button
                // eslint-disable-next-line @typescript-eslint/no-explicit-any
                render={(props: any) => <a href="#architecture" {...props} />}
                size="lg"
                variant="outline"
              >
                {t("landing.hero.ctaSecondary")}
                <Iconify icon="arrow-up-right" className="size-4" />
              </Button>
            </div>
          </Reveal>

          <Reveal delay={0.2}>
            <div className="mt-10 flex flex-wrap items-center gap-x-6 gap-y-2">
              {specs.map((spec) => (
                <div key={spec} className="flex items-center gap-2">
                  <span className="bg-accent/70 size-1 rounded-full" />
                  <Annotation>{spec}</Annotation>
                </div>
              ))}
            </div>
          </Reveal>
        </div>

        <Reveal delay={0.1} y={24}>
          <div className="relative">
            <div className="border-border bg-surface relative overflow-hidden border">
              <div className="bp-grid-fine absolute inset-0 opacity-60" aria-hidden="true" />
              <div className="absolute left-3 top-3 z-10">
                <Annotation className="bp-label-accent">FIG. 01 — CORE UNIT</Annotation>
              </div>
              <Image
                alt={figures.hero.alt}
                className="relative z-[1] w-full object-cover"
                src={figures.hero.src}
                width={800}
                height={600}
                unoptimized
              />
              <CornerMarks />

              <div className="pointer-events-none absolute inset-0 z-[2] hidden lg:block">
                {callouts.map((c) => (
                  <div
                    key={c.id}
                    className="absolute flex items-center gap-2"
                    style={{top: c.top, left: c.left}}
                  >
                    <span className="bg-accent size-1.5 rounded-full ring-2 ring-accent/25" />
                    <span className="bg-background/75 border-border/70 border px-1.5 py-0.5 backdrop-blur-sm">
                      <Annotation className="text-foreground">
                        <span className="text-accent mr-1.5">{c.id}</span>
                        {t(c.key)}
                      </Annotation>
                    </span>
                  </div>
                ))}
              </div>
            </div>

            <div className="mt-3 flex items-center justify-between">
              <Annotation>{t("landing.hero.viewType")}</Annotation>
              <Annotation>{t("landing.hero.scale")}</Annotation>
            </div>
          </div>
        </Reveal>
      </div>
    </section>
  );
}
