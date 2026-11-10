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
import { useLocalizedText } from "@vitarish/localization";
import { useAuthenticationProfileGet } from "../api/generated/authentication/authentication";
import { isCentralContinuationReturnTo, isSafeRelativeReturnTo, signInReturnUrlQueryKey } from "../centralHost";
import { AuthenticationWebRoutes } from "../routes";

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
    href: AuthenticationWebRoutes.SignInPassword,
    icon: "lock-keyhole",
  },
  {
    id: "phone",
    labelKey: "auth.login.phoneMethod",
    descriptionKey: "auth.login.phoneMethodDescription",
    href: AuthenticationWebRoutes.SignInPhone,
    icon: "mobile-screen-button",
  },
  {
    id: "email",
    labelKey: "auth.login.emailMethod",
    descriptionKey: "auth.login.emailMethodDescription",
    href: AuthenticationWebRoutes.SignInEmail,
    icon: "envelope",
  },
  {
    id: "passkey",
    labelKey: "auth.login.passkeyMethod",
    descriptionKey: "auth.login.passkeyMethodDescription",
    href: AuthenticationWebRoutes.SignInPasskey,
    icon: "fingerprint",
  },
];

/**
 * The sign-in methods the platform offers, shown the same way whichever app hosts the sign-in
 * page. Phone and email sign-in create the account on first use, so it says so rather than
 * pointing at any one app's onboarding.
 */
export function SignInMethodsPage({ searchParams }: { searchParams: Pick<URLSearchParams, "get"> }) {
  const { t } = useLocalizedText();
  const { data: profile } = useAuthenticationProfileGet();
  const requestedReturnUrl = searchParams.get(signInReturnUrlQueryKey);
  // Another app's sign-in arrives with the host's own continuation link, which carries a query.
  const safeReturnUrl = isSafeRelativeReturnTo(requestedReturnUrl) || isCentralContinuationReturnTo(requestedReturnUrl)
    ? requestedReturnUrl
    : undefined;
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
            render={(props) => (
              <Link
                href={safeReturnUrl
                  ? `${method.href}?${new URLSearchParams({
                    [signInReturnUrlQueryKey]: safeReturnUrl,
                  })}`
                  : method.href}
                {...props}
              />
            )}
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
      <p className="mt-6 text-center text-base text-muted">
        {t("auth.signInMethods.accountCreationNotice")}
      </p>
    </LoginPanel>
  );
}
