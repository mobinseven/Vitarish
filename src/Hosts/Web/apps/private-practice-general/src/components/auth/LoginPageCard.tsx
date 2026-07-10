"use client";

import { LoginPanel } from "@vitarish/web-ui";
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
    <LoginPanel
      title={title}
      subtitle={subtitle}
      backHref={backHref}
      backLabel={resolvedBackLabel}
      skeletonHeight={skeletonHeight}
    >
      {children}
    </LoginPanel>
  );
}
