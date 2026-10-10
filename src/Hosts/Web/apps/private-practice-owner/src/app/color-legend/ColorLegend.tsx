"use client";

import type { ReactNode } from "react";
import { Button, Chip, Surface } from "@heroui/react";
import { HeaderIcon, actionRoleClassName, actionRoles } from "@vitarish/ui";
import { useLocalizedText } from "@/localization";

const paletteSwatches = [
  { name: "neutral", className: "bg-surface-secondary text-foreground" },
  { name: "accent", className: "bg-accent text-accent-foreground" },
  { name: "highlight", className: "bg-highlight text-highlight-foreground" },
  { name: "support", className: "bg-support text-support-foreground" },
] as const;

const statusColors = ["success", "warning", "danger", "default"] as const;
const headerIconTones = ["neutral", "success", "warning", "danger"] as const;

/** Renders the app palette, action roles, and status colors for screenshot review. */
export function ColorLegend() {
  const { t } = useLocalizedText();

  return (
    <>
      <h1 className="text-2xl font-bold">{t("colorLegend.title")}</h1>

      <LegendSection title={t("colorLegend.palette")}>
        <div className="flex flex-wrap gap-3">
          {paletteSwatches.map((swatch) => (
            <span
              key={swatch.name}
              className={`${swatch.className} grid h-16 w-32 place-items-center rounded-2xl text-sm font-semibold`}
            >
              {swatch.name}
            </span>
          ))}
        </div>
      </LegendSection>

      <LegendSection title={t("colorLegend.actions")}>
        <div className="flex flex-wrap gap-3">
          {actionRoles.map((role) => (
            <Button key={role} className={actionRoleClassName(role)}>
              {role}
            </Button>
          ))}
          <Button variant="primary">{t("colorLegend.brandAction")}</Button>
        </div>
      </LegendSection>

      <LegendSection title={t("colorLegend.status")}>
        <div className="flex flex-wrap items-center gap-3">
          {statusColors.map((color) => (
            <Chip key={color} color={color} variant="soft">
              <Chip.Label>{color}</Chip.Label>
            </Chip>
          ))}
        </div>
      </LegendSection>

      <LegendSection title={t("colorLegend.headerIcons")}>
        <div className="flex flex-wrap items-center gap-3">
          {headerIconTones.map((tone) => (
            <span key={tone} className="flex items-center gap-2">
              <HeaderIcon icon="circle" level="page" tone={tone} />
              <HeaderIcon icon="circle" tone={tone} />
            </span>
          ))}
        </div>
      </LegendSection>
    </>
  );
}

function LegendSection({ children, title }: { children: ReactNode; title: string }) {
  return (
    <Surface className="flex flex-col gap-6 rounded-3xl p-6">
      <h2 className="text-lg font-semibold text-foreground">{title}</h2>
      {children}
    </Surface>
  );
}
