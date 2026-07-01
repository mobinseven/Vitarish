"use client";

import { Suspense } from "react";
import { Link } from "@heroui/react";
import { CenteredSurface } from "@/design-system";
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
    <CenteredSurface title={title} description={subtitle}>
      <div className="flex flex-col gap-4">
        <Suspense
          fallback={
            <div
              className="w-full animate-pulse rounded-xl bg-[var(--surface-muted)]"
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
      </div>
    </CenteredSurface>
  );
}
