"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { Alert, Button, Tooltip } from "@heroui/react";
import { AuthorizationGuard, useAuthErrorDisplay, useLogout } from "@vitarish/auth";
import { ContentStudio } from "@vitarish/contents/studio";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import { PageContainer, StandaloneDocumentPage } from "@vitarish/ui/layout";
import { LandingRoutes } from "../routes";

/** The studio's way out: back to the platform home, or sign out to the front door. */
function StudioHeaderActions({ navigate }: { navigate: (href: string) => void }) {
  const { t } = useLocalizedText();
  const { logout, isLoading, isContextLoading, error, errorMessage } = useLogout(undefined, LandingRoutes.Home, navigate);
  const failure = useAuthErrorDisplay(error, errorMessage);
  return <>
    <Tooltip>
      <Button variant="tertiary" onPress={() => navigate(LandingRoutes.PlatformHome)}>
        <FaIcon icon="house" aria-hidden="true" />
        <span className="max-sm:sr-only">{t("home.backToHome")}</span>
      </Button>
      <Tooltip.Content>{t("home.backToHome")}</Tooltip.Content>
    </Tooltip>
    <Tooltip>
      <Button variant="tertiary" isDisabled={isLoading || isContextLoading} isPending={isLoading} onPress={() => void logout(LandingRoutes.Home)}>
        <FaIcon icon="arrow-right-from-bracket" aria-hidden="true" className="rtl:-scale-x-100" />
        <span className="max-sm:sr-only">{t("home.signOut")}</span>
      </Button>
      <Tooltip.Content>{t("home.signOut")}</Tooltip.Content>
    </Tooltip>
    {failure ? <Alert status="danger">
      <Alert.Indicator><FaIcon icon="triangle-exclamation" aria-hidden="true" /></Alert.Indicator>
      <Alert.Content><Alert.Description>{failure}</Alert.Description></Alert.Content>
    </Alert> : null}
  </>;
}

export function ContentStudioAccess({ loginHref }: { loginHref: string }) {
  const router = useRouter();
  const navigate = useCallback((href: string) => router.push(href), [router]);

  return (
    <AuthorizationGuard
      requiredRole="owner"
      loginHref={loginHref}
      homeHref={LandingRoutes.PlatformHome}
      onNavigate={navigate}
    >
      <StandaloneDocumentPage className="min-h-dvh bg-surface-secondary py-8 sm:py-12">
        <PageContainer width="content">
          <ContentStudio headerActions={<StudioHeaderActions navigate={navigate} />} />
        </PageContainer>
      </StandaloneDocumentPage>
    </AuthorizationGuard>
  );
}
