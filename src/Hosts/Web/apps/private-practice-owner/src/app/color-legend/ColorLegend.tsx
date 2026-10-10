"use client";

import type { ReactNode } from "react";
import { Button, Chip, Surface } from "@heroui/react";
import {
  HeaderIcon,
  actionRoleClassName,
  actionRoles,
  businessContexts,
  contextColorClassName,
  identityColorClassName,
  identitySlots,
} from "@vitarish/ui";
import { useLocalizedText } from "@/localization";

const statusColors = ["success", "warning", "danger", "default"] as const;

/** Renders every registered color role in the current theme for screenshot review. */
export function ColorLegend() {
  const { t } = useLocalizedText();

  return (
    <>
      <h1 className="text-2xl font-bold">{t("colorLegend.title")}</h1>

      <LegendSection title={t("colorLegend.contexts")}>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {businessContexts.map((context) => (
            <div key={context} className="flex items-center gap-3">
              <HeaderIcon icon="circle" level="page" context={context} />
              <HeaderIcon icon="circle" context={context} />
              <span
                className={`${contextColorClassName(context)} rounded-full bg-tint-soft px-3 py-1 text-sm font-medium text-tint-soft-foreground`}
              >
                {context}
              </span>
            </div>
          ))}
        </div>
      </LegendSection>

      <LegendSection title={t("colorLegend.identity")}>
        <div className="flex flex-wrap gap-3">
          {identitySlots.map((slot) => (
            <span key={slot} className="flex flex-col items-center gap-1">
              <span
                className={`${identityColorClassName(slot)} grid size-10 place-items-center rounded-full bg-tint font-semibold text-tint-foreground`}
              >
                {slot}
              </span>
              <span
                className={`${identityColorClassName(slot)} grid size-10 place-items-center rounded-full bg-tint-soft font-semibold text-tint-soft-foreground`}
              >
                {slot}
              </span>
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
        </div>
      </LegendSection>

      <LegendSection title={t("colorLegend.statusAndBrand")}>
        <div className="flex flex-wrap items-center gap-3">
          {statusColors.map((color) => (
            <Chip key={color} color={color} variant="soft">
              <Chip.Label>{color}</Chip.Label>
            </Chip>
          ))}
          <Button variant="primary">{t("colorLegend.brandAction")}</Button>
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
