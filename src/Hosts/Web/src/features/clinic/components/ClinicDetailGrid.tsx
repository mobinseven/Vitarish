"use client";

import type { ReactNode } from "react";
import { ItemCard } from "@heroui-pro/react";

export interface ClinicDetailItem {
  label: string;
  value: ReactNode;
}

interface ClinicDetailGridProps {
  items: ClinicDetailItem[];
  className?: string;
}

export function ClinicDetailGrid({ items, className = "grid gap-3 sm:grid-cols-2" }: ClinicDetailGridProps) {
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
