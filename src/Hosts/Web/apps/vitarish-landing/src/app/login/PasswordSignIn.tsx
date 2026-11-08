"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { LoginForm, LoginPageCard } from "@vitarish/auth";
import { useLocalizedText } from "@vitarish/localization";

export function PasswordSignIn() {
  const { t } = useLocalizedText();
  const router = useRouter();
  const search = useSearchParams();
  return <LoginPageCard title={t("auth.login.brandTitle")} subtitle={t("auth.login.accountSubtitle")}>
    <LoginForm navigate={href => router.push(href)} returnUrlSearchParams={search} />
  </LoginPageCard>;
}
