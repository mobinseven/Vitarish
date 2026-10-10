"use client";

import type { ReactNode } from "react";
import { Suspense } from "react";
import { Card, Link, Separator } from "@heroui/react";
import { StandaloneCenteredPage } from "../layout";

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
    <StandaloneCenteredPage>
      <Card className="w-full max-w-md gap-6 p-6 sm:p-8">
        <Card.Header className="flex flex-col items-start gap-1.5">
          <h1 className="text-2xl font-semibold text-foreground">{title}</h1>
          <p className="text-sm text-muted">{subtitle}</p>
        </Card.Header>
        <Card.Content className="flex flex-col gap-6">
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
            <div className="flex flex-col items-center gap-4">
              <Separator />
              <Link href={backHref} className="text-sm text-muted">
                {backLabel}
              </Link>
            </div>
          ) : null}
        </Card.Content>
      </Card>
    </StandaloneCenteredPage>
  );
}
