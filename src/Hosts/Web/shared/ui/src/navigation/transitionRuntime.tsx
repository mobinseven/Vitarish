"use client";

import { useId, useLayoutEffect, useRef, useState } from "react";
import { navigationTransitionDocument, navigationTransitionStates, type NavigationTransitionRuntime, type NavigationTransitionOutcome } from "./transitionContract";
export { navigationTransitionStates } from "./transitionContract";

export function navigationTransitionRuntime(): NavigationTransitionRuntime | undefined {
  return typeof window === "undefined" ? undefined
    : (window as unknown as Record<string, NavigationTransitionRuntime>)[navigationTransitionDocument.runtime];
}

/** Participants only release their own work. Terminal outcomes never re-open an attempt. */
export function useNavigationTransitionParticipant(pending: boolean, outcome?: NavigationTransitionOutcome) {
  const participant = useId();
  const [covered, setCovered] = useState(false);
  const participation = useRef<{ attempt: string; signal: AbortSignal } | null>(null);
  useLayoutEffect(() => {
    const runtime = navigationTransitionRuntime();
    const surface = document.getElementById(navigationTransitionDocument.surfaceId);
    setCovered(Boolean(runtime && surface && !surface.hidden && (runtime.active() || runtime.state === navigationTransitionStates.Failed)));
    if (!runtime?.active()) return;
    if (!participation.current || (pending && participation.current.signal.aborted)) {
      participation.current = { attempt: runtime.context.attempt, signal: runtime.signal };
    }
    if (participation.current.attempt !== runtime.context.attempt) return;
    if (outcome) runtime?.finish(outcome);
    else if (pending) runtime?.hold(participant);
    else runtime?.release(participant);
  }, [participant, pending, outcome]);
  useLayoutEffect(() => {
    const runtime = navigationTransitionRuntime();
    return () => { if (participation.current?.attempt === runtime?.context.attempt) runtime?.release(participant); };
  }, [participant]);
  return covered;
}

/** Initial critical shell readiness; widget/data/image work is intentionally absent. */
export function NavigationTransitionShellReady({ pending }: { pending: boolean }) {
  useLayoutEffect(() => {
    if (pending) return;
    const runtime = navigationTransitionRuntime();
    const attempt = runtime?.context.attempt;
    let cancelled = false;
    let frame = requestAnimationFrame(() => {
      document.body.getBoundingClientRect();
      const finishFonts = () => {
        if (cancelled || attempt !== runtime?.context.attempt) return;
        frame = requestAnimationFrame(() => { if (!cancelled && attempt === runtime?.context.attempt) runtime?.release(navigationTransitionDocument.shellParticipant); });
      };
      // A failed font has a usable CSS fallback, rather than an authentication failure.
      void (document.fonts?.ready ?? Promise.resolve()).then(finishFonts, finishFonts);
    });
    return () => { cancelled = true; cancelAnimationFrame(frame); };
  }, [pending]);
  return null;
}
