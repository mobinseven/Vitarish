"use client";

import { useEffect, useState } from "react";
import { Button } from "@heroui/react";
import { useTheme } from "next-themes";
import { useLocalizedText } from "@/localization";
import { FaIcon } from "@vitarish/web-ui";

type ThemeMode = "light" | "dark" | "system";

const THEME_CYCLE: ThemeMode[] = ["light", "dark", "system"];

function resolveThemeMode(value: string | undefined): ThemeMode {
  if (value === "system") return "system";
  return value === "dark" ? "dark" : "light";
}

function nextTheme(current: ThemeMode): ThemeMode {
  const idx = THEME_CYCLE.indexOf(current);
  return THEME_CYCLE[(idx + 1) % THEME_CYCLE.length];
}

const THEME_ICONS: Record<ThemeMode, string> = {
  light: "sun",
  dark: "moon",
  system: "display",
};

export function ThemeSwitch() {
  const [isMounted, setIsMounted] = useState(false);
  const { setTheme, theme } = useTheme();
  const { t } = useLocalizedText();

  const currentTheme = resolveThemeMode(theme);

  const handleToggle = () => {
    setTheme(nextTheme(currentTheme));
  };

  useEffect(() => {
    setIsMounted(true);
  }, []);

  if (!isMounted) return <div aria-hidden="true" className="w-9 h-9" />;

  return (
    <Button
      isIconOnly
      variant="ghost"
      aria-label={t(`appShell.theme.${currentTheme}`)}
      onPress={handleToggle}
    >
      <FaIcon icon={THEME_ICONS[currentTheme]} className="size-4" />
    </Button>
  );
}
