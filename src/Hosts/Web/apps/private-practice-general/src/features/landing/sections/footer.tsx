import {Link} from "@heroui/react";

import {BrandLogotipo, brand} from "@/features/landing/brand";
import {useLocalizedText} from "@/localization";
import {PageContainer} from "@vitarish/web-ui/layout";

const columnKeys = [
  {
    titleKey: "landing.footer.columns.0.title" as const,
    links: [
      {labelKey: "landing.footer.columns.0.links.0.label" as const, href: "#pillars"},
      {labelKey: "landing.footer.columns.0.links.1.label" as const, href: "#architecture"},
      {labelKey: "landing.footer.columns.0.links.2.label" as const, href: "#api"},
      {labelKey: "landing.footer.columns.0.links.3.label" as const, href: "#pricing"},
    ],
  },
  {
    titleKey: "landing.footer.columns.1.title" as const,
    links: [
      {labelKey: "landing.footer.columns.1.links.0.label" as const, href: "#api"},
      {labelKey: "landing.footer.columns.1.links.1.label" as const, href: "#top"},
      {labelKey: "landing.footer.columns.1.links.2.label" as const, href: "#top"},
      {labelKey: "landing.footer.columns.1.links.3.label" as const, href: "#features"},
    ],
  },
  {
    titleKey: "landing.footer.columns.2.title" as const,
    links: [
      {labelKey: "landing.footer.columns.2.links.0.label" as const, href: "#top"},
      {labelKey: "landing.footer.columns.2.links.1.label" as const, href: "#top"},
      {labelKey: "landing.footer.columns.2.links.2.label" as const, href: "#top"},
      {labelKey: "landing.footer.columns.2.links.3.label" as const, href: "#cta"},
    ],
  },
];

export function Footer() {
  const {t} = useLocalizedText();

  return (
    <div className="relative border-t border-border">
      <PageContainer className="relative py-16">
        <div className="grid gap-12 lg:grid-cols-[1.4fr_1fr_1fr_1fr]">
          <div>
            <BrandLogotipo className="h-7 w-auto" />
            <p className="text-muted mt-5 max-w-xs text-sm leading-relaxed">
              {t("landing.footer.brandDescription")}
            </p>
            <div className="mt-6 flex items-center gap-2">
              <span className="bg-accent/70 size-1 rounded-full" />
              <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-muted/88">{t("landing.footer.statusLabel")}</span>
            </div>
          </div>

          {columnKeys.map((col) => (
            <div key={col.titleKey}>
              <p className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-muted/88">{t(col.titleKey)}</p>
              <ul className="mt-5 flex flex-col gap-3">
                {col.links.map((link) => (
                  <li key={link.labelKey}>
                    <Link
                      href={link.href}
                      className="text-muted hover:text-foreground text-sm no-underline transition-colors"
                    >
                      {t(link.labelKey)}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        <div className="mt-14 flex flex-col items-start justify-between gap-4 border-t border-border pt-6 sm:flex-row sm:items-center">
          <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-muted/88">
            © 2025 {brand.name} · all rights reserved
          </span>
          <div className="flex items-center gap-5">
            <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-accent">{brand.shortName?.toUpperCase()}{" // v2.4.0"}</span>
            <span className="font-mono text-[0.6875rem] leading-none tracking-[0.18em] uppercase text-muted/88">region: us-east-1</span>
          </div>
        </div>
      </PageContainer>
    </div>
  );
}
