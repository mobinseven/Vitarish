import type { ReactNode } from "react";
import { CentralAuthenticationLayout } from "@vitarish/auth/central-server";
export default function LoginLayout({ children }: { children: ReactNode }) {
  return <CentralAuthenticationLayout>{children}</CentralAuthenticationLayout>;
}
