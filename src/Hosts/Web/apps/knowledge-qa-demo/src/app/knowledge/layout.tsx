"use client";

import { useCallback, type ReactNode } from "react";
import { usePathname, useRouter } from "next/navigation";
import { AuthorizationGuard } from "@vitarish/auth-web";
import { Routes } from "@/app/routes";

export default function KnowledgeLayout({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const navigate = useCallback(
    (href: string) => router.replace(href),
    [router],
  );
  const loginHref = `${Routes.Login}?returnUrl=${encodeURIComponent(pathname)}`;

  return (
    <AuthorizationGuard loginHref={loginHref} onNavigate={navigate}>
      {children}
    </AuthorizationGuard>
  );
}
