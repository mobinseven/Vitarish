import { CentralAuthenticationLayout } from "@vitarish/auth/central-server";
import { Suspense } from "react";
import { PasswordSignIn } from "./PasswordSignIn";
import { LandingRoutes } from "../routes";

export default function LoginPage() {
  return <CentralAuthenticationLayout defaultReturnPath={LandingRoutes.PlatformHome}><Suspense><PasswordSignIn /></Suspense></CentralAuthenticationLayout>;
}
