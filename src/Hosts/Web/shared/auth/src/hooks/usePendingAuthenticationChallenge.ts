"use client";

import { useEffect, useRef, useState } from "react";
import { authenticationPendingChallengeGet, authenticationPendingChallengeClear } from "../api/generated/authentication/authentication";
import type { PendingAuthenticationChallengeResponse } from "../api/generated/authentication/model";

/** Recovers presentation state from the server; never caches passwords or entered codes. */
export function usePendingAuthenticationChallenge(
  restore: (pending: PendingAuthenticationChallengeResponse) => void,
  onError: (error: unknown) => void,
) {
  const handlers = useRef({ restore, onError });
  handlers.current = { restore, onError };
  const accepted = useRef(true);
  const [isRestoring, setIsRestoring] = useState(true);
  useEffect(() => {
    const controller = new AbortController();
    Promise.resolve().then(() => authenticationPendingChallengeGet(undefined, controller.signal))
      .then(pending => {
        if (!controller.signal.aborted && accepted.current && pending)
          handlers.current.restore(pending);
      })
      .catch(error => { if (!controller.signal.aborted) handlers.current.onError(error); })
      .finally(() => { if (!controller.signal.aborted) setIsRestoring(false); });
    return () => controller.abort();
  }, []);
  return {
    isRestoring,
    cancelRestore: () => { accepted.current = false; },
    clear: () => {
      accepted.current = false;
      void authenticationPendingChallengeClear().catch(error => handlers.current.onError(error));
    },
  };
}
