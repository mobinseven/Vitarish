import { Suspense, type ReactNode } from "react";
import { AuthenticatedRedirectBoundary } from "@/components/auth/SessionRedirect";

function LoginRouteFallback() {
  return (
    <main className="ds-app-shell ds-centered-shell">
      <p className="text-muted">Loading...</p>
    </main>
  );
}

export default function LoginLayout({ children }: { children: ReactNode }) {
  return (
    <Suspense fallback={<LoginRouteFallback />}>
      <AuthenticatedRedirectBoundary>{children}</AuthenticatedRedirectBoundary>
    </Suspense>
  );
}
