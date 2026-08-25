"use client";

import { ItemCardGroup } from "@heroui-pro/react";
import { ImageActionCard } from "@vitarish/ui";
import { useRouter } from "next/navigation";
import { useLocalizedText, type MessageKey } from "@/localization";
import { Routes } from "@/app/routes";
import { StandaloneCenteredPage } from "@vitarish/ui/layout";

const authMethods = [
  {
    id: "password",
    labelKey: "auth.login.passwordMethod" as MessageKey,
    href: Routes.Login.Password,
    imageSrc: "/sign-in-password.webp",
  },
  {
    id: "phone",
    labelKey: "auth.login.phoneMethod" as MessageKey,
    href: Routes.Login.Phone,
    imageSrc: "/sign-in-phone.webp",
  },
  {
    id: "email",
    labelKey: "auth.login.emailMethod" as MessageKey,
    href: Routes.Login.Email,
    imageSrc: "/sign-in-email.webp",
  },
  {
    id: "passkey",
    labelKey: "auth.login.passkeyMethod" as MessageKey,
    href: Routes.Login.Passkey,
    imageSrc: "/sign-in-passkey.webp",
  },
];

export default function LoginPage() {
  const { t } = useLocalizedText();
  const router = useRouter();

  return (
    <StandaloneCenteredPage width="fluid">
      <div className="mx-auto w-full max-w-md">
        <div className="mb-6 flex flex-col items-start gap-1">
          <h1 className="text-2xl font-bold">{t("auth.login.brandTitle")}</h1>
          <p className="text-sm text-muted">
            {t("auth.login.accountSubtitle")}
          </p>
        </div>
        <ItemCardGroup layout="grid" columns={2} variant="transparent">
          {authMethods.map((method) => (
            <ImageActionCard
              key={method.id}
              className="aspect-square w-full"
              imageSrc={method.imageSrc}
              label={t(method.labelKey)}
              labelPosition="bottom"
              onPress={() => router.push(method.href)}
            />
          ))}
        </ItemCardGroup>
      </div>
    </StandaloneCenteredPage>
  );
}
