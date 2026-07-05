"use client";

import { Card, Link } from "@heroui/react";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { useLocalizedText, type MessageKey } from "@/localization";

const settingsLinks = [
  { labelKey: "account.navigation.name" as MessageKey, href: "/account/name", icon: "fa-solid fa-person" },
  { labelKey: "account.navigation.email" as MessageKey, href: "/account/email", icon: "fa-solid fa-envelope" },
  { labelKey: "account.navigation.phone" as MessageKey, href: "/account/phone", icon: "fa-solid fa-mobile" },
  { labelKey: "account.navigation.password" as MessageKey, href: "/account/password", icon: "fa-solid fa-lock" },
  { labelKey: "account.navigation.passkeys" as MessageKey, href: "/account/passkeys", icon: "fa-solid fa-key" },
  { labelKey: "account.navigation.theme" as MessageKey, href: "/account/theme", icon: "fa-solid fa-paint-brush" },
  { labelKey: "account.navigation.language" as MessageKey, href: "/account/culture", icon: "fa-solid fa-globe" },
];

export default function AccountSettingsPage() {
  const { isAuthenticated, displayProfile } = useAuthSession();
  const { t } = useLocalizedText();

  return (
    <div className="max-w-2xl mx-auto">
      <Card>
        <Card.Header>
          <h1 className="text-2xl font-bold">{t("account.settings")}</h1>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          {isAuthenticated && (
            <div className="text-sm text-muted mb-4">
              {t("account.signedInAs", { name: displayProfile.displayLabel })}
            </div>
          )}
          {settingsLinks.map((link) => (
            <Link key={link.href} href={link.href} className="block">
              <Card>
                <div className="flex items-center gap-4 p-3">
                  <i className={link.icon} />
                  <span>{t(link.labelKey)}</span>
                </div>
              </Card>
            </Link>
          ))}
        </Card.Content>
      </Card>
    </div>
  );
}
