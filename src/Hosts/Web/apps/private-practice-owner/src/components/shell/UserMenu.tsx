"use client";

import { useCallback } from "react";
import { Avatar, AvatarFallback, Dropdown, Label } from "@heroui/react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { Routes } from "@/app/routes";
import { useAuthSession } from "@vitarish/auth";
import { useLogout } from "@vitarish/auth";
import { useLocalizedText } from "@/localization";

export function UserMenu() {
  const router = useRouter();
  const navigate = useCallback((href: string) => router.push(href), [router]);
  const { isAuthenticated, isLoading, displayProfile } = useAuthSession();
  const { logout, isLoading: isLoggingOut } = useLogout(
    undefined,
    Routes.Login.Index,
    navigate,
  );
  const { t } = useLocalizedText();

  function handleAction(key: React.Key) {
    if (key === "account") {
      router.push(Routes.Account.Index);
    } else if (key === "logout") {
      logout();
    }
  }

  if (isLoading) {
    return <div className="h-9 w-9 animate-pulse rounded-full bg-current/10" />;
  }

  if (!isAuthenticated) {
    return (
      <Link
        href={Routes.Login.Index}
        className="text-sm font-medium text-foreground no-underline"
      >
        {t("appShell.userMenu.logIn")}
      </Link>
    );
  }

  return (
    <Dropdown>
      <Dropdown.Trigger aria-label={t("appShell.userMenu.account")}>
        <span className="flex min-w-0 items-center gap-2 px-2">
          <Avatar size="sm" className="h-7 w-7 text-xs">
            <AvatarFallback>{displayProfile.initials ?? ""}</AvatarFallback>
          </Avatar>
          <span className="hidden text-sm font-medium sm:inline">
            {displayProfile.displayLabel}
          </span>
        </span>
      </Dropdown.Trigger>
      <Dropdown.Popover>
        <Dropdown.Menu onAction={handleAction}>
          <Dropdown.Item
            id="account"
            textValue={t("appShell.userMenu.account")}
          >
            <Label>{t("appShell.userMenu.account")}</Label>
          </Dropdown.Item>
          <Dropdown.Item
            id="logout"
            textValue={t("appShell.userMenu.signOut")}
            variant="danger"
            isDisabled={isLoggingOut}
          >
            <Label>{t("appShell.userMenu.signOut")}</Label>
          </Dropdown.Item>
        </Dropdown.Menu>
      </Dropdown.Popover>
    </Dropdown>
  );
}
