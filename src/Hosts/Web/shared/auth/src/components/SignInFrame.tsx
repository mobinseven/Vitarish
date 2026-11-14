"use client";

import { Suspense, useCallback, type ComponentType, type ReactNode } from "react";
import { navigationTransitionRuntime, useNavigationTransitionParticipant } from "@vitarish/ui/navigation-runtime";
import { isCentralContinuationReturnTo, isSafeRelativeReturnTo, signInReturnUrlQueryKey } from "../centralHost";
import type { AuthNavigate } from "../hooks/navigation";
import { AuthenticatedRedirectBoundary } from "./SessionRedirect";

interface SignInRedirectRoutes {
  /** Where a signed-in visitor of a standalone (non-central) sign-in page goes. */
  homeHref: string;
  /** The continue route on the central sign-in host. */
  continueHref: string;
}

function signInContinueHref(searchParams: Pick<URLSearchParams, "get">, continueHref: string): string {
  const requestedReturnUrl = searchParams.get(signInReturnUrlQueryKey);
  if (isCentralContinuationReturnTo(requestedReturnUrl)) return requestedReturnUrl;
  if (!isSafeRelativeReturnTo(requestedReturnUrl)) return continueHref;

  const query = new URLSearchParams({ [signInReturnUrlQueryKey]: requestedReturnUrl });
  return `${continueHref}?${query.toString()}`;
}

export function authenticatedSignInRedirectOptions(
  continueOnSignInHost: boolean,
  searchParams: Pick<URLSearchParams, "get">,
  { homeHref, continueHref }: SignInRedirectRoutes,
) {
  return continueOnSignInHost
    ? { authenticatedHref: signInContinueHref(searchParams, continueHref), returnUrlSearchParams: undefined }
    : { authenticatedHref: homeHref, returnUrlSearchParams: searchParams };
}

interface SignInFrameProps extends SignInRedirectRoutes {
  children: ReactNode;
  continueOnSignInHost: boolean;
  searchParams: Pick<URLSearchParams, "get">;
  onNavigate: AuthNavigate;
  /** The app's page chrome around the sign-in pages. */
  frame: ComponentType<{ children: ReactNode }>;
  loadingFallback: ReactNode;
}

function SignInDiscoveryFallback({ children }: { children: ReactNode }) {
  const covered = useNavigationTransitionParticipant(true);
  return covered ? null : <>{children}</>;
}

/** Sends a visitor who arrives signed in onward, and frames the sign-in pages for the hosting app. */
export function SignInFrame({
  children,
  continueOnSignInHost,
  searchParams,
  onNavigate,
  homeHref,
  continueHref,
  frame: Frame,
  loadingFallback,
}: SignInFrameProps) {
  const redirectOptions = authenticatedSignInRedirectOptions(continueOnSignInHost, searchParams, { homeHref, continueHref });
  const continueSignedIn = useCallback((href: string) => {
    // Auth's continuation is automatic work in the same document. Keep it covered
    // across the gap between route commits instead of declaring the login ready.
    if (continueOnSignInHost) navigationTransitionRuntime()?.departing();
    onNavigate(href);
  }, [continueOnSignInHost, onNavigate]);

  return (
      <Suspense fallback={<SignInDiscoveryFallback>{loadingFallback}</SignInDiscoveryFallback>}>
        <AuthenticatedRedirectBoundary
          {...redirectOptions}
          onNavigate={continueSignedIn}
          loadingFallback={loadingFallback}
        >
          <Frame>{children}</Frame>
        </AuthenticatedRedirectBoundary>
      </Suspense>
  );
}
