import {Button, useTheme} from "@heroui/react";

import {Iconify} from "@/features/landing/icons/iconify";

export function ThemeToggle() {
  const {resolvedTheme, setTheme} = useTheme("system");
  const isDark = resolvedTheme === "dark";

  return (
    <Button
      isIconOnly
      aria-label={isDark ? "Switch to light mode" : "Switch to dark mode"}
      size="sm"
      variant="ghost"
      onPress={() => setTheme(isDark ? "light" : "dark")}
    >
      <Iconify icon={isDark ? "sun" : "moon"} className="size-4" />
    </Button>
  );
}
