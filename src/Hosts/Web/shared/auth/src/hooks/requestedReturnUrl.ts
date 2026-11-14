import { withoutNavigationTransition } from "@vitarish/ui/navigation-context";
import { AuthenticationSearchParams } from "../routes";

export function getRequestedReturnUrl(
  searchParams: Pick<URLSearchParams, "get"> | undefined,
  explicitReturnUrl?: string
): string | undefined {
  const raw =
    explicitReturnUrl ?? searchParams?.get(AuthenticationSearchParams.ReturnUrl) ?? undefined;

  if (!raw) return undefined;

  const requestedReturnUrl = withoutNavigationTransition(raw.trim());

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
