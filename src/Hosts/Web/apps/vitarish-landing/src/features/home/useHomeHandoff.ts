"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import type { NavigationTransitionContext } from "@vitarish/ui/navigation-context";
import { carryNavigationTransition, isCurrentNavigationTransition, takeNavigationTransition, navigationTransitionSignal } from "@vitarish/ui/navigation";
import { navigationTransitionStates, navigationTransitionRuntime } from "@vitarish/ui/navigation-runtime";
import { handoffUrl } from "@vitarish/journey";
import { tenantHandoffPath } from "@vitarish/journey/onboarding-host";
import { landingIssueOnboardingHandoff, landingIssueWorkspaceHandoff } from "@vitarish/platform";

/** What the person chose to leave home for: one of their workspaces, or creating one in an app. */
export type HomeHandoffTarget = { kind: "open"; tenantId: string } | { kind: "create"; appId: string };

/**
 * Leaves home for a workspace or for workspace setup. Opening a workspace keeps the home session;
 * the server picks every target host (ADR-0020).
 */
export function useHomeHandoff() {
  const [pending, setPending] = useState<HomeHandoffTarget | null>(null);
  const [failed, setFailed] = useState<HomeHandoffTarget | null>(null);
  const inFlight = useRef(false);
  const mounted = useRef(true);
  const activeAttempt = useRef<NavigationTransitionContext | null>(null);

  useEffect(() => {
    mounted.current = true;
    return () => {
      mounted.current = false;
      if (inFlight.current && activeAttempt.current && isCurrentNavigationTransition(activeAttempt.current)) {
        navigationTransitionRuntime()?.finish(navigationTransitionStates.Cancelled);
      }
    };
  }, []);

  const start = useCallback((target: HomeHandoffTarget) => {
    if (inFlight.current) return;
    inFlight.current = true;
    const attempt = takeNavigationTransition();
    activeAttempt.current = attempt;
    const signal = navigationTransitionSignal();
    signal?.addEventListener("abort", () => {
      inFlight.current = false;
      if (mounted.current) setPending(null);
    }, { once: true });
    setPending(target);
    setFailed(null);
    const issue = target.kind === "open"
      ? landingIssueWorkspaceHandoff(target.tenantId, undefined, navigationTransitionSignal())
      : landingIssueOnboardingHandoff({ category: null, answers: [], appId: target.appId }, undefined, navigationTransitionSignal());
    issue.then(
      (handoff) => { if (mounted.current && isCurrentNavigationTransition(attempt)) window.location.assign(carryNavigationTransition(handoff.handoffUrl ?? handoffUrl(handoff.hostname, tenantHandoffPath, handoff.token))); },
    ).catch(() => {
        if (!mounted.current || !isCurrentNavigationTransition(attempt)) return;
        navigationTransitionRuntime()?.finish(navigationTransitionStates.Failed);
        inFlight.current = false;
        setPending(null);
        setFailed(target);
    });
  }, []);

  return { pending, failed, start };
}
