"use client";

import { type ReactNode } from "react";
import { useRouter } from "next/navigation";
import { useAuthSession } from "@vitarish/auth-web";
import { useLocalizedText } from "@/localization";
import { PlatformAdminShell } from "@/components/shell/PlatformAdminShell";
import { Routes } from "@/app/routes";
import { useEffect } from "react";

interface PlatformAdminGuardProps {
  children: ReactNode;
}

export function PlatformAdminGuard({ children }: PlatformAdminGuardProps) {
  const router = useRouter();
  const { session, isAuthenticated, isLoading, isError, user, displayProfile } = useAuthSession();
  const { t } = useLocalizedText();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace(Routes.Login.Index);
    }
  }, [isLoading, isAuthenticated, router]);

  if (isLoading) {
    return (
      <main className="grid min-h-screen place-items-center">
        <p className="text-muted">{t("common.states.loading")}</p>
      </main>
    );
  }

  if (isError) {
    return (
      <main className="grid min-h-screen place-items-center">
        <div className="text-center">
          <h1 className="text-2xl font-bold">{t("platformAdmin.auth.sessionError.title")}</h1>
          <p className="text-sm text-muted mt-2">{t("platformAdmin.auth.sessionError.description")}</p>
        </div>
      </main>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  const canAccessPlatformAdmin = session?.platformAccess?.canAccessPlatformAdmin === true;

  if (!canAccessPlatformAdmin) {
    return (
      <main className="grid min-h-screen place-items-center">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-danger">{t("platformAdmin.auth.forbidden.title")}</h1>
          <p className="text-sm text-muted mt-2">{t("platformAdmin.auth.forbidden.description")}</p>
          <button
            type="button"
            onClick={() => router.replace(Routes.Login.Index)}
            className="mt-4 text-sm text-accent hover:underline"
          >
            {t("auth.login.logoutAction")}
          </button>
        </div>
      </main>
    );
  }

  return (
    <PlatformAdminShell user={user} displayProfile={displayProfile}>
      {children}
    </PlatformAdminShell>
  );
}
