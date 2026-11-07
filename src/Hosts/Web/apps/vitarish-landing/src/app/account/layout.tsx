import type { ReactNode } from "react";
import { CentralAuthenticationLayout } from "@vitarish/auth/central-server";
import { AuthenticationWebRoutes } from "@vitarish/auth/routes";

export default function AccountLayout({ children }: { children: ReactNode }) {
  return <CentralAuthenticationLayout securityPath={AuthenticationWebRoutes.Account}>{children}</CentralAuthenticationLayout>;
}
