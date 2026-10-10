/** Root attribute next-themes writes the resolved `light` | `dark` mode to. */
export const themeModeAttribute = "data-theme";

/** HeroUI Pro palette class applied for each resolved next-themes mode. */
export type ThemePaletteClasses = Readonly<Record<"light" | "dark", string>>;

/**
 * HeroUI theme a host renders: `base` ships with `@heroui/styles`; every other
 * name is a `@heroui-pro/react/themes/<name>` stylesheet.
 */
export type HeroUiTheme = "base" | "brutalism" | "glass" | "mouve";

/**
 * Palette classes that activate a HeroUI theme's CSS and its design-system
 * exports: base uses `light` | `dark`, Pro themes use `<name>-light` | `<name>-dark`.
 */
export function themePaletteClassesFor(theme: HeroUiTheme): ThemePaletteClasses {
  return theme === "base"
    ? { light: "light", dark: "dark" }
    : { light: `${theme}-light`, dark: `${theme}-dark` };
}

/** Palette classes that activate the HeroUI Pro Brutalism theme CSS. */
export const brutalismThemePaletteClasses = themePaletteClassesFor("brutalism");

/**
 * Mirrors the root mode attribute onto its palette class, now and on every
 * later change. HeroUI Pro theme CSS only activates under its palette names,
 * while HeroUI's base dark tokens and `dark:` variant require
 * `data-theme="light" | "dark"`, so next-themes keeps owning the mode and the
 * palette class is derived from it. Serialized into an inline script, so it
 * must stay self-contained.
 */
export function syncThemePaletteClass(
  modeAttribute: string,
  classes: Readonly<Record<string, string>>,
) {
  const root = document.documentElement;
  const apply = () => {
    const paletteClass = classes[root.getAttribute(modeAttribute) ?? ""];
    for (const candidate of Object.values(classes)) {
      if (candidate !== paletteClass) root.classList.remove(candidate);
    }
    if (paletteClass) root.classList.add(paletteClass);
  };
  new MutationObserver(apply).observe(root, { attributeFilter: [modeAttribute] });
  apply();
}

export interface ThemePaletteClassScriptProps {
  classes: ThemePaletteClasses;
  nonce?: string;
}

/**
 * Renders in the document head, before next-themes' body script sets the mode,
 * so the observer applies the palette class before the first paint.
 */
export function ThemePaletteClassScript({ classes, nonce }: ThemePaletteClassScriptProps) {
  const script = `(${syncThemePaletteClass.toString()})(${JSON.stringify(themeModeAttribute)},${JSON.stringify(classes)})`;
  return (
    <script
      nonce={nonce}
      suppressHydrationWarning
      dangerouslySetInnerHTML={{ __html: script }}
    />
  );
}
