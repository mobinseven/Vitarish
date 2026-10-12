import { chromaChipClassName, chromaColors } from "@vitarish/ui/chroma";

/**
 * Class name that paints a HeroUI Chip for a user-defined patient tag. The
 * color is picked from the tag text, so a tag keeps one color on every page
 * and users can spot it at a glance (docs/hosts/web/color-roles.md).
 */
export function patientTagClassName(tag: string): string {
  let hash = 0;
  for (const character of tag.trim().toLocaleLowerCase()) {
    hash = (hash * 31 + character.codePointAt(0)!) >>> 0;
  }

  return chromaChipClassName(chromaColors[hash % chromaColors.length]);
}
