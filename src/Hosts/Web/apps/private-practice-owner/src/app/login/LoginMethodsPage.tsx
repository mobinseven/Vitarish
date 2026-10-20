"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { ItemCard } from "@heroui-pro/react";
import {
  FaIcon,
  LoginPanel,
  signInMethodClassName,
  signInMethodTileClassName,
  type SignInMethod,
} from "@vitarish/ui";
import { useAuthenticationProfile } from "@vitarish/auth";
import { useLocalizedText, type MessageKey } from "@/localization";
import { Routes } from "@/app/routes";

const authMethods: {
  id: SignInMethod;
  labelKey: MessageKey;
  descriptionKey: MessageKey;
  href: string;
  icon: string;
}[] = [
  {
    id: "password",
    labelKey: "auth.login.passwordMethod",
    descriptionKey: "auth.login.passwordMethodDescription",
    href: Routes.Login.Password,
    icon: "lock-keyhole",
  },
  {
    id: "phone",
    labelKey: "auth.login.phoneMethod",
    descriptionKey: "auth.login.phoneMethodDescription",
    href: Routes.Login.Phone,
    icon: "mobile-screen-button",
  },
  {
    id: "email",
    labelKey: "auth.login.emailMethod",
    descriptionKey: "auth.login.emailMethodDescription",
    href: Routes.Login.Email,
    icon: "envelope",
  },
  {
    id: "passkey",
    labelKey: "auth.login.passkeyMethod",
    descriptionKey: "auth.login.passkeyMethodDescription",
    href: Routes.Login.Passkey,
    icon: "fingerprint",
  },
];

interface LoginMethodsPageProps {
  /** The onboarding host's address, where people create their own clinic; null when not offered. */
  onboardingUrl: string | null;
}

/** The onboarding link, unless this page already is the onboarding host. */
function useCreateClinicHref(onboardingUrl: string | null): string | null {
  const [href, setHref] = useState<string | null>(null);
  useEffect(() => {
    if (!onboardingUrl) return;
    const target = new URL(onboardingUrl);
    if (target.host !== window.location.host) setHref(target.toString());
  }, [onboardingUrl]);
  return href;
}

export function LoginMethodsPage({ onboardingUrl }: LoginMethodsPageProps) {
  const { t } = useLocalizedText();
  const { data: profile } = useAuthenticationProfile();
  const createClinicHref = useCreateClinicHref(onboardingUrl);
  const enabledMethods = authMethods.filter((method) =>
    method.id === "password" ||
    (method.id === "phone" && profile?.allowPasswordlessPhoneLogin === true) ||
    (method.id === "email" && profile?.allowPasswordlessEmailLogin === true) ||
    (method.id === "passkey" && profile?.allowPasskeyLogin === true));

  return (
    <LoginPanel
      title={t("auth.login.brandTitle")}
      subtitle={t("auth.login.accountSubtitle")}
    >
      <div className="flex flex-col gap-3">
        {enabledMethods.map((method) => (
          <ItemCard<"a">
            key={method.id}
            variant="secondary"
            className={`w-full cursor-pointer transition-colors ${signInMethodClassName(method.id)}`}
            render={(props) => <Link href={method.href} {...props} />}
          >
            <ItemCard.Icon
              className={`size-12 shrink-0 rounded-xl ${signInMethodTileClassName}`}
            >
              <FaIcon icon={method.icon} size="xl" aria-hidden="true" />
            </ItemCard.Icon>
            <ItemCard.Content>
              <ItemCard.Title>{t(method.labelKey)}</ItemCard.Title>
              <ItemCard.Description className="max-w-full whitespace-normal">
                {t(method.descriptionKey)}
              </ItemCard.Description>
            </ItemCard.Content>
          </ItemCard>
        ))}
      </div>
      {createClinicHref ? (
        <p className="mt-6 flex flex-wrap items-center justify-center gap-2 text-base text-muted">
          {t("auth.login.noClinicYet")}
          <Link href={createClinicHref} className="inline-flex items-center gap-2 font-medium text-accent hover:underline">
            <FaIcon icon="hospital" aria-hidden="true" />
            {t("auth.login.createClinic")}
          </Link>
        </p>
      ) : null}
    </LoginPanel>
  );
}
