"use client";

import { useState, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import { useTheme } from "next-themes";
import { createCommandGroups, type Command, type CommandGroup } from "../commandRegistry";

export function useCommandPalette() {
  const [isOpen, setOpen] = useState(false);
  const router = useRouter();
  const { theme, setTheme } = useTheme();

  const navigate = useCallback((href: string) => router.push(href), [router]);

  const toggleTheme = useCallback(() => {
    setTheme(theme === "dark" ? "light" : "dark");
  }, [theme, setTheme]);

  const commandGroups: CommandGroup[] = createCommandGroups(navigate, toggleTheme);

  useEffect(() => {
    function handleKeyDown(e: KeyboardEvent) {
      if ((e.metaKey || e.ctrlKey) && e.key === "k") {
        e.preventDefault();
        setOpen((prev) => !prev);
      }
    }
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, []);

  const executeCommand = useCallback((command: Command) => {
    command.action();
    setOpen(false);
  }, []);

  return { isOpen, setOpen, commandGroups, executeCommand };
}
