"use client";

import { useCallback, useEffect, useReducer, useState } from "react";
import { Alert, Spinner } from "@heroui/react";
import { useAuthSession } from "@vitarish/auth";
import { onboardingHandoffUrl } from "@vitarish/journey/onboarding-host";
import { useLocalizedText } from "@vitarish/localization";
import { landingIssueOnboardingHandoff } from "@vitarish/platform";
import { MarketingFooter } from "@vitarish/ui";
import { ConsultationView } from "./consultation/consultation-view";
import { consultationReducer, initialConsultation, pendingSignIn } from "./consultation/consultation-state";
import { FrontDoorHeader, FrontDoorHero } from "./front-door-hero";
import { DayAtClinic, Roadmap, Spotlight, TrustSection } from "./front-door-sections";
import { useFrontDoorConversation } from "./use-front-door-conversation";
import { useLandingCatalog } from "./use-landing-catalog";
import styles from "./front-door.module.css";

interface FrontDoorPageProps {
  /** Where people create their own clinic; null when this deployment offers none. */
  onboardingUrl: string | null;
  /** The WhatsApp number that answers "talk to a person"; null when none is configured. */
  whatsappNumber: string | null;
}

/**
 * The public front door. Visitors start on a single question; once they ask something or choose
 * "I run a clinic", the page becomes the consultation, and the scrolling sections give way to it.
 */
export function FrontDoorPage({ onboardingUrl, whatsappNumber }: FrontDoorPageProps) {
  const { t } = useLocalizedText();
  const catalog = useLandingCatalog();
  const conversation = useFrontDoorConversation();
  const [state, dispatch] = useReducer(consultationReducer, initialConsultation);
  const isConsulting = state.thread.length > 0;
  const session = useAuthSession();
  const [hasSignedInHere, setSignedInHere] = useState(false);
  const isSignedIn = hasSignedInHere || session.isAuthenticated;
  const [isOpening, setOpening] = useState(false);
  const [isOpenError, setOpenError] = useState(false);

  // A one-time ticket signs the person in on the onboarding host, so they do not sign in twice.
  const handOff = useCallback(async () => {
    if (!onboardingUrl) return;
    setOpening(true);
    setOpenError(false);
    try {
      const handoff = await landingIssueOnboardingHandoff();
      window.location.assign(onboardingHandoffUrl(onboardingUrl, handoff.token));
    } catch {
      setOpening(false);
      setOpenError(true);
    }
  }, [onboardingUrl]);

  const openClinic = useCallback(() => {
    if (isSignedIn) void handOff();
    else dispatch({ type: "requestSignIn", reason: "clinic" });
  }, [handOff, isSignedIn]);

  const tryVoice = useCallback(() => {
    if (!isSignedIn) dispatch({ type: "requestSignIn", reason: "voice" });
  }, [isSignedIn]);

  // Signing in for the clinic continues to sign-up; signing in for voice just turns the microphone on.
  const onSignedIn = useCallback(() => {
    const reason = pendingSignIn(state)?.reason;
    setSignedInHere(true);
    dispatch({ type: "signedIn" });
    if (reason === "clinic") void handOff();
  }, [handOff, state]);

  useEffect(() => {
    if (isConsulting) window.scrollTo({ top: 0 });
  }, [isConsulting]);

  const say = useCallback(
    async (text: string) => {
      const message = text.trim();
      if (!message || conversation.isPending) return;
      dispatch({ type: "said", text: message });
      const reply = await conversation.submit(message);
      if (reply) dispatch({ type: "replied", reply });
    },
    [conversation],
  );

  const retry = useCallback(async () => {
    const reply = await conversation.retry();
    if (reply) dispatch({ type: "replied", reply });
  }, [conversation]);

  const restart = useCallback(() => {
    conversation.reset();
    dispatch({ type: "restart" });
  }, [conversation]);

  return (
    <div className={styles.page}>
      <FrontDoorHeader />
      <main className={styles.main} id="vitarish-landing-main">
        {isConsulting ? (
          catalog.data ? (
            <ConsultationView
              catalog={catalog.data}
              conversation={{ isPending: conversation.isPending, isError: conversation.isError, retry: () => void retry() }}
              dispatch={dispatch}
              state={state}
              onRestart={restart}
              onSay={(text) => void say(text)}
              onboardingUrl={onboardingUrl}
              signInFlow={{ isSignedIn, isOpening, isError: isOpenError, openClinic, tryVoice, onSignedIn }}
              whatsappNumber={whatsappNumber}
            />
          ) : catalog.isError ? (
            <Alert className={styles.sectionBody} status="danger">
              <Alert.Indicator />
              <Alert.Content>
                <Alert.Description>{t("landing.roadmap.error")}</Alert.Description>
              </Alert.Content>
            </Alert>
          ) : (
            <div className={styles.loading}>
              <Spinner aria-label={t("landing.roadmap.loading")} />
            </div>
          )
        ) : (
          <>
            <FrontDoorHero
              isPending={conversation.isPending}
              isSignedIn={isSignedIn}
              onAsk={(text) => void say(text)}
              onStartClinic={() => dispatch({ type: "start" })}
            />
            <Spotlight />
            <DayAtClinic onAsk={(text) => void say(text)} />
            <TrustSection />
            <Roadmap />
          </>
        )}
      </main>
      <MarketingFooter
        brand={<span className={styles.footerBrand}>{t("landing.brand.name")}</span>}
        description={t("landing.footer.description")}
        navigationLabel={t("landing.footer.navigation")}
      />
    </div>
  );
}
