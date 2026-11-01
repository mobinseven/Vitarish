"use client";

import { Link } from "@heroui/react";
import NextLink from "next/link";
import { usePathname } from "next/navigation";
import type { ComponentProps } from "react";
import { Routes } from "../app/routes";

const navigationItems = [
  { label: "Home", href: Routes.home },
  { label: "About", href: Routes.about },
  { label: "Work", href: Routes.work },
  { label: "Contact", href: Routes.contact },
];

export function SiteNavigation() {
  const pathname = usePathname();

  return (
    <nav aria-label="Main" className="flex justify-center gap-6 p-6 text-sm">
      {navigationItems.map(({ label, href }) => {
        const isCurrent = pathname === href;
        return (
          <Link
            key={href}
            aria-current={isCurrent ? "page" : undefined}
            className={isCurrent ? "text-foreground no-underline" : "text-muted no-underline"}
            href={href}
            render={(props) => <NextLink {...(props as ComponentProps<typeof NextLink>)} href={href} />}
          >
            {label}
          </Link>
        );
      })}
    </nav>
  );
}
