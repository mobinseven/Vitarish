"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { Link, Spinner, Tooltip } from "@heroui/react";
import { withoutNavigationTransition } from "@vitarish/ui/navigation-context";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import { carryNavigationTransition, takeNavigationTransition, isCurrentNavigationTransition, navigationTransitionSignal } from "@vitarish/ui/navigation";
import { navigationTransitionStates, useNavigationTransitionParticipant } from "@vitarish/ui/navigation-runtime";
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
  const covered = useNavigationTransitionParticipant(!failed, failed ? navigationTransitionStates.Failed : undefined);

  const continueHome = useCallback(() => {
    setFailed(false);
    const attempt = takeNavigationTransition();
    const rawRequested = new URLSearchParams(window.location.search).get(signInReturnUrlQueryKey);
    const requested = rawRequested ? withoutNavigationTransition(rawRequested) : null;
    setSafeReturnUrl(isSafeRelativeReturnTo(requested) ? requested : undefined);
    partiesIssueSignInHomeHandoff({ returnTo: requested }, undefined, navigationTransitionSignal())
      .then((handoff) => { if (isCurrentNavigationTransition(attempt)) window.location.assign(carryNavigationTransition(handoff.handoffUrl ?? handoffUrl(handoff.hostname, tenantHandoffPath, handoff.token, handoff.returnTo))); })
      .catch(() => { if (isCurrentNavigationTransition(attempt)) setFailed(true); });
  }, []);

  useEffect(() => {
    // Start one home entry per mount, including React strict-mode effect replay.
    if (started.current) return;
    started.current = true;
    continueHome();
  }, [continueHome]);

  if (!failed && covered) return null;
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
