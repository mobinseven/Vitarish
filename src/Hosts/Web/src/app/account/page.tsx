"use client";

import { Card, Link } from "@heroui/react";
import { Routes } from "@/app/routes";
import { SurfaceCard } from "@/design-system";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { useLocalizer } from "@/i18n/client";
import type { MessageKey } from "@/i18n/messages";

const settingsLinks = [
  { labelKey: "Account.Name", href: Routes.Account.Name, icon: "fa-solid fa-person" },
  { labelKey: "Account.Email", href: Routes.Account.Email, icon: "fa-solid fa-envelope" },
  { labelKey: "Account.Phone", href: Routes.Account.Phone, icon: "fa-solid fa-mobile" },
  { labelKey: "Account.Password", href: Routes.Account.Password, icon: "fa-solid fa-lock" },
  { labelKey: "Account.Passkeys", href: Routes.Account.Passkeys, icon: "fa-solid fa-key" },
  { labelKey: "Account.Theme", href: Routes.Account.Theme, icon: "fa-solid fa-paint-brush" },
  { labelKey: "Account.Language", href: Routes.Account.Culture, icon: "fa-solid fa-globe" },
] as const satisfies ReadonlyArray<{
  labelKey: MessageKey;
  href: string;
  icon: string;
}>;

export default function AccountSettingsPage() {
  const { isAuthenticated, displayProfile } = useAuthSession();
  const { t } = useLocalizer();

  return (
    <div className="max-w-2xl mx-auto">
      <SurfaceCard>
        <Card.Header>
          <h1 className="text-2xl font-bold">{t("Account.SettingsTitle")}</h1>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          {isAuthenticated && (
            <div className="text-sm text-muted mb-4">
              {t("Account.SignedInAs", { name: displayProfile.displayLabel })}
            </div>
          )}
          {settingsLinks.map((link) => (
            <Link key={link.href} href={link.href} className="block">
              <Card className="ds-surface-card ds-surface-card-default ds-card-list-item">
                <div className="flex items-center gap-4">
                  <i className={link.icon} />
                  <span>{t(link.labelKey)}</span>
                </div>
              </Card>
            </Link>
          ))}
        </Card.Content>
      </SurfaceCard>
    </div>
  );
}
