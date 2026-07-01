"use client";

import NextLink from "next/link";
import { useLocalizedText } from "@/localization";
import { ThemeSwitch } from "./ThemeSwitch";
import { SearchInput } from "./SearchInput";
import { TopNavbarMenuButton } from "./TopNavbarMenuButton";
import { UserMenu } from "./UserMenu";
import type { TopNavbarProps } from "./types";

export function TopNavbar({ navItems = [], onSearch }: TopNavbarProps) {
  const { t } = useLocalizedText();

  return (
    <nav className="sticky top-0 z-40 w-full border-b border-separator bg-background/70 backdrop-blur-lg">
      <header className="mx-auto flex h-16 max-w-[1280px] items-center justify-between gap-4 px-6">
        <div className="flex items-center gap-4">
          <NextLink className="flex items-center gap-1" href="/">
            <p className="font-bold text-inherit">{t("appShell.brand")}</p>
          </NextLink>
          <ul className="hidden lg:flex gap-4 ml-2">
            {navItems.map((item) => (
              <li key={item.href}>
                <NextLink
                  className="text-foreground hover:text-accent transition-colors"
                  href={item.href}
                >
                  {item.label}
                </NextLink>
              </li>
            ))}
          </ul>
        </div>

        <div className="hidden sm:flex items-center gap-2">
          <SearchInput onSearch={onSearch} />
          <ThemeSwitch />
          <UserMenu />
        </div>

        <TopNavbarMenuButton navItems={navItems} />
      </header>
    </nav>
  );
}
