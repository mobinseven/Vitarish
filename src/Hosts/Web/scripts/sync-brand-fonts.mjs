import { copyFileSync, existsSync, mkdirSync } from "node:fs";
import { join } from "node:path";

// Ahang is a licensed FontIran font, so the public repository never carries it. Each host copies it
// from the licensee's local font folder into its gitignored public directory; @vitarish/ui/typography.css
// serves it from /fonts/ahang/AhangVariable.woff and falls back to system fonts when it is absent.
export const brandFontDirectoryEnvironmentKey = "VITARISH_BRAND_FONT_DIR";
const sourceFile = "AhangVariableGX.woff";
const targetFile = "AhangVariable.woff";

/** Copies Ahang into `<appRoot>/public/fonts/ahang/` when missing; warns instead of failing without it. */
export function syncBrandFonts(appRoot) {
  const target = join(appRoot, "public", "fonts", "ahang");
  if (existsSync(join(target, targetFile))) return;

  const sourceDirectory = process.env[brandFontDirectoryEnvironmentKey];
  const source = sourceDirectory ? join(sourceDirectory, sourceFile) : undefined;
  if (!source || !existsSync(source)) {
    console.warn(
      `Ahang brand font not found; set ${brandFontDirectoryEnvironmentKey} to the folder holding ${sourceFile}. Using fallback fonts.`,
    );
    return;
  }
  mkdirSync(target, { recursive: true });
  copyFileSync(source, join(target, targetFile));
}
