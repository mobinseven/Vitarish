import type { ReactNode } from "react";
import { AuthenticatedRedirectBoundary } from "@/components/auth/SessionRedirect";

export default function LoginLayout({ children }: { children: ReactNode }) {
  return <AuthenticatedRedirectBoundary>{children}</AuthenticatedRedirectBoundary>;
}
