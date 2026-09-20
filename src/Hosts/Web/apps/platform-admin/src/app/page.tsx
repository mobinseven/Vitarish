"use client";

import { useAuthSession, usePlatformAuthorization } from "@vitarish/auth";
import { CenteredRegion, FaIcon } from "@vitarish/ui";
import { ItemCard } from "@heroui-pro/react";
import { useLocalizedText } from "@vitarish/localization";
import Link from "next/link";
import { Routes } from "./routes";

const administrationSections = [
  { href: Routes.Users.Index, icon: "users", title: "usersTitle", caption: "usersCaption" },
  { href: Routes.Owners.Index, icon: "user-tie", title: "ownersTitle", caption: "ownersCaption" },
  { href: Routes.Tenants.Index, icon: "building", title: "tenantsTitle", caption: "tenantsCaption" },
  { href: Routes.Settings.Index, icon: "sliders", title: "settingsTitle", caption: "settingsCaption" },
  { href: Routes.Audit, icon: "shield-halved", title: "auditTitle", caption: "auditCaption" },
] as const;

export default function RootPage() {
  const { isAuthenticated, isLoading } = useAuthSession();
  const { hasAccess } = usePlatformAuthorization();
  const { t } = useLocalizedText();

  if (isLoading) return <CenteredRegion><p>{t("common.states.loading")}</p></CenteredRegion>;
  if (!isAuthenticated) return <CenteredRegion><h1 className="text-2xl font-semibold">{t("admin.title")}</h1><p className="mt-2 text-muted">{t("admin.signInRequired")}</p></CenteredRegion>;
  if (!hasAccess) return <CenteredRegion><h1 className="text-2xl font-semibold text-danger">{t("admin.forbiddenTitle")}</h1><p className="mt-2 text-muted">{t("admin.forbiddenDescription")}</p></CenteredRegion>;
  return (
    <CenteredRegion className="px-6 py-12">
      <div className="w-full max-w-5xl text-center">
        <div className="mx-auto max-w-2xl">
          <p className="mb-3 text-sm font-medium text-primary">{t("admin.title")}</p>
          <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">{t("admin.foundationTitle")}</h1>
          <p className="mt-4 text-base leading-7 text-muted">{t("admin.foundationDescription")}</p>
        </div>
        <nav aria-label={t("admin.title")} className="mx-auto mt-10 flex max-w-4xl flex-wrap justify-center gap-4 text-start">
          {administrationSections.map((section) => (
            <ItemCard<"a">
              key={section.href}
              className="w-full max-w-[22rem] flex-1 basis-[18rem] transition-transform duration-200 hover:-translate-y-0.5"
              render={(props) => <Link {...props} href={section.href} />}
            >
              <ItemCard.Icon className="size-12 rounded-2xl bg-accent/10 text-accent">
                <FaIcon icon={section.icon} aria-hidden="true" className="text-xl" />
              </ItemCard.Icon>
              <ItemCard.Content className="gap-1">
                <ItemCard.Title>{t(`admin.${section.title}`)}</ItemCard.Title>
                <ItemCard.Description>{t(`admin.${section.caption}`)}</ItemCard.Description>
              </ItemCard.Content>
              <ItemCard.Action>
                <FaIcon icon="chevron-right" aria-hidden="true" className="size-4 text-muted rtl:rotate-180" />
              </ItemCard.Action>
            </ItemCard>
          ))}
        </nav>
      </div>
    </CenteredRegion>
  );
}
