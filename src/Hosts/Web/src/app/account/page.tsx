"use client";

import Link from "next/link";
import { ItemCard, ItemCardGroup } from "@heroui-pro/react";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { Routes } from "@/app/routes";
import { AccountSurface } from "@/components/account/AccountSurface";
import { useLocalizedText, type MessageKey } from "@/localization";

const settingsLinks = [
  { labelKey: "account.navigation.name" as MessageKey, href: Routes.Account.Name, icon: "fa-solid fa-person" },
  { labelKey: "account.navigation.email" as MessageKey, href: Routes.Account.Email, icon: "fa-solid fa-envelope" },
  { labelKey: "account.navigation.phone" as MessageKey, href: Routes.Account.Phone, icon: "fa-solid fa-mobile" },
  { labelKey: "account.navigation.password" as MessageKey, href: Routes.Account.Password, icon: "fa-solid fa-lock" },
  { labelKey: "account.navigation.passkeys" as MessageKey, href: Routes.Account.Passkeys, icon: "fa-solid fa-key" },
  { labelKey: "account.navigation.theme" as MessageKey, href: Routes.Account.Theme, icon: "fa-solid fa-paint-brush" },
  { labelKey: "account.navigation.language" as MessageKey, href: Routes.Account.Culture, icon: "fa-solid fa-globe" },
];

export default function AccountSettingsPage() {
  const { isAuthenticated, displayProfile } = useAuthSession();
  const { t } = useLocalizedText();

  return (
    <AccountSurface width="wide">
      <ItemCardGroup layout="grid" columns={2}>
        <ItemCardGroup.Header>
          <ItemCardGroup.Title>{t("account.settings")}</ItemCardGroup.Title>
          {isAuthenticated && (
            <ItemCardGroup.Description>
              {t("account.signedInAs", { name: displayProfile.displayLabel })}
            </ItemCardGroup.Description>
          )}
        </ItemCardGroup.Header>
        {settingsLinks.map((item) => (
          <Link key={item.href} href={item.href} className="block h-full min-w-0">
            <ItemCard className="h-full min-w-0 transition-colors hover:bg-surface-secondary" variant="outline">
              <ItemCard.Icon>
                <i aria-hidden className={`${item.icon} text-lg text-accent`} />
              </ItemCard.Icon>
              <ItemCard.Content>
                <ItemCard.Title className="whitespace-normal break-words">
                  {t(item.labelKey)}
                </ItemCard.Title>
              </ItemCard.Content>
            </ItemCard>
          </Link>
        ))}
      </ItemCardGroup>
    </AccountSurface>
  );
}
