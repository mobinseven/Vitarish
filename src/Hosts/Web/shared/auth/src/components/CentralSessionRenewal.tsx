"use client";

import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { getAuthenticationSessionGetQueryKey } from "../api/generated/authentication/authentication";

const renewalIntervalMilliseconds = 30_000;
const renewalTimeoutMilliseconds = 5_000;

/** Active app requests also renew Auth's host-only cookie without replacing the page. */
export function CentralSessionRenewal({ signInUrl }: { signInUrl: string }) {
  const client = useQueryClient();
  useEffect(() => {
    if (new URL(signInUrl).origin === window.location.origin) return;
    let lastRenewal = -renewalIntervalMilliseconds;
    let controller: AbortController | undefined;
    let disposed = false;
    const unsubscribe = client.getQueryCache().subscribe(event => {
      if (event.type !== "updated" || event.action.type !== "success" ||
          !client.getQueryData<{ isAuthenticated: boolean }>(getAuthenticationSessionGetQueryKey())?.isAuthenticated ||
          performance.now() - lastRenewal < renewalIntervalMilliseconds) return;
      lastRenewal = performance.now();
      controller?.abort();
      controller = new AbortController();
      const request = controller;
      const timeout = setTimeout(() => request.abort(), renewalTimeoutMilliseconds);
      // Opaque GET intentionally sends no tenant or account data and reads no cross-origin
      // response. The actual Auth hostname chooses its cookie/tenant; the normal server
      // cookie validator owns renewal. No redirect or app authentication decision follows.
      void fetch(new URL(getAuthenticationSessionGetQueryKey()[0], signInUrl), {
        mode: "no-cors", credentials: "include", referrerPolicy: "no-referrer", cache: "no-store", signal: request.signal,
      }).catch(() => { if (!disposed) console.warn("Central session cookie renewal is temporarily unavailable."); }).finally(() => clearTimeout(timeout));
    });
    return () => { disposed = true; unsubscribe(); controller?.abort(); };
  }, [client, signInUrl]);
  return null;
}
