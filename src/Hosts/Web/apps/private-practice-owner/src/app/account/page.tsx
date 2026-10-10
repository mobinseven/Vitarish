"use client";

import Link from "next/link";
import { Surface } from "@heroui/react";
import { ItemCard } from "@heroui-pro/react";
import { useAuthSession } from "@vitarish/auth";
import { FaIcon, HeaderIcon } from "@vitarish/ui";
import { Routes } from "@/app/routes";
import { AccountSurface } from "@/components/account/AccountSurface";
import { useLocalizedText, type MessageKey } from "@/localization";

const settingsLinks = [
  {
    labelKey: "account.navigation.name" as MessageKey,
    captionKey: "account.captions.name" as MessageKey,
    href: Routes.Account.Name,
    icon: "person",
  },
  {
    labelKey: "account.navigation.email" as MessageKey,
    captionKey: "account.captions.email" as MessageKey,
    href: Routes.Account.Email,
    icon: "envelope",
  },
  {
    labelKey: "account.navigation.phone" as MessageKey,
    captionKey: "account.captions.phone" as MessageKey,
    href: Routes.Account.Phone,
    icon: "mobile",
  },
  {
    labelKey: "account.navigation.password" as MessageKey,
    captionKey: "account.captions.password" as MessageKey,
    href: Routes.Account.Password,
    icon: "lock",
  },
  {
    labelKey: "account.navigation.passkeys" as MessageKey,
    captionKey: "account.captions.passkeys" as MessageKey,
    href: Routes.Account.Passkeys,
    icon: "key",
  },
  {
    labelKey: "account.navigation.language" as MessageKey,
    captionKey: "account.captions.language" as MessageKey,
    href: Routes.Account.Culture,
    icon: "globe",
  },
  {
    labelKey: "account.navigation.security" as MessageKey,
    captionKey: "account.captions.security" as MessageKey,
    href: Routes.Account.Security,
    icon: "shield",
  },
];

export default function AccountSettingsPage() {
  const { isAuthenticated, displayProfile } = useAuthSession();
  const { t } = useLocalizedText();

  return (
    <AccountSurface width="wide">
      <Surface className="flex flex-col gap-6 rounded-3xl p-6">
        <div>
          <div className="flex items-center gap-3">
            <HeaderIcon context="general" icon="user-gear" />
            <div className="min-w-0">
              <h2 className="text-lg font-semibold text-foreground">
                {t("account.settings")}
              </h2>
            </div>
          </div>
          {isAuthenticated && (
            <p className="text-sm text-muted">
              {t("account.signedInAs", { name: displayProfile.displayLabel })}
            </p>
          )}
        </div>
        <div className="grid gap-3 sm:grid-cols-2">
          {settingsLinks.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              className="block h-full min-w-0"
            >
              <ItemCard
                className="h-full min-w-0 transition-colors hover:bg-surface-tertiary"
                variant="secondary"
              >
                <ItemCard.Icon>
                  <FaIcon
                    icon={item.icon}
                    aria-hidden="true"
                    className="text-lg text-muted"
                  />
                </ItemCard.Icon>
                <ItemCard.Content className="gap-1">
                  <ItemCard.Title className="whitespace-normal break-words">
                    {t(item.labelKey)}
                  </ItemCard.Title>
                  <ItemCard.Description className="whitespace-normal break-words">
                    {t(item.captionKey)}
                  </ItemCard.Description>
                </ItemCard.Content>
              </ItemCard>
            </Link>
          ))}
        </div>
      </Surface>
    </AccountSurface>
  );
}
