import {Button} from "@heroui/react";

import {BrandIsotipo} from "@/features/landing/brand";
import {Iconify} from "@/features/landing/icons/iconify";
import {CornerMarks, Reveal, SectionLabel} from "@/components/blueprint";
import {useLocalizedText} from "@/localization";

export function FinalCta() {
  const {t} = useLocalizedText();

  return (
    <section className="relative scroll-mt-24 py-20 lg:py-28" id="cta">
      <div className="mx-auto max-w-[1200px] px-6">
        <Reveal y={24}>
          <div className="bg-surface relative overflow-hidden border border-border px-6 py-16 text-center lg:px-16 lg:py-24">
            <div className="bp-grid bp-grid-mask absolute inset-0 opacity-70" aria-hidden="true" />
            <CornerMarks />

            <div className="relative">
              <div className="flex justify-center">
                <BrandIsotipo className="text-accent size-11" />
              </div>
              <div className="mt-8 flex justify-center">
                <SectionLabel index="FIG. 13">{t("landing.finalCta.sectionLabel")}</SectionLabel>
              </div>
              <h2 className="mx-auto mt-6 max-w-3xl text-4xl font-semibold tracking-tight sm:text-5xl lg:text-6xl">
                {t("landing.finalCta.headline")}
              </h2>
              <p className="text-muted mx-auto mt-6 max-w-xl leading-relaxed">
                {t("landing.finalCta.body")}
              </p>
              <div className="mt-10 flex flex-wrap items-center justify-center gap-3">
                {/* eslint-disable-next-line @typescript-eslint/no-explicit-any */}
                <Button render={(props: any) => <a href="#top" {...props} />} size="lg" variant="primary">
                  {t("landing.finalCta.ctaPrimary")}
                  <Iconify icon="arrow-right" className="size-4" />
                </Button>
                <Button
                  // eslint-disable-next-line @typescript-eslint/no-explicit-any
                  render={(props: any) => <a href="#architecture" {...props} />}
                  size="lg"
                  variant="outline"
                >
                  {t("landing.finalCta.ctaSecondary")}
                </Button>
              </div>
            </div>
          </div>
        </Reveal>
      </div>
    </section>
  );
}
