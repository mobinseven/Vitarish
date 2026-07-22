"use client";

import type { ReactNode } from "react";
import { ItemCard } from "@heroui-pro/react/item-card";

export interface DetailGridItem {
  label: string;
  value: ReactNode;
}

interface DetailGridProps {
  items: DetailGridItem[];
  className?: string;
}

export function DetailGrid({
  items,
  className = "grid gap-3 sm:grid-cols-2",
}: DetailGridProps) {
  return (
    <div className={className}>
      {items.map((item) => (
        <ItemCard key={item.label} className="min-w-0" variant="secondary">
          <ItemCard.Content>
            <ItemCard.Description>{item.label}</ItemCard.Description>
            <ItemCard.Title className="whitespace-normal break-words">
              {item.value}
            </ItemCard.Title>
          </ItemCard.Content>
        </ItemCard>
      ))}
    </div>
  );
}
