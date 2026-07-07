import {Reveal, SectionLabel} from "@/components/blueprint";
import {useLocalizedText} from "@/localization";

const metricKeys = [
  {value: "99.99", unit: "%", labelKey: "landing.metrics.items.0.label" as const, noteKey: "landing.metrics.items.0.note" as const},
  {value: "12", unit: "ms", labelKey: "landing.metrics.items.1.label" as const, noteKey: "landing.metrics.items.1.note" as const},
  {value: "10", unit: "k+", labelKey: "landing.metrics.items.2.label" as const, noteKey: "landing.metrics.items.2.note" as const},
  {value: "38", unit: "s", labelKey: "landing.metrics.items.3.label" as const, noteKey: "landing.metrics.items.3.note" as const},
];

export function Metrics() {
  const {t} = useLocalizedText();

  return (
    <section className="relative py-20 lg:py-24">
      <div className="mx-auto max-w-[1200px] px-6">
        <Reveal>
          <SectionLabel index="FIG. 07" className="justify-center">
            {t("landing.metrics.sectionLabel")}
          </SectionLabel>
        </Reveal>

        <div className="mt-12 grid grid-cols-2 lg:grid-cols-4">
          {metricKeys.map((m, i) => (
            <Reveal key={m.labelKey} delay={0.05 * i}>
              <div
                className={`relative px-4 py-6 lg:px-8 ${
                  i !== 0 ? "border-l border-border" : ""
                } ${i === 2 ? "border-l lg:border-l" : ""} ${
                  i >= 2 ? "border-t border-border lg:border-t-0" : ""
                }`}
              >
                <div className="flex items-baseline gap-1">
                  <span className="text-5xl font-semibold tracking-tight tabular-nums sm:text-6xl">
                    {m.value}
                  </span>
                  <span className="text-accent font-mono text-xl font-medium">{m.unit}</span>
                </div>
                <p className="text-foreground mt-4 text-sm font-medium">{t(m.labelKey)}</p>
                <p className="bp-label mt-2">{t(m.noteKey)}</p>
              </div>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  );
}
