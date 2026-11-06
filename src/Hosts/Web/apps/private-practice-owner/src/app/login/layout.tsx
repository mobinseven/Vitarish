import type { ReactNode } from "react";
import { headers } from "next/headers";
import { redirect } from "next/navigation";
import { isSignInHost, readSignInUrl, signInHostRedirectUrl } from "@vitarish/journey/sign-in-host";
import { Routes } from "@/app/routes";
import { LoginFrame } from "./LoginFrame";

export default async function LoginLayout({ children }: { children: ReactNode }) {
  // People sign in only on the shared sign-in host (ADR-0018); every other host sends them there.
  const signInUrl = readSignInUrl(process.env);
  const requestHost = (await headers()).get("host");
  const continueOnSignInHost = signInUrl !== null && isSignInHost(signInUrl, requestHost);
  const target = signInUrl && signInHostRedirectUrl(signInUrl, requestHost, Routes.Login.Index);
  if (target) redirect(target);
  return <LoginFrame continueOnSignInHost={continueOnSignInHost}>{children}</LoginFrame>;
}
