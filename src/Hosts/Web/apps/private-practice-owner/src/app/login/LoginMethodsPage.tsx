"use client";

import { useSearchParams } from "next/navigation";
import { SignInMethodsPage } from "@vitarish/auth";

/** The clinic app's sign-in route supplies the request's query to the shared sign-in methods. */
export function LoginMethodsPage() {
  return <SignInMethodsPage searchParams={useSearchParams()} />;
}
