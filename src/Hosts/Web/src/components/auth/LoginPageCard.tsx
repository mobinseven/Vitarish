"use client";

import { Suspense } from "react";
import { Card, Link } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";

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
  backHref = Routes.Login.Index,
  backLabel,
  children,
}: LoginPageCardProps) {
  const { t } = useLocalizedText();
  const resolvedBackLabel = backLabel ?? t("auth.login.otherMethods");
  return (
    <main className="grid min-h-screen place-items-center px-4">
      <Card className="w-full max-w-md">
        <Card.Header className="flex flex-col items-start gap-1">
          <h1 className="text-2xl font-bold">{title}</h1>
          <p className="text-sm text-default-500">{subtitle}</p>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          <Suspense
            fallback={
              <div
                className="w-full animate-pulse rounded-xl bg-default-200"
                style={{ height: skeletonHeight }}
              />
            }
          >
            {children}
          </Suspense>
          <div className="flex flex-col items-center gap-2 text-sm">
            <Link href={backHref} className="text-sm">
              {resolvedBackLabel}
            </Link>
          </div>
        </Card.Content>
      </Card>
    </main>
  );
}
