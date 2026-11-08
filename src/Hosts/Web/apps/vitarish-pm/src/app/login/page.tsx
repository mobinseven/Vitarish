import { CentralAuthenticationLayout } from "@vitarish/auth/central-server";
import { Suspense } from "react";
import { PasswordSignIn } from "./PasswordSignIn";
export default function LoginPage() {
  return <CentralAuthenticationLayout><Suspense><PasswordSignIn /></Suspense></CentralAuthenticationLayout>;
}
