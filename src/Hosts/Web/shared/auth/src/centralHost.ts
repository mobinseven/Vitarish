import { AuthenticationSearchParams } from "@vitarish/auth/routes";
/**
 * The runtime environment variable naming the shared sign-in host's address (ADR-0018), the one
 * place people sign in. Hosts read it on the server per request, so one image serves every
 * environment; the deployment sets it from the same value as Main's `Vitarish:SignInUrl`.
 */
export const signInUrlEnvironmentKey = "VITARISH_SIGN_IN_URL";
/** The sign-in pages' requested-return query key, matching Journey's `JourneyPolicy.ReturnUrlQueryKey`. */
export const signInReturnUrlQueryKey = AuthenticationSearchParams.ReturnUrl;

/** The sign-in address from the server environment, or null when none is set. */
export function readSignInUrl(environment: Record<string, string | undefined>): string | null {
  const value = environment[signInUrlEnvironmentKey]?.trim();
  return value ? value : null;
}

/**
 * Where a sign-in page requested on `host` (`name[:port]`) continues: `pathname` on the sign-in
 * host, preserving only a safe relative return path from its query, or null when `host` already
 * is the sign-in host. The target is always the sign-in host.
 */
/** True when `host` (`name[:port]`) is the shared sign-in host itself. */
export function isSignInHost(signInUrl: string, host: string | null | undefined): boolean {
  return host?.trim().toLowerCase() === new URL(signInUrl).host;
}

export function signInHostRedirectUrl(signInUrl: string, host: string | null | undefined, pathname: string): string | null {
  const target = new URL(signInUrl);
  if (isSignInHost(signInUrl, host)) return null;

  const requested = new URL(pathname, target);
  target.pathname = requested.pathname;
  target.search = "";
  target.hash = "";
  const returnTo = requested.searchParams.get(signInReturnUrlQueryKey);
  if (isSafeRelativeReturnTo(returnTo)) {
    target.searchParams.set(signInReturnUrlQueryKey, returnTo);
  }
  return target.toString();
}

/** True when `value` is a same-host relative path with no query or fragment, safe to navigate to. */
export function isSafeRelativeReturnTo(value: string | null | undefined): value is string {
  if (!value) return false;

  let decoded: string;
  try {
    decoded = decodeURIComponent(value);
  } catch {
    return false;
  }

  return !/[\u0000-\u001F\u007F\s]/u.test(decoded)
    && decoded.startsWith("/")
    && !decoded.startsWith("//")
    && !decoded.startsWith("/\\")
    && !decoded.includes("\\")
    && !decoded.includes("?")
    && !decoded.includes("#");
}
