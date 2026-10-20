"use client";

import { useEffect, useRef, useState } from "react";
import { Button, Spinner, Tooltip } from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import { authenticationTenantHandoffRedeem } from "../api/generated/authentication/authentication";
import { JourneyGateCard } from "../components/JourneyGateCard";
import { JourneyGateHeader } from "../components/JourneyGateHeader";
import { JourneyGateLayout } from "../components/JourneyGateLayout";

interface TenantHandoffPageProps {
  /** Where to sign in normally when the link cannot be used. */
  signInPath: string;
}

/** Takes the one-time token from the URL fragment, removes it from the address bar, and redeems it. */
function takeTokenFromFragment(): string | null {
  const token = window.location.hash.replace(/^#/, "");
  window.history.replaceState(null, "", window.location.pathname + window.location.search);
  return token === "" ? null : token;
}

/** Signs a new clinic's creator in on its own address with the one-time link from setup. */
export function TenantHandoffPage({ signInPath }: TenantHandoffPageProps) {
  const { t } = useLocalizedText();
  const [failed, setFailed] = useState(false);
  const started = useRef(false);

  useEffect(() => {
    // The token works once, so a second effect run (React strict mode) must not spend it again.
    if (started.current) return;
    started.current = true;
    const token = takeTokenFromFragment();
    if (!token) {
      setFailed(true);
      return;
    }
    authenticationTenantHandoffRedeem({ token })
      // A full load picks up the new session everywhere in the app.
      .then((result) => window.location.assign(result.returnUrl))
      .catch(() => setFailed(true));
  }, []);

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
