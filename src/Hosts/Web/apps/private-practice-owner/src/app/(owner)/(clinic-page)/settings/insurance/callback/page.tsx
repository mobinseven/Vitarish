"use client";

import { InsuranceProviderAuthorizationCallbackPage } from "@vitarish/clinic";
import { useRouter } from "next/navigation";
import { Routes } from "@/app/routes";

export default function InsuranceProviderAuthorizationCallbackRoute() {
  const router = useRouter();
  return (
    <InsuranceProviderAuthorizationCallbackPage
      onReturn={() => router.replace(Routes.Clinic.Settings.Insurance)}
    />
  );
}
