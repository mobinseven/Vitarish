"use client";

import { AuthMethodList, type AuthMethodListItem } from "@vitarish/web-ui";
import { useLocalizedText, type MessageKey } from "@/localization";

const methods = [
  {
    id: "password",
    labelKey: "auth.login.passwordMethod" as MessageKey,
    href: "/login/password",
    iconClassName: "fa-solid fa-lock",
  },
  {
    id: "phone",
    labelKey: "auth.login.phoneMethod" as MessageKey,
    href: "/login/phone",
    iconClassName: "fa-solid fa-mobile-screen-button",
  },
  {
    id: "email",
    labelKey: "auth.login.emailMethod" as MessageKey,
    href: "/login/email",
    iconClassName: "fa-solid fa-envelope",
  },
  {
    id: "passkey",
    labelKey: "auth.login.passkeyMethod" as MessageKey,
    href: "/login/passkey",
    iconClassName: "fa-solid fa-key",
  },
];

export function AuthMethodSelector() {
  const { t } = useLocalizedText();
  const localizedMethods: AuthMethodListItem[] = methods.map((method) => ({
    id: method.id,
    href: method.href,
    label: t(method.labelKey),
    icon: <i aria-hidden="true" className={`${method.iconClassName} size-5`} />,
  }));

  return <AuthMethodList methods={localizedMethods} />;
}
