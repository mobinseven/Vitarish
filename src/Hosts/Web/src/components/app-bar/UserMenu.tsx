"use client";

import { Avatar, Button, Dropdown, Label } from "@heroui/react";
import { useRouter } from "next/navigation";
import { Routes } from "@/app/routes";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { useLogout } from "@/hooks/auth/useLogout";

export function UserMenu() {
  const { displayProfile } = useAuthSession();
  const { logout, isLoading } = useLogout();
  const router = useRouter();

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
      <Dropdown.Popover>
        <Dropdown.Menu onAction={handleAction}>
          <Dropdown.Item id="account" textValue="Account">
            <Label>Account</Label>
          </Dropdown.Item>
          <Dropdown.Item
            id="logout"
            textValue="Log Out"
            variant="danger"
            isDisabled={isLoading}
          >
            <Label>Log Out</Label>
          </Dropdown.Item>
        </Dropdown.Menu>
      </Dropdown.Popover>
    </Dropdown>
  );
}
