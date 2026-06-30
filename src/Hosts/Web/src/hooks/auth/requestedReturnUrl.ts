export function getRequestedReturnUrl(
  searchParams: URLSearchParams,
  explicitReturnUrl?: string
): string | null {
  const raw =
    explicitReturnUrl ?? searchParams.get("returnUrl") ?? undefined;

  if (!raw) return null;

  const requestedReturnUrl = raw.trim();

  if (!requestedReturnUrl || requestedReturnUrl === "/") {
    return null;
  }

  const path = requestedReturnUrl.split(/[?#]/)[0];

  if (
    path === "/login" ||
    path.startsWith("/login/") ||
    path === "/logout" ||
    path.startsWith("/logout/")
  ) {
    return null;
  }

  return requestedReturnUrl;
}
