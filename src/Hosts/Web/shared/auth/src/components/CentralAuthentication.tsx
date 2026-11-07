"use client";

import { useEffect, useRef, useState } from "react";
import { Spinner } from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import { StatusAlert } from "@vitarish/ui";
import { StandaloneCenteredPage } from "@vitarish/ui/layout";
import { ApiError } from "@vitarish/api-client";
import { authenticationContinuationBegin, authenticationContinuationComplete, authenticationTenantHandoffRedeem } from "../api/generated/authentication/authentication";
import { AuthenticationSearchParams, AuthenticationWebRoutes } from "../routes";
import { getRequestedReturnUrl } from "../hooks/requestedReturnUrl";

function AuthenticationProgress({ failed }: { failed: boolean }) {
  const { t } = useLocalizedText();
  return <StandaloneCenteredPage>{failed
    ? <div className="flex flex-col gap-4"><StatusAlert message={t("auth.failures.providerFailure")} /><a className="text-accent underline" href={AuthenticationWebRoutes.SignIn}>{t("auth.login.signInAction")}</a></div>
    : <div role="status" className="flex items-center gap-3"><Spinner /><p>{t("auth.guard.loading")}</p></div>}
  </StandaloneCenteredPage>;
}

/** Keeps interactive authentication exclusively on Auth while preserving the originating app. */
export function CentralAuthenticationBoundary({ signInUrl, securityPath }: {
  signInUrl: string; securityPath?: string;
}) {
  const started = useRef(false);
  const [failed, setFailed] = useState(false);
  useEffect(() => {
    if (started.current) return;
    started.current = true;
    if (securityPath) {
      window.location.replace(new URL(window.location.pathname.startsWith(AuthenticationWebRoutes.Account) ? window.location.pathname + window.location.search : securityPath, signInUrl).toString());
      return;
    }
    const returnPath = getRequestedReturnUrl(new URLSearchParams(window.location.search));
    authenticationContinuationBegin({ returnPath: returnPath ?? null }).then(({ token }) => {
      const method = window.location.pathname.startsWith(AuthenticationWebRoutes.SignIn + "/")
        ? window.location.pathname : AuthenticationWebRoutes.SignIn;
      const target = new URL(method, signInUrl);
      const continuation = new URL(AuthenticationWebRoutes.Continue, signInUrl);
      continuation.searchParams.set(AuthenticationSearchParams.Request, token);
      target.searchParams.set(AuthenticationSearchParams.ReturnUrl, continuation.pathname + continuation.search);
      window.location.replace(target.toString());
    }).catch(() => setFailed(true));
  }, [securityPath, signInUrl]);
  return <AuthenticationProgress failed={failed} />;
}

/** Redeems the one-time, host- and browser-bound ticket and loads the app with its session. */
export function CentralAuthenticationHandoff() {
  const started = useRef(false);
  const [failed, setFailed] = useState(false);
  useEffect(() => {
    if (started.current) return;
    started.current = true;
    const token = window.location.hash.slice(1);
    window.history.replaceState(null, "", window.location.pathname + window.location.search);
    if (!token) { setFailed(true); return; }
    authenticationTenantHandoffRedeem({ token }).then(result => window.location.replace(result.returnUrl))
      .catch(() => setFailed(true));
  }, []);
  return <AuthenticationProgress failed={failed} />;
}

/** Auth's signed-in continuation endpoint; anonymous visitors return here after authentication. */
export function CentralAuthenticationContinue() {
  const started = useRef(false);
  const [failed, setFailed] = useState(false);
  useEffect(() => {
    if (started.current) return;
    started.current = true;
    const token = new URLSearchParams(window.location.search).get(AuthenticationSearchParams.Request);
    if (!token) { setFailed(true); return; }
    authenticationContinuationComplete({ token }).then(result => window.location.replace(result.returnUrl))
      .catch(error => {
        if (error instanceof ApiError && error.status === 401) {
          const signIn = new URL(AuthenticationWebRoutes.SignIn, window.location.origin);
          signIn.searchParams.set(AuthenticationSearchParams.ReturnUrl, window.location.pathname + window.location.search);
          window.location.replace(signIn.toString());
        } else setFailed(true);
      });
  }, []);
  return <AuthenticationProgress failed={failed} />;
}
