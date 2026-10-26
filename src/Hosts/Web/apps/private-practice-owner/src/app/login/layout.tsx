import type { ReactNode } from "react";
import { headers } from "next/headers";
import { redirect } from "next/navigation";
import { readSignInUrl, signInHostRedirectUrl } from "@vitarish/journey/sign-in-host";
import { Routes } from "@/app/routes";
import { LoginFrame } from "./LoginFrame";

export default async function LoginLayout({ children }: { children: ReactNode }) {
  // People sign in only on the shared sign-in host (ADR-0018); every other host sends them there.
  const signInUrl = readSignInUrl(process.env);
  const target = signInUrl && signInHostRedirectUrl(signInUrl, (await headers()).get("host"), Routes.Login.Index);
  if (target) redirect(target);
  return <LoginFrame>{children}</LoginFrame>;
}
