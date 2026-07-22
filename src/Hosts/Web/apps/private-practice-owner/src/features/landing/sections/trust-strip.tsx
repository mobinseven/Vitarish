import { useLocalizedText } from "@/localization";
import { PageContainer } from "@vitarish/web-ui/layout";

const companies = [
  { mark: "NW", nameKey: "landing.trustStrip.companies.nw.name" as const },
  { mark: "CB", nameKey: "landing.trustStrip.companies.cb.name" as const },
  { mark: "HL", nameKey: "landing.trustStrip.companies.hl.name" as const },
  { mark: "MD", nameKey: "landing.trustStrip.companies.md.name" as const },
  { mark: "VG", nameKey: "landing.trustStrip.companies.vg.name" as const },
  { mark: "PX", nameKey: "landing.trustStrip.companies.px.name" as const },
];

export function TrustStrip() {
  const { t } = useLocalizedText();

  return (
    <section className="relative">
      <PageContainer>
        <div className="border-border border-t" />
        <div className="py-10">
          <p className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-muted/88 text-center">
            {t("landing.trustStrip.eyebrow")}
          </p>
          <ul className="mt-8 grid grid-cols-1 gap-x-6 gap-y-8 min-[360px]:grid-cols-2 sm:grid-cols-3 lg:grid-cols-6">
            {companies.map((c) => (
              <li
                key={c.mark}
                className="flex min-w-0 items-center justify-center gap-2.5 opacity-80"
              >
                <span className="border-border text-accent flex size-8 shrink-0 items-center justify-center border font-mono text-[0.7rem] font-medium">
                  {c.mark}
                </span>
                <span className="text-foreground/80 min-w-0 break-words text-sm font-medium tracking-tight">
                  {t(c.nameKey)}
                </span>
              </li>
            ))}
          </ul>
        </div>
        <div className="border-border border-t" />
      </PageContainer>
    </section>
  );
}
