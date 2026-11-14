"use client";

import { useEffect, useRef, useState } from "react";
import { Button, Spinner, Tooltip } from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import { carryNavigationTransition, takeNavigationTransition, isCurrentNavigationTransition, navigationTransitionSignal } from "@vitarish/ui/navigation";
import { navigationTransitionStates, useNavigationTransitionParticipant } from "@vitarish/ui/navigation-runtime";
import { authenticationTenantHandoffRedeem } from "../api/generated/authentication/authentication";
import { JourneyGateCard } from "../components/JourneyGateCard";
import { JourneyGateHeader } from "../components/JourneyGateHeader";
import { JourneyGateLayout } from "../components/JourneyGateLayout";

interface TenantHandoffPageProps {
  /** Where to sign in normally when the link cannot be used. */
  signInPath: string;
  /** Where to go once signed in; when omitted, the server's journey decides. */
  destination?: string;
}

/** Takes the one-time token from the URL fragment, removes it from the address bar, and redeems it. */
function takeTokenFromFragment(): string | null {
  const token = window.location.hash.replace(/^#/, "");
  window.history.replaceState(null, "", window.location.pathname + window.location.search);
  return token === "" ? null : token;
}

/** Signs a new clinic's creator in on its own address with the one-time link from setup. */
export function TenantHandoffPage({ signInPath, destination }: TenantHandoffPageProps) {
  const { t } = useLocalizedText();
  const [failed, setFailed] = useState(false);
  const started = useRef(false);
  const covered = useNavigationTransitionParticipant(!failed, failed ? navigationTransitionStates.Failed : undefined);

  useEffect(() => {
    // The token works once, so a second effect run (React strict mode) must not spend it again.
    if (started.current) return;
    started.current = true;
    const attempt = takeNavigationTransition();
    const token = takeTokenFromFragment();
    if (!token) {
      setFailed(true);
      return;
    }
    authenticationTenantHandoffRedeem({ token }, undefined, navigationTransitionSignal())
      // A full load picks up the new session everywhere in the app.
      .then((result) => { if (isCurrentNavigationTransition(attempt)) window.location.assign(carryNavigationTransition(destination ?? result.returnUrl)); })
      .catch(() => { if (isCurrentNavigationTransition(attempt)) setFailed(true); });
  }, [destination]);

  if (!failed && covered) return null;
  return (
    <JourneyGateLayout>
      <JourneyGateCard
        header={
          <JourneyGateHeader
            title={failed ? t("journey.handoff.failedTitle") : t("journey.handoff.title")}
            description={failed ? t("journey.handoff.failedDescription") : t("journey.handoff.description")}
          />
        }
      >
        {failed ? (
          <Tooltip>
            <Button className="self-start" onPress={() => window.location.assign(signInPath)}>
              <FaIcon icon="arrow-right-to-bracket" aria-hidden="true" className="rtl:-scale-x-100" />
              {t("journey.handoff.signIn")}
            </Button>
            <Tooltip.Content>{t("journey.handoff.signIn")}</Tooltip.Content>
          </Tooltip>
        ) : (
          <div className="flex items-center gap-3 text-base text-muted" aria-live="polite">
            <Spinner size="sm" />
            {t("journey.handoff.signingIn")}
          </div>
        )}
      </JourneyGateCard>
    </JourneyGateLayout>
  );
}
