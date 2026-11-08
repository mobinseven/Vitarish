"use client";

import type { PageBackNavigationProps } from "@vitarish/ui";
import { useLocalizedText } from "../../localization";
import type { NavigationIntent } from "../../navigation";

/** One ancestor of a page: its message key, or its own text such as a patient's name, and where pressing it goes. */
export interface PageAncestor {
  labelKey?: string;
  label?: string;
  intent: NavigationIntent;
}

/** The ancestors a clinic page can sit under, so every page names its parents the same way. */
export const pageAncestors = {
  settings: { labelKey: "clinic.dashboard.sections.settings.title", intent: { kind: "clinicSettings" } },
  patients: { labelKey: "clinic.patients.title", intent: { kind: "patientIndex" } },
  surgeries: { labelKey: "clinic.surgeries.title", intent: { kind: "surgeryIndex" } },
  shifts: { labelKey: "clinic.appointments.shifts.title", intent: { kind: "ownerShiftIndex" } },
  services: { labelKey: "clinic.services.title", intent: { kind: "serviceIndex" } },
  serviceTypes: { labelKey: "clinic.serviceTypes.title", intent: { kind: "serviceTypeIndex" } },
  serviceProviders: { labelKey: "clinic.serviceProviders.title", intent: { kind: "serviceProviderIndex" } },
  reservations: { labelKey: "clinic.appointments.reservations.title", intent: { kind: "ownerReservationIndex" } },
} as const satisfies Record<string, PageAncestor>;

/**
 * The back trail of a child page, from the top of the hierarchy down to its parent. Call it before any
 * early return, as with every hook.
 */
export function usePageBack(
  ancestors: readonly PageAncestor[],
  onNavigate: ((intent: NavigationIntent) => void) | undefined,
): PageBackNavigationProps {
  const { t } = useLocalizedText();
  const crumbs = ancestors.map((ancestor) => ({
    id: ancestor.labelKey ?? ancestor.label ?? "",
    label: ancestor.label ?? t(ancestor.labelKey ?? ""),
    onPress: () => onNavigate?.(ancestor.intent),
  }));
  const parent = crumbs.at(-1);

  return {
    crumbs,
    navigationLabel: t("clinic.navigation.pageLocation"),
    backLabel: parent ? t("clinic.navigation.backTo", { name: parent.label }) : "",
  };
}
