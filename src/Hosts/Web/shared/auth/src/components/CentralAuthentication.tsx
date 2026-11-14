"use client";

import { useEffect, useRef, useState } from "react";
import { Spinner } from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import { carryNavigationTransition, takeNavigationTransition, isCurrentNavigationTransition, navigationTransitionSignal } from "@vitarish/ui/navigation";
import { withoutNavigationTransition } from "@vitarish/ui/navigation-context";
import { navigationTransitionStates, useNavigationTransitionParticipant } from "@vitarish/ui/navigation-runtime";
import { StandaloneCenteredPage } from "@vitarish/ui/layout";
import { ApiError } from "@vitarish/api-client";
import { authenticationContinuationBegin, authenticationContinuationComplete, authenticationTenantHandoffRedeem } from "../api/generated/authentication/authentication";
import { AuthenticationSearchParams, AuthenticationWebRoutes } from "../routes";
import { getRequestedReturnUrl } from "../hooks/requestedReturnUrl";
import { AuthenticationEntryFailure } from "./AuthenticationEntryFailure";

function AuthenticationProgress({ failed, error }: { failed: boolean; error?: unknown }) {
  const covered = useNavigationTransitionParticipant(!failed, failed ? navigationTransitionStates.Failed : undefined);
  const { t } = useLocalizedText();
  if (failed) return <AuthenticationEntryFailure error={error} />;
  if (covered) return null;
  return <StandaloneCenteredPage><div role="status" className="flex items-center gap-3"><Spinner /><p>{t("auth.guard.loading")}</p></div></StandaloneCenteredPage>;
}

/** Keeps interactive authentication exclusively on Auth while preserving the originating app. */
export function CentralAuthenticationBoundary({ signInUrl, securityPath, defaultReturnPath }: {
  signInUrl: string; securityPath?: string; defaultReturnPath?: string;
}) {
  const started = useRef(false);
  const [failed, setFailed] = useState(false);
  const [error, setError] = useState<unknown>();
  useEffect(() => {
    if (started.current) return;
    started.current = true;
    const attempt = takeNavigationTransition();
    if (securityPath) {
      window.location.replace(carryNavigationTransition(new URL(window.location.pathname.startsWith(AuthenticationWebRoutes.Account) ? withoutNavigationTransition(window.location.pathname + window.location.search) : securityPath, signInUrl).toString()));
      return;
    }
    const returnPath = getRequestedReturnUrl(new URLSearchParams(window.location.search)) ?? defaultReturnPath;
    authenticationContinuationBegin({ returnPath: returnPath ? withoutNavigationTransition(returnPath) : null }, undefined, navigationTransitionSignal()).then(({ token }) => {
      if (!isCurrentNavigationTransition(attempt)) return;
      const method = window.location.pathname.startsWith(AuthenticationWebRoutes.SignIn + "/")
        ? window.location.pathname : AuthenticationWebRoutes.SignIn;
      const target = new URL(method, signInUrl);
      const continuation = new URL(AuthenticationWebRoutes.Continue, signInUrl);
      continuation.searchParams.set(AuthenticationSearchParams.Request, token);
      target.searchParams.set(AuthenticationSearchParams.ReturnUrl, continuation.pathname + continuation.search);
      window.location.replace(carryNavigationTransition(target.toString()));
    }).catch(error => { if (isCurrentNavigationTransition(attempt)) { setError(error); setFailed(true); } });
  }, [securityPath, signInUrl, defaultReturnPath]);
  return <AuthenticationProgress failed={failed} error={error} />;
}

/** Redeems the one-time, host- and browser-bound ticket and loads the app with its session. */
export function CentralAuthenticationHandoff() {
  const started = useRef(false);
  const [failed, setFailed] = useState(false);
  const [error, setError] = useState<unknown>();
  useEffect(() => {
    if (started.current) return;
    started.current = true;
    const attempt = takeNavigationTransition();
    const token = window.location.hash.slice(1);
    window.history.replaceState(null, "", window.location.pathname + window.location.search);
    if (!token) { setFailed(true); return; }
    authenticationTenantHandoffRedeem({ token }, undefined, navigationTransitionSignal()).then(result => { if (isCurrentNavigationTransition(attempt)) window.location.replace(carryNavigationTransition(result.returnUrl)); })
      .catch(error => { if (isCurrentNavigationTransition(attempt)) { setError(error); setFailed(true); } });
  }, []);
  return <AuthenticationProgress failed={failed} error={error} />;
}

/** Auth's signed-in continuation endpoint; anonymous visitors return here after authentication. */
export function CentralAuthenticationContinue() {
  const started = useRef(false);
  const [failed, setFailed] = useState(false);
  const [error, setError] = useState<unknown>();
  useEffect(() => {
    if (started.current) return;
    started.current = true;
    const attempt = takeNavigationTransition();
    const token = new URLSearchParams(window.location.search).get(AuthenticationSearchParams.Request);
    if (!token) { setFailed(true); return; }
    authenticationContinuationComplete({ token }, undefined, navigationTransitionSignal()).then(result => { if (isCurrentNavigationTransition(attempt)) window.location.replace(carryNavigationTransition(result.returnUrl)); })
      .catch(error => {
        if (!isCurrentNavigationTransition(attempt)) return;
        if (error instanceof ApiError && error.status === 401) {
          const signIn = new URL(AuthenticationWebRoutes.SignIn, window.location.origin);
          signIn.searchParams.set(AuthenticationSearchParams.ReturnUrl, withoutNavigationTransition(window.location.pathname + window.location.search));
          window.location.replace(carryNavigationTransition(signIn.toString()));
        } else { setError(error); setFailed(true); }
      });
  }, []);
  return <AuthenticationProgress failed={failed} error={error} />;
}
