"use client";

import type { ReactNode } from "react";
import { Button, Chip, Surface } from "@heroui/react";
import { clinicOperationButtonClassName, clinicOperations } from "@vitarish/clinic";
import { FaIcon, HeaderIcon, removeButtonClassName } from "@vitarish/ui";
import { useLocalizedText } from "@/localization";

const paletteSwatches = [
  { name: "neutral", className: "bg-surface-secondary text-foreground" },
  { name: "accent", className: "bg-accent text-accent-foreground" },
  { name: "chroma-primary", className: "bg-chroma-primary text-chroma-primary-foreground" },
  { name: "chroma-secondary", className: "bg-chroma-secondary text-chroma-secondary-foreground" },
  { name: "chroma-tertiary", className: "bg-chroma-tertiary text-chroma-tertiary-foreground" },
  { name: "chroma-quaternary", className: "bg-chroma-quaternary text-chroma-quaternary-foreground" },
  { name: "chroma-quinary", className: "bg-chroma-quinary text-chroma-quinary-foreground" },
  { name: "chroma-senary", className: "bg-chroma-senary text-chroma-senary-foreground" },
] as const;

const statusColors = ["success", "warning", "danger", "default"] as const;
const headerIconTones = ["neutral", "success", "warning", "danger"] as const;

/** Renders the app palette, operation colors, and status colors for screenshot review. */
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
              className={`${swatch.className} grid h-16 w-40 place-items-center rounded-2xl text-sm font-semibold`}
            >
              {swatch.name}
            </span>
          ))}
        </div>
      </LegendSection>

      <LegendSection title={t("colorLegend.actions")}>
        <div className="flex flex-wrap gap-3">
          {clinicOperations.map((operation) => (
            <Button key={operation} className={clinicOperationButtonClassName(operation)}>
              <FaIcon icon="circle" aria-hidden="true" />
              {operation}
            </Button>
          ))}
          <Button variant="tertiary">{t("colorLegend.genericAction")}</Button>
          <Button className={removeButtonClassName}>{t("colorLegend.removeAction")}</Button>
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
