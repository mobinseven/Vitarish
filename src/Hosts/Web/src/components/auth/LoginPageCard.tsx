"use client";

import { Suspense } from "react";
import { Card, Link } from "@heroui/react";

interface LoginPageCardProps {
  title: string;
  subtitle: string;
  skeletonHeight?: string;
  backHref?: string;
  backLabel?: string;
  children: React.ReactNode;
}

export function LoginPageCard({
  title,
  subtitle,
  skeletonHeight = "128px",
  backHref = "/login",
  backLabel = "Other sign-in methods",
  children,
}: LoginPageCardProps) {
  return (
    <div className="auth-bg flex min-h-screen items-center justify-center p-4">
      <Card className="glass-card w-full max-w-md">
        <Card.Header className="flex flex-col items-center gap-2 pb-2">
          <h1 className="text-2xl font-bold">{title}</h1>
          <p className="text-sm text-muted">{subtitle}</p>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          <Suspense
            fallback={
              <div
                className="w-full animate-pulse rounded-xl bg-default"
                style={{ height: skeletonHeight }}
              />
            }
          >
            {children}
          </Suspense>
          <div className="flex flex-col items-center gap-2 text-sm">
            <Link href={backHref} className="text-sm">
              {backLabel}
            </Link>
          </div>
        </Card.Content>
      </Card>
    </div>
  );
}
