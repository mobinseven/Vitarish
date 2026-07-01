import { defaultLocale, type SupportedLocale, toSupportedLocale } from "./locales";

export const localeCookieName = "vitarish.locale";

export function parseLocaleCookie(cookieHeader: string | null | undefined): SupportedLocale | null {
  if (!cookieHeader) return null;
  const pair = cookieHeader.split(";").map((part) => part.trim()).find((part) => part.startsWith(`${localeCookieName}=`));
  if (!pair) return null;
  return toSupportedLocale(decodeURIComponent(pair.slice(localeCookieName.length + 1)));
}

export function serializeLocaleCookie(locale: SupportedLocale = defaultLocale): string {
  return `${localeCookieName}=${encodeURIComponent(locale)}; Path=/; SameSite=Lax; Max-Age=31536000`;
}
