import {RulerLine, Reveal, Annotation} from "@/components/blueprint";
import {useLocalizedText} from "@/localization";

const companies = [
  {mark: "NW", nameKey: "landing.trustStrip.companies.nw.name" as const},
  {mark: "CB", nameKey: "landing.trustStrip.companies.cb.name" as const},
  {mark: "HL", nameKey: "landing.trustStrip.companies.hl.name" as const},
  {mark: "MD", nameKey: "landing.trustStrip.companies.md.name" as const},
  {mark: "VG", nameKey: "landing.trustStrip.companies.vg.name" as const},
  {mark: "PX", nameKey: "landing.trustStrip.companies.px.name" as const},
];

export function TrustStrip() {
  const {t} = useLocalizedText();

  return (
    <section className="relative">
      <div className="mx-auto max-w-[1200px] px-6">
        <RulerLine />
        <div className="py-10">
          <Reveal>
            <p className="bp-label text-center">{t("landing.trustStrip.eyebrow")}</p>
          </Reveal>
          <Reveal delay={0.06}>
            <ul className="mt-8 grid grid-cols-2 gap-x-6 gap-y-8 sm:grid-cols-3 lg:grid-cols-6">
              {companies.map((c) => (
                <li key={c.mark} className="flex items-center justify-center gap-2.5 opacity-80">
                  <span className="border-border text-accent flex size-8 shrink-0 items-center justify-center border font-mono text-[0.7rem] font-medium">
                    {c.mark}
                  </span>
                  <span className="text-foreground/80 text-sm font-medium tracking-tight">
                    {t(c.nameKey)}
                  </span>
                </li>
              ))}
            </ul>
          </Reveal>
        </div>
        <div className="relative">
          <RulerLine />
          <div className="absolute -top-2 right-0">
            <Annotation>n = 6</Annotation>
          </div>
        </div>
      </div>
    </section>
  );
}
