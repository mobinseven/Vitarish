"use client";

import { useEffect, useState, type ReactNode } from "react";
import { Button, Tooltip } from "@heroui/react";
import {
  useTenantAuthorization,
  type TenantRole,
} from "../hooks/useTenantAuthorization";
import { useLocalizedText } from "@vitarish/localization";
import { ConfirmDialog, FaIcon, StatusAlert } from "@vitarish/ui";
import { StandaloneCenteredPage } from "@vitarish/ui/layout";
import { ignoreAuthNavigation, type AuthNavigate } from "../hooks/navigation";
import { useAuthErrorDisplay } from "../hooks/useAuthErrorDisplay";
import { useLogout } from "../hooks/useLogout";
import { currentReturnPath, signInHrefReturningTo } from "../routes";

interface AuthorizationGuardProps {
  requiredRole?: TenantRole;
  loginHref: string;
  /** Where the denied screen offers to go; sign-out also returns here. Omit to offer sign-out only. */
  homeHref?: string;
  /**
   * Ask before signing out from the denied screen, because it ends every linked app in this browser. The app's
   * catalog then defines `auth.guard.signOutConfirmTitle` and `auth.guard.signOutConfirmDescription`.
   */
  confirmSignOut?: boolean;
  onNavigate?: AuthNavigate;
  loadingFallback?: ReactNode;
  children: React.ReactNode;
}

export function AuthorizationGuard({
  requiredRole,
  loginHref,
  homeHref,
  confirmSignOut = false,
  onNavigate = ignoreAuthNavigation,
  loadingFallback,
  children,
}: AuthorizationGuardProps) {
  const { isAuthenticated, isLoading, hasAccess } =
    useTenantAuthorization(requiredRole);
  const { t } = useLocalizedText();
  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      // Sign-in brings the person back to the page they opened.
      onNavigate(signInHrefReturningTo(currentReturnPath(), loginHref));
    }
  }, [isLoading, isAuthenticated, loginHref, onNavigate]);

  if (isLoading) {
    if (loadingFallback) {
      return <>{loadingFallback}</>;
    }

    return (
      <StandaloneCenteredPage>
        <p className="text-muted">{t("auth.guard.loading")}</p>
      </StandaloneCenteredPage>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  if (!hasAccess) {
    return (
      <StandaloneCenteredPage>
        <AccessDenied loginHref={loginHref} homeHref={homeHref} confirmSignOut={confirmSignOut} onNavigate={onNavigate} />
      </StandaloneCenteredPage>
    );
  }

  return <>{children}</>;
}

function AccessDenied({ loginHref, homeHref, confirmSignOut, onNavigate }: {
  loginHref: string;
  homeHref?: string;
  confirmSignOut: boolean;
  onNavigate: AuthNavigate;
}) {
  const [confirming, setConfirming] = useState(false);
  const { t } = useLocalizedText();
  const { logout, isLoading, isContextLoading, error, errorMessage } = useLogout(
    undefined,
    loginHref,
    onNavigate,
  );
  const failure = useAuthErrorDisplay(error, errorMessage);

  return (
    <div className="flex flex-col items-center gap-6 text-center">
      <div>
        <h1 className="text-2xl font-bold text-danger">{t("auth.guard.accessDenied")}</h1>
        <p className="mt-2 text-sm text-muted">{t("auth.guard.noPermission")}</p>
      </div>
      {failure ? <StatusAlert message={failure} /> : null}
      <div className="flex flex-wrap items-center justify-center gap-2">
        {homeHref ? (
          <Tooltip>
            <Button variant="secondary" onPress={() => onNavigate(homeHref)}>
              <FaIcon icon="house" aria-hidden="true" />
              {t("auth.guard.home")}
            </Button>
            <Tooltip.Content>{t("auth.guard.home")}</Tooltip.Content>
          </Tooltip>
        ) : null}
        <Tooltip>
          <Button
            variant="tertiary"
            isDisabled={isLoading || isContextLoading}
            isPending={isLoading}
            onPress={() => (confirmSignOut ? setConfirming(true) : void logout(homeHref))}
          >
            <FaIcon icon="arrow-right-from-bracket" aria-hidden="true" className="rtl:-scale-x-100" />
            {t("auth.guard.signOut")}
          </Button>
          <Tooltip.Content>{t("auth.guard.signOut")}</Tooltip.Content>
        </Tooltip>
      </div>
      {confirmSignOut ? (
        <ConfirmDialog
          isOpen={confirming}
          isPending={isLoading}
          title={t("auth.guard.signOutConfirmTitle")}
          message={t("auth.guard.signOutConfirmDescription")}
          confirmLabel={t("auth.guard.signOut")}
          cancelLabel={t("common.actions.cancel")}
          confirmIcon="arrow-right-from-bracket"
          onCancel={() => setConfirming(false)}
          onConfirm={() => void logout(homeHref)}
        />
      ) : null}
    </div>
  );
}
