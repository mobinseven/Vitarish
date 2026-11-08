"use client";

import { Suspense, useCallback, type ReactNode } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { AuthenticatedRedirectBoundary } from "@vitarish/auth";
import { isSafeRelativeReturnTo, signInReturnUrlQueryKey } from "@vitarish/journey/sign-in-host";
import { AuthenticationSearchParams, AuthenticationWebRoutes } from "@vitarish/auth/routes";
import { Routes } from "@/app/routes";
import { BrandedPageFrame } from "@/components/branding/BrandedPageFrame";
import { LoadingScreen } from "@/components/loading/LoadingScreen";

function LoginRouteFallback() {
  return <LoadingScreen />;
}

function signInContinueHref(searchParams: Pick<URLSearchParams, "get">): string {
  const requestedReturnUrl = searchParams.get(signInReturnUrlQueryKey);
  if (requestedReturnUrl?.startsWith(AuthenticationWebRoutes.Continue + "?") && new URLSearchParams(requestedReturnUrl.split("?")[1]).has(AuthenticationSearchParams.Request)) return requestedReturnUrl;
  if (!isSafeRelativeReturnTo(requestedReturnUrl)) return Routes.SignInContinue;

  const query = new URLSearchParams({ [signInReturnUrlQueryKey]: requestedReturnUrl });
  return `${Routes.SignInContinue}?${query.toString()}`;
}

export function authenticatedLoginRedirectOptions(
  continueOnSignInHost: boolean,
  searchParams: Pick<URLSearchParams, "get">,
) {
  return continueOnSignInHost
    ? { authenticatedHref: signInContinueHref(searchParams), returnUrlSearchParams: undefined }
    : { authenticatedHref: Routes.Home, returnUrlSearchParams: searchParams };
}

export function LoginFrame({
  children,
  continueOnSignInHost,
}: {
  children: ReactNode;
  continueOnSignInHost: boolean;
}) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const navigate = useCallback(
    (href: string) => router.replace(href),
    [router],
  );
  const redirectOptions = authenticatedLoginRedirectOptions(continueOnSignInHost, searchParams);

  return (
    <BrandedPageFrame>
      <Suspense fallback={<LoginRouteFallback />}>
        <AuthenticatedRedirectBoundary
          {...redirectOptions}
          onNavigate={navigate}
          loadingFallback={<LoadingScreen />}
        >
          {children}
        </AuthenticatedRedirectBoundary>
      </Suspense>
    </BrandedPageFrame>
  );
}
