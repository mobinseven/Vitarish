"use client";

import { NavigationTransitionShellReady } from "@vitarish/ui/navigation-runtime";
import { useAuthSession } from "../hooks/useAuthSession";

export function NavigationTransitionReadiness({ sessionEnabled = true }: { sessionEnabled?: boolean }) {
  const { isLoading } = useAuthSession({ enabled: sessionEnabled });
  return <NavigationTransitionShellReady pending={isLoading} />;
}
