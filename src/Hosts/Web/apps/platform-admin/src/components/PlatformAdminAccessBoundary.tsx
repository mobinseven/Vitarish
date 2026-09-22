"use client";

import { useEffect, useRef } from "react";
import { usePathname } from "next/navigation";
import { recordPlatformAuditAccessAttempt } from "@vitarish/security";
import { useAuthSession, usePlatformAuthorization } from "@vitarish/auth";
import { CenteredRegion, LoadingSpinner } from "@vitarish/ui";
import { useLocalizedText } from "@vitarish/localization";
import { ItemCard } from "@heroui-pro/react";
import { FaIcon } from "@vitarish/ui";
import Link from "next/link";
import { Routes } from "../app/routes";

export function PlatformAdminAccessBoundary({
  children,
}: {
  children: React.ReactNode;
}) {
  const pathname = usePathname();
  const { isAuthenticated, isLoading } = useAuthSession();
  const { hasAccess } = usePlatformAuthorization();
  const { t } = useLocalizedText();
  const recordedAttempt = useRef(false);

  useEffect(() => {
    if (isAuthenticated && !hasAccess && !recordedAttempt.current) {
      recordedAttempt.current = true;
      void recordPlatformAuditAccessAttempt();
    }
  }, [hasAccess, isAuthenticated]);

  if (pathname.startsWith(Routes.Login)) {
    return <>{children}</>;
  }

  if (isLoading) {
    return (
      <CenteredRegion>
        <div role="status" aria-label={t("common.states.loading")}>
          <LoadingSpinner />
        </div>
      </CenteredRegion>
    );
  }

  if (isAuthenticated && !hasAccess) {
    return (
      <CenteredRegion>
        <main role="main" className="text-center">
          <p className="text-6xl font-semibold tracking-tight text-danger">
            {t("admin.forbiddenCode")}
          </p>
          <h1 className="mt-4 text-2xl font-semibold text-danger">
            {t("admin.forbiddenTitle")}
          </h1>
          <p className="mt-2 text-muted">{t("admin.forbiddenDescription")}</p>
          <nav
            aria-label={t("admin.recoveryNavigation")}
            className="mx-auto mt-8 flex max-w-2xl flex-wrap justify-center gap-4 text-start"
          >
            {(
              [
                {
                  href: Routes.Root,
                  icon: "house",
                  iconClassName: "bg-accent/10 text-accent",
                  title: "homeTitle",
                  description: "homeDescription",
                },
                {
                  href: Routes.Logout,
                  icon: "arrow-right-from-bracket",
                  iconClassName: "bg-danger/10 text-danger",
                  title: "logoutTitle",
                  description: "logoutDescription",
                },
              ] as const
            ).map((item) => (
              <ItemCard<"a">
                key={item.href}
                className="w-full max-w-[18rem] flex-1 basis-[14rem]"
                render={(props) => <Link {...props} href={item.href} />}
              >
                <ItemCard.Icon
                  className={`size-12 rounded-2xl ${item.iconClassName}`}
                >
                  <FaIcon
                    icon={item.icon}
                    aria-hidden="true"
                    className="text-xl"
                  />
                </ItemCard.Icon>
                <ItemCard.Content className="gap-1">
                  <ItemCard.Title>{t(`admin.${item.title}`)}</ItemCard.Title>
                  <ItemCard.Description>
                    {t(`admin.${item.description}`)}
                  </ItemCard.Description>
                </ItemCard.Content>
                <ItemCard.Action>
                  <FaIcon
                    icon="chevron-right"
                    aria-hidden="true"
                    className="size-4 text-muted rtl:rotate-180"
                  />
                </ItemCard.Action>
              </ItemCard>
            ))}
          </nav>
        </main>
      </CenteredRegion>
    );
  }

  return <>{children}</>;
}
