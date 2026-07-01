"use client";

import { Avatar, AvatarFallback, Button, Dropdown, Label } from "@heroui/react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { Routes } from "@/app/routes";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { useLogout } from "@/hooks/auth/useLogout";
import { useLocalizedText } from "@/localization";

export function UserMenu() {
  const router = useRouter();
  const { isAuthenticated, isLoading, displayProfile } = useAuthSession();
  const { logout, isLoading: isLoggingOut } = useLogout();
  const { t } = useLocalizedText();

  function handleAction(key: React.Key) {
    if (key === "account") {
      router.push(Routes.Account.Index);
    } else if (key === "logout") {
      logout();
    }
  }

  if (isLoading) {
    return <div className="h-8 w-8 animate-pulse rounded-full bg-current/10" />;
  }

  if (!isAuthenticated) {
    return (
      <Link href={Routes.Login.Index} className="text-sm font-medium text-foreground">
        {t("appShell.userMenu.logIn")}
      </Link>
    );
  }

  return (
    <Dropdown>
      <Dropdown.Trigger>
        <Button variant="tertiary" className="min-w-0 gap-2 rounded-full px-2 py-1">
          <Avatar size="sm" className="h-7 w-7 text-xs">
            <AvatarFallback>{displayProfile.initials ?? ""}</AvatarFallback>
          </Avatar>
          <span className="hidden text-sm font-medium sm:inline">
            {displayProfile.displayLabel}
          </span>
        </Button>
      </Dropdown.Trigger>
      <Dropdown.Popover>
        <Dropdown.Menu onAction={handleAction}>
          <Dropdown.Item id="account" textValue={t("appShell.userMenu.account")}>
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
