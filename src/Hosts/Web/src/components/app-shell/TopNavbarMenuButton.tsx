"use client";

import { useState } from "react";
import { Button } from "@heroui/react";
import Link from "next/link";
import { useLocalizedText } from "@/localization";
import type { NavItem } from "./types";

export interface TopNavbarMenuButtonProps {
  navItems?: NavItem[];
}

export function TopNavbarMenuButton({ navItems = [] }: TopNavbarMenuButtonProps) {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const { t } = useLocalizedText();

  return (
    <div className="flex sm:hidden items-center gap-2">
      <Button
        isIconOnly
        variant="tertiary"
        aria-label={isMenuOpen ? t("appShell.menu.close") : t("appShell.menu.open")}
        onPress={() => setIsMenuOpen(!isMenuOpen)}
      >
        <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          {isMenuOpen ? (
            <path d="M6 18L18 6M6 6l12 12" strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} />
          ) : (
            <path d="M4 6h16M4 12h16M4 18h16" strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} />
          )}
        </svg>
      </Button>

      {isMenuOpen && (
        <div className="absolute top-16 left-0 right-0 border-t border-separator bg-background z-50">
          <ul className="flex flex-col gap-2 p-4">
            {navItems.map((item) => (
              <li key={item.href}>
                <Link
                  className="block py-2 text-lg no-underline text-foreground"
                  href={item.href}
                  onClick={() => setIsMenuOpen(false)}
                >
                  {item.label}
                </Link>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
