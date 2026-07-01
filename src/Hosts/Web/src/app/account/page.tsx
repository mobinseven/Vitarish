"use client";

import { Card, Link } from "@heroui/react";
import { SurfaceCard } from "@/design-system";
import { useAuthSession } from "@/hooks/auth/useAuthSession";

const settingsLinks = [
  { label: "Name", href: "/account/name", icon: "fa-solid fa-person" },
  { label: "Email", href: "/account/email", icon: "fa-solid fa-envelope" },
  { label: "Phone", href: "/account/phone", icon: "fa-solid fa-mobile" },
  { label: "Password", href: "/account/password", icon: "fa-solid fa-lock" },
  { label: "Passkeys", href: "/account/passkeys", icon: "fa-solid fa-key" },
  { label: "Theme", href: "/account/theme", icon: "fa-solid fa-paint-brush" },
  { label: "Language", href: "/account/culture", icon: "fa-solid fa-globe" },
];

export default function AccountSettingsPage() {
  const { isAuthenticated, displayProfile } = useAuthSession();

  return (
    <div className="max-w-2xl mx-auto">
      <SurfaceCard>
        <Card.Header>
          <h1 className="text-2xl font-bold">Account Settings</h1>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          {isAuthenticated && (
            <div className="text-sm text-muted mb-4">
              Signed in as {displayProfile.displayLabel}
            </div>
          )}
          {settingsLinks.map((link) => (
            <Link key={link.href} href={link.href} className="block">
              <Card className="ds-surface-card ds-surface-card-default ds-card-list-item">
                <div className="flex items-center gap-4">
                  <i className={link.icon} />
                  <span>{link.label}</span>
                </div>
              </Card>
            </Link>
          ))}
        </Card.Content>
      </SurfaceCard>
    </div>
  );
}
