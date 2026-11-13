const signInPath = "/login";

/** Routes shared by the authentication surfaces in every Web app. */
export const AuthenticationWebRoutes = {
  SignIn: signInPath,
  SignInPassword: signInPath + "/password",
  SignInPhone: signInPath + "/phone",
  SignInEmail: signInPath + "/email",
  SignInPasskey: signInPath + "/passkey",
  PasswordRecovery: signInPath + "/forgot-password",
  Continue: "/continue",
  Handoff: "/handoff",
  Account: "/account",
  AccountSecurity: "/account/security",
} as const;

export const AuthenticationSearchParams = {
  ReturnUrl: "returnUrl",
  Request: "request",
} as const;

/**
 * A sign-in link that brings the person back to `returnPath` afterwards: the current page's path and query on
 * this host. Anything that is not a local path is left out; the server validates the return target again.
 */
export function signInHrefReturningTo(returnPath: string | null | undefined, signInHref: string = AuthenticationWebRoutes.SignIn): string {
  if (!returnPath || !returnPath.startsWith("/") || returnPath.startsWith("//") || returnPath.startsWith("/\\")) return signInHref;
  const separator = signInHref.includes("?") ? "&" : "?";
  return `${signInHref}${separator}${AuthenticationSearchParams.ReturnUrl}=${encodeURIComponent(returnPath)}`;
}

/** The current page as a sign-in return target: its path and query on this host. */
export function currentReturnPath(): string | undefined {
  return typeof window === "undefined" ? undefined : window.location.pathname + window.location.search;
}
