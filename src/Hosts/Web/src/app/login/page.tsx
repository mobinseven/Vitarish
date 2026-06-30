"use client";

import { CenteredSurface } from "@/design-system";
import { AuthMethodSelector } from "@/components/auth/AuthMethodSelector";

export default function LoginPage() {
  return (
    <CenteredSurface title="Vitarish" description="Sign in to your account">
      <AuthMethodSelector />
    </CenteredSurface>
  );
}
