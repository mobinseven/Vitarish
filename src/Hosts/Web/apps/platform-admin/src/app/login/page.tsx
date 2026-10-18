"use client";

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
import { useLocalizedText } from "@vitarish/localization";
import { Routes } from "@/app/routes";

// The platform-admin sign-in methods, matching the owner app's method picker.
const authMethods: {
  id: SignInMethod;
  labelKey: string;
  descriptionKey: string;
  href: string;
  icon: string;
}[] = [
  {
    id: "password",
    labelKey: "auth.login.passwordMethod",
    descriptionKey: "auth.login.passwordMethodDescription",
    href: Routes.LoginPassword,
    icon: "lock-keyhole",
  },
  {
    id: "email",
    labelKey: "auth.login.emailMethod",
    descriptionKey: "auth.login.emailMethodDescription",
    href: Routes.LoginEmail,
    icon: "envelope",
  },
];

export default function PlatformLoginPage() {
  const { t } = useLocalizedText();
  const { data: profile } = useAuthenticationProfile();
  const enabledMethods = authMethods.filter((method) =>
    method.id === "password" ||
    (method.id === "email" && profile?.allowPasswordlessEmailLogin === true));

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
    </LoginPanel>
  );
}
