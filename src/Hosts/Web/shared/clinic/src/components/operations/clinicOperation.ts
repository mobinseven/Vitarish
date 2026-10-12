import { chromaButtonClassName, type ChromaColor } from "@vitarish/ui/chroma";

/**
 * The clinical operations users start from hub pages. Each keeps one chroma
 * color on every page, including its form's commit button, so repeat visitors
 * find it by color (docs/hosts/web/color-roles.md).
 */
const clinicOperationColors = {
  visit: "primary",
  procedure: "secondary",
  prescription: "tertiary",
  appointment: "quinary",
} as const satisfies Record<string, ChromaColor>;

export type ClinicOperation = keyof typeof clinicOperationColors;

export const clinicOperations = Object.keys(clinicOperationColors) as ClinicOperation[];

/** Class name that paints a HeroUI Button with its clinical operation's color. */
export function clinicOperationButtonClassName(operation: ClinicOperation): string {
  return chromaButtonClassName(clinicOperationColors[operation]);
}
