"use client";

import { Suspense, useCallback, type ReactNode } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { AuthenticatedRedirectBoundary } from "@vitarish/auth";
import { Routes } from "@/app/routes";
import { LoadingScreen } from "@/components/loading/LoadingScreen";
import { useLocalizedText } from "@/localization";

function LoginLogotype({
  src,
  darkSrc,
  alt,
}: {
  src: string;
  darkSrc: string;
  alt: string;
}) {
  return (
    <header className="w-full">
      <img
        src={src}
        alt={alt}
        aria-hidden={alt === "" ? true : undefined}
        className="block h-auto w-full dark:hidden"
      />
      <img
        src={darkSrc}
        alt=""
        aria-hidden="true"
        className="hidden h-auto w-full dark:block"
      />
    </header>
  );
}

function LoginRouteFallback() {
  return <LoadingScreen />;
}

export default function LoginLayout({ children }: { children: ReactNode }) {
  const { t } = useLocalizedText();
  const router = useRouter();
  const searchParams = useSearchParams();
  const navigate = useCallback(
    (href: string) => router.replace(href),
    [router],
  );

  return (
    <>
      <LoginLogotype
        src="/Master-Hel-Logotype.svg"
        darkSrc="/Master-Hel-Logotype-Dark.svg"
        alt={t("appShell.brand")}
      />
      <Suspense fallback={<LoginRouteFallback />}>
        <AuthenticatedRedirectBoundary
          authenticatedHref={Routes.Home}
          onNavigate={navigate}
          loadingFallback={<LoadingScreen />}
          returnUrlSearchParams={searchParams}
        >
          {children}
        </AuthenticatedRedirectBoundary>
      </Suspense>
      <LoginLogotype
        src="/Master-Hel-Logotype-©.svg"
        darkSrc="/Master-Hel-Logotype-Dark-©.svg"
        alt=""
      />
    </>
  );
}
