"use client";

import { ApiError } from "@vitarish/api-client";
import { useLocalizedText } from "@vitarish/localization";
import { StatusAlert } from "@vitarish/ui";
import { StandaloneCenteredPage } from "@vitarish/ui/layout";
import { navigationTransitionMessages } from "@vitarish/ui/navigation-messages";
import { parseAuthError } from "../auth/errors";
import { useAuthErrorDisplay } from "../hooks/useAuthErrorDisplay";
import { AuthenticationWebRoutes } from "../routes";

/** Displays the existing transport/authentication outcome without choosing an access policy. */
export function AuthenticationEntryFailure({ error, signInHref = AuthenticationWebRoutes.SignIn }: { error?: unknown; signInHref?: string }) {
  const { t, locale } = useLocalizedText();
  const signInLabel = navigationTransitionMessages[locale].signIn;
  const failure = error ? parseAuthError(error) : null;
  const message = useAuthErrorDisplay(failure, error instanceof ApiError ? error.message : t("auth.failures.providerFailure"));
  return <StandaloneCenteredPage>
    <div className="flex max-w-md flex-col gap-4">
      <StatusAlert message={message ?? t("auth.failures.providerFailure")} />
      <a className="inline-flex items-center gap-2 text-accent underline" href={signInHref} title={signInLabel}>
        <i className="fa-light fa-arrow-right-to-bracket rtl:-scale-x-100" aria-hidden="true" />
        {signInLabel}
      </a>
    </div>
  </StandaloneCenteredPage>;
}
