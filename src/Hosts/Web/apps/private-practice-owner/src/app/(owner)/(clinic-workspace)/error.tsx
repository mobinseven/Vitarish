"use client";

import { ClinicRouteErrorState } from "@/components/shell/ClinicRouteStates";

export default function ClinicRouteError({ reset }: { error: Error; reset: () => void }) {
  return <ClinicRouteErrorState reset={reset} />;
}
