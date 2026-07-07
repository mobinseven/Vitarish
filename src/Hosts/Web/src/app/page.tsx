"use client";

import { useRouter } from "next/navigation";
import { Routes } from "@/app/routes";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { LandingPage } from "@/features/landing/landing-page";

export default function RootPage() {
  const router = useRouter();
  const { isAuthenticated, isLoading } = useAuthSession();

  if (isLoading) {
    return (
      <main className="grid min-h-screen place-items-center">
        <p className="text-muted">Loading...</p>
      </main>
    );
  }

  if (isAuthenticated) {
    router.replace(Routes.Home);
    return (
      <main className="grid min-h-screen place-items-center">
        <p className="text-muted">Redirecting...</p>
      </main>
    );
  }

  return <LandingPage />;
}
