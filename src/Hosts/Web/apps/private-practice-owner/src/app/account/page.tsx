"use client";

import Link from "next/link";
import { ItemCard, ItemCardGroup } from "@heroui-pro/react";
import { useAuthSession } from "@vitarish/auth";
import { FaIcon } from "@vitarish/ui";
import { Routes } from "@/app/routes";
import { AccountSurface } from "@/components/account/AccountSurface";
import { useLocalizedText, type MessageKey } from "@/localization";

const settingsLinks = [
  {
    labelKey: "account.navigation.name" as MessageKey,
    href: Routes.Account.Name,
    icon: "person",
  },
  {
    labelKey: "account.navigation.email" as MessageKey,
    href: Routes.Account.Email,
    icon: "envelope",
  },
  {
    labelKey: "account.navigation.phone" as MessageKey,
    href: Routes.Account.Phone,
    icon: "mobile",
  },
  {
    labelKey: "account.navigation.password" as MessageKey,
    href: Routes.Account.Password,
    icon: "lock",
  },
  {
    labelKey: "account.navigation.passkeys" as MessageKey,
    href: Routes.Account.Passkeys,
    icon: "key",
  },
  {
    labelKey: "account.navigation.language" as MessageKey,
    href: Routes.Account.Culture,
    icon: "globe",
  },
  {
    labelKey: "account.navigation.security" as MessageKey,
    href: Routes.Account.Security,
    icon: "shield",
  },
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
          <Link
            key={item.href}
            href={item.href}
            className="block h-full min-w-0"
          >
            <ItemCard
              className="h-full min-w-0 transition-colors hover:bg-surface-secondary"
              variant="outline"
            >
              <ItemCard.Icon>
                <FaIcon
                  icon={item.icon}
                  aria-hidden="true"
                  className="text-lg text-accent"
                />
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
