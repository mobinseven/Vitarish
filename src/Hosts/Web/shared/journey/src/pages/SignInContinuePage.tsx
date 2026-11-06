"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { Button, Link, Spinner, Tooltip } from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import { partiesIssueSignInHomeHandoff } from "../api/generated/parties/parties";
import { JourneyGateCard } from "../components/JourneyGateCard";
import { JourneyGateHeader } from "../components/JourneyGateHeader";
import { JourneyGateLayout } from "../components/JourneyGateLayout";
import { handoffUrl } from "../features/handoff/hostUrl";
import { tenantHandoffPath } from "../onboardingHost";
import { isSafeRelativeReturnTo, signInReturnUrlQueryKey } from "../signInHost";

interface SignInContinuePageProps {
  /** The owning host's sign-in route, used if issuing the home link fails after ending the session. */
  signInPath: string;
}

/**
 * The shared sign-in host's last step: it asks the server for a one-time link to the person's
 * platform home and leaves. The server picks the home host and checks the requested return path.
 */
export function SignInContinuePage({ signInPath }: SignInContinuePageProps) {
  const { t } = useLocalizedText();
  const [failed, setFailed] = useState(false);
  const [safeReturnUrl, setSafeReturnUrl] = useState<string | undefined>();
  const started = useRef(false);

  const continueHome = useCallback(() => {
    setFailed(false);
    const requested = new URLSearchParams(window.location.search).get(signInReturnUrlQueryKey);
    setSafeReturnUrl(isSafeRelativeReturnTo(requested) ? requested : undefined);
    partiesIssueSignInHomeHandoff({ returnTo: requested })
      .then((handoff) => window.location.assign(handoffUrl(handoff.hostname, tenantHandoffPath, handoff.token, handoff.returnTo)))
      .catch(() => setFailed(true));
  }, []);

  useEffect(() => {
    // Issuing the link ends the session here, so a second effect run (React strict mode) must not repeat it.
    if (started.current) return;
    started.current = true;
    continueHome();
  }, [continueHome]);

  return (
    <JourneyGateLayout>
      <JourneyGateCard
        header={
          <JourneyGateHeader
            title={failed ? t("journey.continue.failedTitle") : t("journey.continue.title")}
            description={failed ? t("journey.continue.failedDescription") : t("journey.continue.description")}
          />
        }
      >
        {failed ? (
          <div className="flex flex-wrap gap-3">
            <Tooltip>
              <Button className="self-start" onPress={continueHome}>
                <FaIcon icon="rotate-right" aria-hidden="true" />
                {t("journey.continue.retry")}
              </Button>
              <Tooltip.Content>{t("journey.continue.retry")}</Tooltip.Content>
            </Tooltip>
            <Tooltip>
              <Link
                className="inline-flex items-center gap-2 self-start"
                href={safeReturnUrl
                  ? `${signInPath}?${new URLSearchParams({ [signInReturnUrlQueryKey]: safeReturnUrl })}`
                  : signInPath}
              >
                <FaIcon icon="arrow-right-to-bracket" aria-hidden="true" className="rtl:-scale-x-100" />
                {t("journey.continue.signInAgain")}
              </Link>
              <Tooltip.Content>{t("journey.continue.signInAgain")}</Tooltip.Content>
            </Tooltip>
          </div>
        ) : (
          <div className="flex items-center gap-3 text-base text-muted" aria-live="polite">
            <Spinner size="sm" />
            {t("journey.continue.opening")}
          </div>
        )}
      </JourneyGateCard>
    </JourneyGateLayout>
  );
}
