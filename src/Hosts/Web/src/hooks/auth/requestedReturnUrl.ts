export function getRequestedReturnUrl(
  searchParams: URLSearchParams,
  explicitReturnUrl?: string
): string | undefined {
  const raw =
    explicitReturnUrl ?? searchParams.get("returnUrl") ?? undefined;

  if (!raw) return undefined;

  const requestedReturnUrl = raw.trim();

  if (!requestedReturnUrl || requestedReturnUrl === "/") {
    return undefined;
  }

  const path = requestedReturnUrl.split(/[?#]/)[0];

  if (
    path === "/login" ||
    path.startsWith("/login/") ||
    path === "/logout" ||
    path.startsWith("/logout/")
  ) {
    return undefined;
  }

  return requestedReturnUrl;
}
