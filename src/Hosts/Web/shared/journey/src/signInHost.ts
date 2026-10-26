/**
 * The runtime environment variable naming the shared sign-in host's address (ADR-0018), the one
 * place people sign in. Hosts read it on the server per request, so one image serves every
 * environment; the deployment sets it from the same value as Main's `Vitarish:SignInUrl`.
 */
export const signInUrlEnvironmentKey = "VITARISH_SIGN_IN_URL";

/** The sign-in address from the server environment, or null when none is set. */
export function readSignInUrl(environment: Record<string, string | undefined>): string | null {
  const value = environment[signInUrlEnvironmentKey]?.trim();
  return value ? value : null;
}

/**
 * Where a sign-in page requested on `host` (`name[:port]`) continues: `pathname` on the sign-in
 * host, or null when `host` already is the sign-in host. Only the path is set, so the target is
 * always the sign-in host.
 */
export function signInHostRedirectUrl(signInUrl: string, host: string | null | undefined, pathname: string): string | null {
  const target = new URL(signInUrl);
  if (host?.trim().toLowerCase() === target.host) return null;
  target.pathname = pathname;
  return target.toString();
}
