"use client";

import type { ReactNode } from "react";
import { Suspense } from "react";
import { Card, Link } from "@heroui/react";

export interface LoginPanelProps {
  title: string;
  subtitle: string;
  children: ReactNode;
  backHref?: string;
  backLabel?: string;
  skeletonHeight?: string;
}

export function LoginPanel({
  backHref,
  backLabel,
  children,
  skeletonHeight = "128px",
  subtitle,
  title,
}: LoginPanelProps) {
  return (
    <main className="grid min-h-screen place-items-center px-4">
      <Card className="w-full max-w-md">
        <Card.Header className="flex flex-col items-start gap-1">
          <h1 className="text-2xl font-bold">{title}</h1>
          <p className="text-sm text-muted">{subtitle}</p>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          <Suspense
            fallback={
              <div
                className="w-full animate-pulse rounded-lg bg-surface-secondary"
                style={{ height: skeletonHeight }}
              />
            }
          >
            {children}
          </Suspense>
          {backHref && backLabel ? (
            <div className="flex flex-col items-center gap-2 text-sm">
              <Link href={backHref} className="text-sm">
                {backLabel}
              </Link>
            </div>
          ) : null}
        </Card.Content>
      </Card>
    </main>
  );
}
