"use client";

import { TenantHandoffPage } from "@vitarish/journey";

export function LandingHandoff({ signInHref, destination }: { signInHref: string; destination: string }) {
  return <TenantHandoffPage signInPath={signInHref} destination={destination} />;
}
