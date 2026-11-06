"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { Button, Spinner } from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import { partiesIssueSignInHomeHandoff } from "../api/generated/parties/parties";
import { JourneyGateCard } from "../components/JourneyGateCard";
import { JourneyGateHeader } from "../components/JourneyGateHeader";
import { JourneyGateLayout } from "../components/JourneyGateLayout";
import { handoffUrl } from "../features/handoff/hostUrl";
import { tenantHandoffPath } from "../onboardingHost";
import { signInReturnUrlQueryKey } from "../signInHost";

/**
 * The shared sign-in host's last step: it asks the server for a one-time link to the person's
 * platform home and leaves. The server picks the home host and checks the requested return path.
 */
export function SignInContinuePage() {
  const { t } = useLocalizedText();
  const [failed, setFailed] = useState(false);
  const started = useRef(false);

  const continueHome = useCallback(() => {
    setFailed(false);
    const requested = new URLSearchParams(window.location.search).get(signInReturnUrlQueryKey);
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
          <Button className="self-start" onPress={continueHome}>
            <FaIcon icon="rotate-right" aria-hidden="true" />
            {t("journey.continue.retry")}
          </Button>
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
