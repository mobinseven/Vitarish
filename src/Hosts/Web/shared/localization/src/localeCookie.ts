import { defaultLocale, type SupportedLocale, toSupportedLocale } from "./locales";

export const localeCookieName = "vitarish.locale";

/**
 * The runtime environment variable naming the domain every host of a deployment shares, such as
 * `vitarish.com`. The locale cookie is scoped to it, so a language chosen on one host (the landing
 * page) holds on the others (the clinic app, its onboarding host, each clinic). Unset keeps the
 * cookie on the current host, as local development needs.
 */
export const localeCookieDomainEnvironmentKey = "VITARISH_LOCALE_COOKIE_DOMAIN";

const domainPattern = /^[a-z0-9](?:[a-z0-9.-]*[a-z0-9])?$/;

/** The shared cookie domain from the server environment, or null when unset or not a plain domain name. */
export function readLocaleCookieDomain(environment: Record<string, string | undefined>): string | null {
  const value = environment[localeCookieDomainEnvironmentKey]?.trim().toLowerCase();
  return value && domainPattern.test(value) ? value : null;
}

export function parseLocaleCookie(cookieHeader: string | null | undefined): SupportedLocale | null {
  if (!cookieHeader) return null;
  const pair = cookieHeader.split(";").map((part) => part.trim()).find((part) => part.startsWith(`${localeCookieName}=`));
  if (!pair) return null;
  try {
    return toSupportedLocale(decodeURIComponent(pair.slice(localeCookieName.length + 1)));
  } catch {
    return null;
  }
}

export function serializeLocaleCookie(locale: SupportedLocale = defaultLocale, domain?: string | null): string {
  const scope = domain ? `; Domain=${domain}` : "";
  return `${localeCookieName}=${encodeURIComponent(locale)}; Path=/${scope}; SameSite=Lax; Max-Age=31536000`;
}

export function setLocaleCookie(locale: SupportedLocale, domain?: string | null): void {
  // A host-only cookie from before the shared domain would shadow the shared one on this host.
  if (domain) document.cookie = `${localeCookieName}=; Path=/; Max-Age=0`;
  document.cookie = serializeLocaleCookie(locale, domain);
}
