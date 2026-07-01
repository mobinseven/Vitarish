"use client";

import { Avatar, AvatarFallback, Button, Dropdown, Label } from "@heroui/react";
import { useRouter } from "next/navigation";
import { Routes } from "@/app/routes";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { useLogout } from "@/hooks/auth/useLogout";
import { useLocalizedText } from "@/localization";

export function UserMenu() {
  const { displayProfile } = useAuthSession();
  const { logout, isLoading } = useLogout();
  const router = useRouter();
  const { t } = useLocalizedText();

  function handleAction(key: React.Key) {
    if (key === "account") {
      router.push(Routes.Account.Index);
    } else if (key === "logout") {
      logout();
    }
  }

  return (
    <Dropdown>
      <Dropdown.Trigger>
        <Button
          variant="tertiary"
          className="min-w-0 gap-2 rounded-full px-2 py-1"
        >
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
            isDisabled={isLoading}
          >
            <Label>{t("appShell.userMenu.signOut")}</Label>
          </Dropdown.Item>
        </Dropdown.Menu>
      </Dropdown.Popover>
    </Dropdown>
  );
}
