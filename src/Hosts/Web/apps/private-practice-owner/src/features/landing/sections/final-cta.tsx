import { Button } from "@heroui/react";

import { BrandIsotipo } from "@/features/landing/brand";
import { FaIcon } from "@vitarish/web-ui";
import { PageContainer } from "@vitarish/web-ui/layout";
import { formatFigureLabel } from "@/features/landing/data/figures";
import { useLocalizedText } from "@/localization";

export function FinalCta() {
  const { t } = useLocalizedText();

  return (
    <section className="relative scroll-mt-24 py-20 lg:py-28" id="cta">
      <PageContainer>
        <div className="bg-surface relative overflow-hidden border border-border px-6 py-16 text-center lg:px-16 lg:py-24">
          <div className="relative">
            <div className="flex justify-center">
              <BrandIsotipo className="text-accent size-11" />
            </div>
            <div className="mt-8 flex justify-center">
              <div className="flex items-center gap-3">
                <span className="bg-accent inline-block size-1.5" />
                <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-accent">
                  <span className="text-muted mr-2">
                    {formatFigureLabel("13")}
                  </span>
                  {t("landing.finalCta.sectionLabel")}
                </span>
              </div>
            </div>
            <h2 className="mx-auto mt-6 max-w-3xl text-4xl font-semibold tracking-tight sm:text-5xl lg:text-6xl">
              {t("landing.finalCta.headline")}
            </h2>
            <p className="text-muted mx-auto mt-6 max-w-xl leading-relaxed">
              {t("landing.finalCta.body")}
            </p>
            <div className="mt-10 flex flex-wrap items-center justify-center gap-3">
              <Button
                onPress={() => {
                  window.location.hash = "top";
                }}
                size="lg"
                variant="primary"
              >
                {t("landing.finalCta.ctaPrimary")}
                <FaIcon icon="arrow-right" className="size-4" />
              </Button>
              <Button
                onPress={() => {
                  window.location.hash = "architecture";
                }}
                size="lg"
                variant="outline"
              >
                {t("landing.finalCta.ctaSecondary")}
              </Button>
            </div>
          </div>
        </div>
      </PageContainer>
    </section>
  );
}
