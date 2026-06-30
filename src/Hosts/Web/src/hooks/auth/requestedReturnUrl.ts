export function getRequestedReturnUrl(
  searchParams: URLSearchParams,
  explicitReturnUrl?: string
): string | undefined {
  const requestedReturnUrl =
    explicitReturnUrl ?? searchParams.get("returnUrl") ?? undefined;

  if (!requestedReturnUrl || requestedReturnUrl === "/") {
    return undefined;
  }

  if (
    requestedReturnUrl === "/login" ||
    requestedReturnUrl.startsWith("/login/")
  ) {
    return undefined;
  }

  return requestedReturnUrl;
}
