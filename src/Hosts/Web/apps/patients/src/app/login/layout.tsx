import type { ReactNode } from "react";
import { CentralAuthenticationLayout } from "@vitarish/auth/central-server";
import { LoginFrame } from "./LoginFrame";
export default function LoginLayout({ children }: { children: ReactNode }) {
  return <CentralAuthenticationLayout><LoginFrame>{children}</LoginFrame></CentralAuthenticationLayout>;
}
