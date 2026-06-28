"use client";

import { Card, CardBody, CardHeader, Link } from "@heroui/react";
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
  const { user } = useAuthSession();

  return (
    <div className="max-w-2xl mx-auto">
      <Card>
        <CardHeader>
          <h1 className="text-2xl font-bold">Account Settings</h1>
        </CardHeader>
        <CardBody className="flex flex-col gap-4">
          {user && (
            <div className="text-sm text-default-500 mb-4">
              Signed in as {user.displayName ?? user.email ?? user.phoneNumber}
            </div>
          )}
          {settingsLinks.map((link) => (
            <Link key={link.href} href={link.href} className="block">
              <Card className="p-4 hover:bg-default-100 cursor-pointer">
                <div className="flex items-center gap-3">
                  <i className={link.icon} />
                  <span>{link.label}</span>
                </div>
              </Card>
            </Link>
          ))}
        </CardBody>
      </Card>
    </div>
  );
}
