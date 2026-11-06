"use client";

import { useCallback, useState } from "react";
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

  const start = useCallback((target: HomeHandoffTarget) => {
    setPending(target);
    setFailed(null);
    const issue = target.kind === "open"
      ? landingIssueWorkspaceHandoff(target.tenantId)
      : landingIssueOnboardingHandoff({ category: null, answers: [], appId: target.appId });
    issue.then(
      (handoff) => window.location.assign(handoffUrl(handoff.hostname, tenantHandoffPath, handoff.token)),
      () => {
        setPending(null);
        setFailed(target);
      },
    );
  }, []);

  return { pending, failed, start };
}
