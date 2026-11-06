"use client";

import { useState } from "react";
import { TenantHandoffPage } from "@vitarish/journey";
import { isSafeRelativeReturnTo, signInReturnUrlQueryKey } from "@vitarish/journey/sign-in-host";
import { LandingRoutes } from "../routes";

/** Home unless the link carries a safe path on this host to return to, such as the studio. */
function readDestination(): string {
  const requested = new URLSearchParams(window.location.search).get(signInReturnUrlQueryKey);
  return isSafeRelativeReturnTo(requested) ? requested : LandingRoutes.PlatformHome;
}

export function LandingHandoff({ signInHref }: { signInHref: string }) {
  // Read once, before the handoff page takes the token and rewrites the address.
  const [destination] = useState(() => (typeof window === "undefined" ? LandingRoutes.PlatformHome : readDestination()));
  return <TenantHandoffPage signInPath={signInHref} destination={destination} />;
}
