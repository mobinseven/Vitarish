import type { ReactNode } from "react";
import { CentralAuthenticationLayout, isCentralAuthenticationHost } from "@vitarish/auth/central-server";
import { LoginFrame } from "./LoginFrame";

export default async function LoginLayout({ children }: { children: ReactNode }) {
  const central = await isCentralAuthenticationHost();
  return <CentralAuthenticationLayout><LoginFrame continueOnSignInHost={central}>{children}</LoginFrame></CentralAuthenticationLayout>;
}
