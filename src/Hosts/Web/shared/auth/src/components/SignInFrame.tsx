"use client";

import { Suspense, type ComponentType, type ReactNode } from "react";
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

  return (
    <Frame>
      <Suspense fallback={loadingFallback}>
        <AuthenticatedRedirectBoundary
          {...redirectOptions}
          onNavigate={onNavigate}
          loadingFallback={loadingFallback}
        >
          {children}
        </AuthenticatedRedirectBoundary>
      </Suspense>
    </Frame>
  );
}
