"use client";

import { useSearchParams } from "next/navigation";
import { PasswordRecoveryPage } from "@vitarish/auth";
import { Routes } from "@/app/routes";

export default function ForgotPasswordPage() {
  const searchParams = useSearchParams();
  return <PasswordRecoveryPage loginHref={Routes.Login.Password} returnUrlSearchParams={searchParams} />;
}
