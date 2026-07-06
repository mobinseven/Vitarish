"use client";

import { useEffect, useState, type JSX } from "react";
import { Button } from "@heroui/react";
import { useTheme } from "next-themes";
import { useLocalizedText } from "@/localization";

type Theme = "light" | "dark" | "system";

const THEME_CYCLE: Theme[] = ["light", "dark", "system"];

function nextTheme(current: Theme): Theme {
  const idx = THEME_CYCLE.indexOf(current);
  return THEME_CYCLE[(idx + 1) % THEME_CYCLE.length];
}

function SunIcon() {
  return (
    <svg aria-hidden="true" focusable="false" height={20} role="presentation" viewBox="0 0 24 24" width={20}>
      <g fill="currentColor">
        <path d="M19 12a7 7 0 11-7-7 7 7 0 017 7z" />
        <path d="M12 22.96a.969.969 0 01-1-.96v-.08a1 1 0 012 0 1.038 1.038 0 01-1 1.04zm7.14-2.82a1.024 1.024 0 01-.71-.29l-.13-.13a1 1 0 011.41-1.41l.13.13a1 1 0 010 1.41.984.984 0 01-.7.29zm-14.28 0a1.024 1.024 0 01-.71-.29 1 1 0 010-1.41l.13-.13a1 1 0 011.41 1.41l-.13.13a1 1 0 01-.7.29zM22 13h-.08a1 1 0 010-2 1.038 1.038 0 011.04 1 .969.969 0 01-.96 1zM2.08 13H2a1 1 0 010-2 1.038 1.038 0 011.04 1 .969.969 0 01-.96 1zm16.93-7.01a1.024 1.024 0 01-.71-.29 1 1 0 010-1.41l.13-.13a1 1 0 011.41 1.41l-.13.13a.984.984 0 01-.7.29zm-14.02 0a1.024 1.024 0 01-.71-.29l-.13-.14a1 1 0 011.41-1.41l.13.13a1 1 0 010 1.41.97.97 0 01-.7.3zM12 3.04a.969.969 0 01-1-.96V2a1 1 0 012 0 1.038 1.038 0 01-1 1.04z" />
      </g>
    </svg>
  );
}

function MoonIcon() {
  return (
    <svg aria-hidden="true" focusable="false" height={20} role="presentation" viewBox="0 0 24 24" width={20}>
      <path d="M21.53 15.93c-.16-.27-.61-.69-1.73-.49a8.46 8.46 0 01-1.88.13 8.409 8.409 0 01-5.91-2.82 8.068 8.068 0 01-1.44-8.66c.44-1.01.13-1.54-.09-1.76s-.77-.55-1.83-.11a10.318 10.318 0 00-6.32 10.21 10.475 10.475 0 007.04 8.99 10 10 0 002.89.55c.16.01.32.02.48.02a10.5 10.5 0 008.47-4.27c.67-.93.49-1.519.32-1.79z" fill="currentColor" />
    </svg>
  );
}

function MonitorIcon() {
  return (
    <svg aria-hidden="true" focusable="false" height={20} role="presentation" viewBox="0 0 24 24" width={20}>
      <g fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <rect x="2" y="3" width="20" height="14" rx="2" ry="2" />
        <line x1="8" y1="21" x2="16" y2="21" />
        <line x1="12" y1="17" x2="12" y2="21" />
      </g>
    </svg>
  );
}

const THEME_ICONS: Record<Theme, () => JSX.Element> = {
  light: SunIcon,
  dark: MoonIcon,
  system: MonitorIcon,
};

export function ThemeSwitch() {
  const [isMounted, setIsMounted] = useState(false);
  const { setTheme, theme } = useTheme();
  const { t } = useLocalizedText();

  const currentTheme: Theme = (theme as Theme) ?? "system";

  const handleToggle = () => {
    setTheme(nextTheme(currentTheme));
  };

  useEffect(() => {
    setIsMounted(true);
  }, []);

  if (!isMounted) return <div aria-hidden="true" className="w-9 h-9" />;

  const Icon = THEME_ICONS[currentTheme];

  return (
    <Button
      isIconOnly
      variant="ghost"
      aria-label={t(`appShell.theme.${currentTheme}`)}
      onPress={handleToggle}
    >
      <Icon />
    </Button>
  );
}
