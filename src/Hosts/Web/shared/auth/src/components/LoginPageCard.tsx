"use client";

import { getRequestedReturnUrl } from "../hooks/requestedReturnUrl";
import { AuthenticationSearchParams } from "../routes";
import { LoginPanel } from "@vitarish/ui";
import { useLocalizedText } from "@vitarish/localization";

interface LoginPageCardProps {
  title: string;
  subtitle: string;
  skeletonHeight?: string;
  backHref?: string;
  returnUrlSearchParams?: Pick<URLSearchParams, "get">;
  backLabel?: string;
  children: React.ReactNode;
}

export function LoginPageCard({
  title,
  subtitle,
  skeletonHeight = "128px",
  backHref,
  returnUrlSearchParams,
  backLabel,
  children,
}: LoginPageCardProps) {
  const { t } = useLocalizedText();
  const returnUrl = getRequestedReturnUrl(returnUrlSearchParams);
  const resolvedBackLabel = backLabel ?? t("auth.login.otherMethods");
  return (
    <LoginPanel
      title={title}
      subtitle={subtitle}
      backHref={backHref && returnUrl ? `${backHref}?${AuthenticationSearchParams.ReturnUrl}=${encodeURIComponent(returnUrl)}` : backHref}
      backLabel={resolvedBackLabel}
      skeletonHeight={skeletonHeight}
    >
      {children}
    </LoginPanel>
  );
}
