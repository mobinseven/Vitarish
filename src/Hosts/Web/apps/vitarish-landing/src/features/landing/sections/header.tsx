"use client";

import { Avatar, Button } from "@heroui/react";
import { Navbar } from "@heroui-pro/react";

interface HeaderProps {
  fa: boolean;
  spotlight: string;
  apps: string;
  purpose: string;
  onLocaleChange: () => void;
}

export function Header({
  fa,
  spotlight,
  apps,
  purpose,
  onLocaleChange,
}: HeaderProps) {
  return (
    <Navbar position="floating">
      <Navbar.Header>
        <Navbar.Brand>
          <Avatar>
            <Avatar.Image src="/transparent.webp" alt="Vitarish" />
          </Avatar>
        </Navbar.Brand>

        <Navbar.Spacer />

        <Navbar.Content>
          <Navbar.Item href="#spotlight">{spotlight}</Navbar.Item>
          <Navbar.Item href="#apps">{apps}</Navbar.Item>
          <Navbar.Item href="#purpose">{purpose}</Navbar.Item>
          <Button
            size="sm"
            variant="tertiary"
            onPress={onLocaleChange}
            aria-label={fa ? "Switch to English" : "تغییر به فارسی"}
          >
            {fa ? "EN" : "فا"}
          </Button>
        </Navbar.Content>
      </Navbar.Header>
    </Navbar>
  );
}
