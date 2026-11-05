"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { AuthorizationGuard } from "@vitarish/auth";
import { ContentStudio } from "@vitarish/contents/studio";

export function ContentStudioAccess({ loginHref }: { loginHref: string }) {
  const router = useRouter();
  const navigate = useCallback((href: string) => router.push(href), [router]);

  return (
    <AuthorizationGuard
      requiredRole="owner"
      loginHref={loginHref}
      onNavigate={navigate}
    >
      <ContentStudio />
    </AuthorizationGuard>
  );
}
