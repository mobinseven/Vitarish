import { Suspense, type ReactNode } from "react";
import { LoadingScreen } from "@/components/loading/LoadingScreen";
import { CentralAuthenticationLayout, isCentralAuthenticationHost } from "@vitarish/auth/central-server";
import { AuthenticationWebRoutes } from "@vitarish/auth/routes";
import { AccountFrame } from "./AccountFrame";
export default async function AccountLayout({ children }: { children: ReactNode }) {
  const central = await isCentralAuthenticationHost();
  return <CentralAuthenticationLayout securityPath={AuthenticationWebRoutes.Account}>
    <Suspense fallback={<LoadingScreen />}><AccountFrame central={central}>{children}</AccountFrame></Suspense>
  </CentralAuthenticationLayout>;
}
