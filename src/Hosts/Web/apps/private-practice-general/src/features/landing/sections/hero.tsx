import Image from "next/image";
import {Button} from "@heroui/react";

import {FaIcon} from "@vitarish/web-ui";
import {
  figures,
  figureTitles,
  formatFigureLabel,
  formatFigureTitleLabel,
} from "@/features/landing/data/figures";
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
      <div className="border-border/70 pointer-events-none absolute inset-x-0 top-0 border-t" aria-hidden="true" />

      <div className="relative mx-auto grid max-w-[1200px] items-center gap-14 px-6 pb-20 pt-16 lg:grid-cols-[1.05fr_1fr] lg:pb-28 lg:pt-24">
        <div>
          <div className="flex items-center gap-3">
            <span className="bg-accent inline-block size-1.5" />
            <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-accent">
              <span className="text-muted mr-2">{formatFigureLabel("00")}</span>
              {t("landing.hero.sectionLabel")}
            </span>
          </div>

          <h1 className="mt-7 text-[2.75rem] font-semibold leading-[1.04] tracking-tight sm:text-6xl lg:text-[4.25rem]">
            {t("landing.hero.headline")}
          </h1>

          <p className="text-muted mt-6 max-w-xl text-base leading-relaxed sm:text-lg">
            {t("landing.hero.subheading")}
          </p>

          <div className="mt-9 flex flex-wrap items-center gap-3">
            {/* eslint-disable-next-line @typescript-eslint/no-explicit-any */}
            <Button render={(props: any) => <a href="#pricing" {...props} />} size="lg" variant="primary">
              {t("landing.hero.ctaPrimary")}
              <FaIcon icon="arrow-right" className="size-4" />
            </Button>
            <Button
              // eslint-disable-next-line @typescript-eslint/no-explicit-any
              render={(props: any) => <a href="#architecture" {...props} />}
              size="lg"
              variant="outline"
            >
              {t("landing.hero.ctaSecondary")}
              <FaIcon icon="arrow-up-right-from-square" className="size-4" />
            </Button>
          </div>

          <div className="mt-10 flex flex-wrap items-center gap-x-6 gap-y-2">
            {specs.map((spec) => (
              <div key={spec} className="flex items-center gap-2">
                <span className="bg-accent/70 size-1 rounded-full" />
                <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-muted/88">{spec}</span>
              </div>
            ))}
          </div>
        </div>

        <div className="relative">
          <div className="border-border bg-surface relative overflow-hidden border">
            <div className="absolute left-3 top-3 z-10">
              <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-accent">
                {formatFigureTitleLabel("01", figureTitles.coreUnit)}
              </span>
            </div>
            <Image
              alt={figures.hero.alt}
              className="relative z-[1] w-full object-cover"
              src={figures.hero.src}
              width={800}
              height={600}
              unoptimized
            />

            <div className="pointer-events-none absolute inset-0 z-[2] hidden lg:block">
              {callouts.map((c) => (
                <div
                  key={c.id}
                  className="absolute flex items-center gap-2"
                  style={{top: c.top, left: c.left}}
                >
                  <span className="bg-accent size-1.5 rounded-full ring-2 ring-accent/25" />
                  <span className="bg-background/75 border-border/70 border px-1.5 py-0.5 backdrop-blur-sm">
                    <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-foreground">
                      <span className="text-accent mr-1.5">{c.id}</span>
                      {t(c.key)}
                    </span>
                  </span>
                </div>
              ))}
            </div>
          </div>

          <div className="mt-3 flex items-center justify-between">
            <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-muted/88">{t("landing.hero.viewType")}</span>
            <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-muted/88">{t("landing.hero.scale")}</span>
          </div>
        </div>
      </div>
    </section>
  );
}
