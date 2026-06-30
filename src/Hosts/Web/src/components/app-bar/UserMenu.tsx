"use client";

import { Avatar, Button, Dropdown, Link } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { useLogout } from "@/hooks/auth/useLogout";

export function UserMenu() {
  const { displayProfile } = useAuthSession();
  const { logout, isLoading } = useLogout();

  return (
    <Dropdown>
      <Dropdown.Trigger>
        <Button
          variant="tertiary"
          className="min-w-0 gap-2 rounded-full px-2 py-1"
        >
          <Avatar
            name={displayProfile.initials ?? undefined}
            size="sm"
            className="h-7 w-7 text-xs"
          />
          <span className="hidden text-sm font-medium sm:inline">
            {displayProfile.displayLabel}
          </span>
        </Button>
      </Dropdown.Trigger>
      <Dropdown.Content>
        <Dropdown.Item id="account">
          <Link href={Routes.Account.Index} className="block w-full">
            Account
          </Link>
        </Dropdown.Item>
        <Dropdown.Item id="logout">
          <Button
            variant="tertiary"
            onPress={() => logout()}
            isDisabled={isLoading}
            className="min-w-0 justify-start px-2 py-1 text-sm"
          >
            Log Out
          </Button>
        </Dropdown.Item>
      </Dropdown.Content>
    </Dropdown>
  );
}
