"use client";

import { useEffect } from "react";
import { Alert, Button, Tooltip } from "@heroui/react";
import { AccountSignInSettings, useAccountPasswordChange, useAccountSignInSecurityActions } from "@vitarish/account";
import { useAuthSession } from "@vitarish/auth";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon, HeaderIcon, SettingsBackLink, SettingsSurface } from "@vitarish/ui";
import { LandingRoutes } from "../../app/routes";
import styles from "./account.module.css";
import { EmailChangeSection } from "./EmailChangeSection";
import { PhoneChangeSection } from "./PhoneChangeSection";

/**
 * The signed-in person's account area on the platform home: how they sign in (password and two-step
 * sign-in), and their email address and phone number. It works for a person with no workspace yet, who
 * has no clinic app to manage these in.
 */
export function AccountPage({ signInHref }: { signInHref: string }) {
  const { t } = useLocalizedText();
  const session = useAuthSession();
  const password = useAccountPasswordChange();
  const signInSecurity = useAccountSignInSecurityActions();

  useEffect(() => {
    if (!session.isLoading && !session.isError && !session.isAuthenticated) window.location.assign(signInHref);
  }, [session.isAuthenticated, session.isError, session.isLoading, signInHref]);

  if (session.isError) {
    return (
      <main className="flex min-h-dvh items-center justify-center px-4">
        <Alert className="w-full max-w-xl" status="danger">
          <Alert.Indicator><FaIcon icon="triangle-exclamation" aria-hidden="true" /></Alert.Indicator>
          <Alert.Content>
            <Alert.Title>{t("home.sessionUnavailable")}</Alert.Title>
            <Alert.Description>
              <Tooltip>
                <Button variant="secondary" onPress={() => void session.refetch()}>
                  <FaIcon icon="rotate-right" aria-hidden="true" />
                  {t("home.retrySession")}
                </Button>
                <Tooltip.Content>{t("home.retrySession")}</Tooltip.Content>
              </Tooltip>
            </Alert.Description>
          </Alert.Content>
        </Alert>
      </main>
    );
  }

  if (session.isLoading || !session.isAuthenticated || !session.user) {
    return (
      <main className="flex min-h-dvh items-center justify-center text-base text-muted" aria-busy="true">
        {t("common.states.loading")}
      </main>
    );
  }

  return (
    <main className={`px-4 py-6 ${styles.visibleFieldBorders}`}>
      <SettingsSurface width="wide">
        <header className="flex items-center gap-3">
          <HeaderIcon icon="user-gear" level="page" />
          <div className="flex min-w-0 flex-col gap-1">
            <h1 className="text-2xl font-semibold text-foreground sm:text-3xl">{t("account.page.title")}</h1>
            <p className="text-base text-muted">{t("account.page.description")}</p>
          </div>
        </header>
        <AccountSignInSettings
          changePassword={password.changePassword}
          setFirstPassword={signInSecurity.setFirstPassword}
          setTwoFactorEnabled={signInSecurity.setTwoFactorEnabled}
        />
        <EmailChangeSection userId={session.user.userId} />
        <PhoneChangeSection userId={session.user.userId} />
        <SettingsBackLink href={LandingRoutes.PlatformHome} label={t("account.page.backToHome")} />
      </SettingsSurface>
    </main>
  );
}
