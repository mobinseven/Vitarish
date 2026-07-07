import {figures} from "@/features/landing/data/figures";
import type {Figure as FigureType} from "@/features/landing/data/figures";
import {FaIcon} from "@/features/landing/icons/fa-icon";
import {DrawLineX, Figure, Reveal, SectionLabel} from "@/components/blueprint";
import {useLocalizedText} from "@/localization";

type Row = {
  fig: string;
  figure: FigureType;
  eyebrowKey: string;
  titleKey: string;
  bodyKey: string;
  pointKeys: string[];
};

const rowKeys: Row[] = [
  {
    fig: "FIG. 09",
    figure: figures.cooling,
    eyebrowKey: "landing.featureRows.items.0.eyebrow",
    titleKey: "landing.featureRows.items.0.title",
    bodyKey: "landing.featureRows.items.0.body",
    pointKeys: ["landing.featureRows.items.0.point0", "landing.featureRows.items.0.point1", "landing.featureRows.items.0.point2"],
  },
  {
    fig: "FIG. 10",
    figure: figures.observe,
    eyebrowKey: "landing.featureRows.items.1.eyebrow",
    titleKey: "landing.featureRows.items.1.title",
    bodyKey: "landing.featureRows.items.1.body",
    pointKeys: ["landing.featureRows.items.1.point0", "landing.featureRows.items.1.point1", "landing.featureRows.items.1.point2"],
  },
  {
    fig: "FIG. 11",
    figure: figures.secure,
    eyebrowKey: "landing.featureRows.items.2.eyebrow",
    titleKey: "landing.featureRows.items.2.title",
    bodyKey: "landing.featureRows.items.2.body",
    pointKeys: ["landing.featureRows.items.2.point0", "landing.featureRows.items.2.point1", "landing.featureRows.items.2.point2"],
  },
];

export function FeatureRows() {
  const {t} = useLocalizedText();

  return (
    <section className="bg-surface relative scroll-mt-24 border-y border-border py-20 lg:py-28" id="features">
      <div className="mx-auto max-w-[1200px] px-6">
        <div className="max-w-2xl">
          <Reveal>
            <SectionLabel index="FIG. 09—11">{t("landing.featureRows.sectionLabel")}</SectionLabel>
          </Reveal>
          <Reveal delay={0.05}>
            <h2 className="mt-6 text-4xl font-semibold tracking-tight sm:text-5xl">
              {t("landing.featureRows.heading")}
            </h2>
          </Reveal>
        </div>

        <div className="mt-16 flex flex-col gap-16 lg:gap-24">
          {rowKeys.map((row, i) => {
            const reversed = i % 2 === 1;

            return (
              <div
                key={row.titleKey}
                className="grid items-center gap-10 lg:grid-cols-2 lg:gap-16"
              >
                <Reveal className={reversed ? "lg:order-2" : ""}>
                  <div>
                    <div className="flex items-center gap-3">
                      <span className="bp-label bp-label-accent">
                        <span className="text-muted mr-2">{row.fig}</span>
                        {t(row.eyebrowKey)}
                      </span>
                      <DrawLineX className="hidden flex-1 sm:block" />
                    </div>
                    <h3 className="mt-5 text-2xl font-semibold tracking-tight sm:text-3xl">
                      {t(row.titleKey)}
                    </h3>
                    <p className="text-muted mt-4 max-w-md leading-relaxed">{t(row.bodyKey)}</p>
                    <ul className="mt-6 flex flex-col gap-2.5">
                      {row.pointKeys.map((pointKey) => (
                        <li key={pointKey} className="flex items-center gap-2.5">
                          <FaIcon icon="circle-check" className="text-accent size-4 shrink-0" />
                          <span className="text-foreground/90 text-sm">{t(pointKey)}</span>
                        </li>
                      ))}
                    </ul>
                  </div>
                </Reveal>

                <Reveal delay={0.08} y={24} className={reversed ? "lg:order-1" : ""}>
                  <Figure figure={row.figure} fig={row.fig} imgClassName="aspect-[4/3]" />
                </Reveal>
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
