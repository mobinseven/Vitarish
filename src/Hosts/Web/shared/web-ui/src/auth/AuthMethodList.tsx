"use client";

import type { ReactNode } from "react";
import { Card } from "@heroui/react";

export interface AuthMethodListItem {
  id: string;
  href: string;
  label: string;
  icon?: ReactNode;
}

export interface AuthMethodListProps {
  methods: readonly AuthMethodListItem[];
  className?: string;
}

export function AuthMethodList({ className = "", methods }: AuthMethodListProps) {
  return (
    <div className={`flex flex-col gap-4 ${className}`.trim()}>
      {methods.map((method) => (
        <Card key={method.id}>
          <a
            href={method.href}
            className="flex h-14 w-full items-center gap-3 rounded-lg px-4 text-foreground transition-colors hover:bg-surface-secondary"
          >
            {method.icon}
            <span>{method.label}</span>
          </a>
        </Card>
      ))}
    </div>
  );
}
