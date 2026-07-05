"use client";

import Link from "next/link";
import { ItemCard } from "@heroui-pro/react";

export interface ClinicNavigationItem {
  title: string;
  description: string;
  href: string;
  icon: string;
}

interface ClinicNavigationGridProps {
  items: ClinicNavigationItem[];
}

export function ClinicNavigationGrid({ items }: ClinicNavigationGridProps) {
  return (
    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
      {items.map((item) => (
        <Link key={item.href} href={item.href} className="block h-full">
          <ItemCard className="h-full transition-colors hover:bg-surface-secondary" variant="outline">
            <ItemCard.Icon>
              <i aria-hidden className={`${item.icon} text-lg text-accent`} />
            </ItemCard.Icon>
            <ItemCard.Content>
              <ItemCard.Title>{item.title}</ItemCard.Title>
              <ItemCard.Description className="whitespace-normal text-pretty">
                {item.description}
              </ItemCard.Description>
            </ItemCard.Content>
          </ItemCard>
        </Link>
      ))}
    </div>
  );
}
