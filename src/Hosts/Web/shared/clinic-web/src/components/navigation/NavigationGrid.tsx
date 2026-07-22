"use client";

import { ItemCard } from "@heroui-pro/react";
import { FaIcon } from "@vitarish/web-ui";
import type { NavigationIntent } from "../../navigation";

export interface NavigationItem {
  title: string;
  description: string;
  intent: NavigationIntent;
  icon: string;
}

interface NavigationGridProps {
  items: NavigationItem[];
  onNavigate?: (intent: NavigationIntent) => void;
}

export function NavigationGrid({ items, onNavigate }: NavigationGridProps) {
  return (
    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
      {items.map((item) => (
        <button
          key={item.intent.kind}
          type="button"
          title={item.title}
          className="block h-full text-left"
          onClick={() => onNavigate?.(item.intent)}
        >
          <ItemCard
            className="h-full transition-colors hover:bg-surface-secondary"
            variant="outline"
          >
            <ItemCard.Icon>
              <FaIcon
                icon={item.icon}
                aria-hidden="true"
                className="text-lg text-accent"
              />
            </ItemCard.Icon>
            <ItemCard.Content>
              <ItemCard.Title>{item.title}</ItemCard.Title>
              <ItemCard.Description className="whitespace-normal text-pretty">
                {item.description}
              </ItemCard.Description>
            </ItemCard.Content>
          </ItemCard>
        </button>
      ))}
    </div>
  );
}
