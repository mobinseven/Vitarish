import type { ReactNode } from "react";
import { headers } from "next/headers";
import { readSignInUrl } from "./centralHost";
import { CentralSessionRenewal } from "./components/CentralSessionRenewal";
import { CentralAuthenticationBoundary } from "./components/CentralAuthentication";

export async function isCentralAuthenticationHost(): Promise<boolean> {
  const signInUrl = readSignInUrl(process.env);
  return signInUrl !== null && (await headers()).get("host")?.toLowerCase() === new URL(signInUrl).host;
}

export async function CentralAuthenticationLayout({ children, securityPath, defaultReturnPath }: { children: ReactNode; securityPath?: string; defaultReturnPath?: string }) {
  const signInUrl = readSignInUrl(process.env);
  if (!signInUrl || await isCentralAuthenticationHost()) return <>{children}</>;
  return <CentralAuthenticationBoundary signInUrl={signInUrl} securityPath={securityPath} defaultReturnPath={defaultReturnPath} />;
}

export async function CentralAuthenticationSessionRenewal() {
  const signInUrl = readSignInUrl(process.env);
  return signInUrl ? <CentralSessionRenewal signInUrl={signInUrl} /> : null;
}
