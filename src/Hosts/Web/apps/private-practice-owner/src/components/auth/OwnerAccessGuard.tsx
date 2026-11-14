"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { AuthorizationGuard } from "@vitarish/auth";
import { Routes } from "@/app/routes";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { LoadingScreen } from "@/components/loading/LoadingScreen";

/**
 * The owner pages' access check. The journey runs first, so a host that is not a clinic (the clinic front page)
 * sends a signed-in person to its clinic list instead of a "no access" screen. A person without access to this
 * clinic can go to their workspaces or, after confirming, sign out to use another account.
 */
export function OwnerAccessGuard({ workspacesHref, children }: { workspacesHref: string | null; children: React.ReactNode }) {
  const router = useRouter();
  // Workspaces live on another host, so that address needs a full navigation.
  const navigate = useCallback(
    (href: string) => (/^https?:\/\//.test(href) ? window.location.assign(href) : router.push(href)),
    [router],
  );

  return (
    <JourneyGateBoundary>
      <AuthorizationGuard
        requiredRole="owner"
        loginHref={Routes.Login.Index}
        homeHref={workspacesHref ?? undefined}
        confirmSignOut
        onNavigate={navigate}
        loadingFallback={<LoadingScreen />}
      >
        {children}
      </AuthorizationGuard>
    </JourneyGateBoundary>
  );
}
