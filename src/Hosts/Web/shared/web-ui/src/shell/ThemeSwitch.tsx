"use client";

import { useEffect, useState } from "react";
import { Button, Tooltip } from "@heroui/react";
import { useTheme } from "next-themes";
import { useLocalizedText } from "@vitarish/web-localization";
import { FaIcon } from "../icons/FaIcon";

type ThemeMode = "light" | "dark" | "system";
const themeCycle: ThemeMode[] = ["light", "dark", "system"];
const themeIcons: Record<ThemeMode, string> = { light: "sun", dark: "moon", system: "display" };

export function ThemeSwitch() {
  const [isMounted, setIsMounted] = useState(false);
  const { setTheme, theme } = useTheme();
  const { t } = useLocalizedText();
  const currentTheme: ThemeMode = theme === "system" ? "system" : theme === "dark" ? "dark" : "light";

  useEffect(() => setIsMounted(true), []);
  if (!isMounted) return <div aria-hidden="true" className="h-9 w-9" />;

  const label = t(`appShell.theme.${currentTheme}`);

  return (
    <Tooltip>
      <Button
        fullWidth
        size="sm"
        variant="ghost"
        aria-label={label}
        className="justify-start gap-3 px-3 text-sm text-muted"
        onPress={() => setTheme(themeCycle[(themeCycle.indexOf(currentTheme) + 1) % themeCycle.length])}
      >
        <FaIcon icon={themeIcons[currentTheme]} className="size-4 shrink-0" />
        <span data-sidebar="label" className="truncate">{label}</span>
      </Button>
      <Tooltip.Content>{label}</Tooltip.Content>
    </Tooltip>
  );
}
