/**
 * The host theme's chroma colors: vivid colors beyond accent. Feature packages
 * give each color its meaning; the values are owned by the host theme.
 */
export const chromaColors = [
  "primary",
  "secondary",
  "tertiary",
  "quaternary",
  "quinary",
  "senary",
] as const;

export type ChromaColor = (typeof chromaColors)[number];

/** Class name that paints a HeroUI Chip with a chroma color. */
export function chromaChipClassName(color: ChromaColor): string {
  return `vitarish-chroma-chip--${color}`;
}

/** Class name that paints a HeroUI Button as a destructive action. */
export const removeButtonClassName = "vitarish-remove-button";
