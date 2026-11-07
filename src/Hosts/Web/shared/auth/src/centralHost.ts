/** Server runtime setting shared with Main's central sign-in configuration. */
export const signInUrlEnvironmentKey = "VITARISH_SIGN_IN_URL";

export function readSignInUrl(environment: Record<string, string | undefined>): string | null {
  return environment[signInUrlEnvironmentKey]?.trim() || null;
}

export function signInHostRedirectUrl(signInUrl: string, host: string | null | undefined, pathname: string): string | null {
  const target = new URL(signInUrl);
  if (host?.trim().toLowerCase() === target.host) return null;
  target.pathname = pathname;
  return target.toString();
}
