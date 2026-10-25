"use client";

import { useCallback, useContext } from "react";
import { LocalizationContext } from "./LocalizationProvider";
import { setLocaleCookie } from "./localeCookie";
import type { SupportedLocale } from "./locales";

/** Saves a language choice in the locale cookie, shared across the deployment's hosts when it names a domain. */
export function useSetLocaleCookie(): (locale: SupportedLocale) => void {
  const localeCookieDomain = useContext(LocalizationContext)?.localeCookieDomain ?? null;
  return useCallback((locale: SupportedLocale) => setLocaleCookie(locale, localeCookieDomain), [localeCookieDomain]);
}
