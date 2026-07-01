"use client";

import {
  createContext,
  useContext,
  useMemo,
  type ReactNode,
} from "react";
import {
  cultureCookieName,
  getCultureDirection,
  normalizeCulture,
  type SupportedCulture,
} from "./cultures";
import { createLocalizer, type Localizer } from "./localizer";

const LocaleContext = createContext<Localizer | null>(null);

export function LocaleProvider({
  culture,
  children,
}: {
  culture: SupportedCulture;
  children: ReactNode;
}) {
  const localizer = useMemo(() => createLocalizer(culture), [culture]);

  return (
    <LocaleContext.Provider value={localizer}>{children}</LocaleContext.Provider>
  );
}

export function useLocalizer(): Localizer {
  const localizer = useContext(LocaleContext);

  if (!localizer) {
    throw new Error("LocaleProvider is missing from the React tree.");
  }

  return localizer;
}

export function persistClientCulture(cultureInput: string): SupportedCulture {
  const culture = normalizeCulture(cultureInput);
  const maxAgeSeconds = 60 * 60 * 24 * 365;

  document.cookie = `${cultureCookieName}=${encodeURIComponent(
    culture,
  )}; path=/; max-age=${maxAgeSeconds}; samesite=lax`;
  document.documentElement.lang = culture;
  document.documentElement.dir = getCultureDirection(culture);

  return culture;
}
