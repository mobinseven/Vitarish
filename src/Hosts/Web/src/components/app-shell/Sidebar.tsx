"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { SidebarProps } from "./types";

export function Sidebar({ title, links, exactMatchHrefs = [] }: SidebarProps) {
  const pathname = usePathname();

  return (
    <aside className="w-64 border-e border-default-200 p-4">
      <h2 className="text-lg font-bold mb-4">{title}</h2>
      <nav className="flex flex-col gap-1">
        {links.map((link) => {
          const isExact = exactMatchHrefs.includes(link.href);
          const isActive = isExact
            ? pathname === link.href
            : pathname === link.href || pathname.startsWith(`${link.href}/`);

          return (
            <Link
              key={link.href}
              href={link.href}
              className={`flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors ${
                isActive
                  ? "bg-accent-soft text-accent font-medium"
                  : "text-default-500 hover:text-foreground hover:bg-default-100"
              }`}
            >
              <i className={`${link.icon} w-4 text-center text-xs`} />
              {link.label}
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}
