"use client";

import { useState, type Dispatch, type ReactNode } from "react";
import { Alert, Button, Tooltip } from "@heroui/react";
import { Stepper } from "@heroui-pro/react/stepper";
import {
  AgentConversationComposer,
  InteractiveOptionPicker,
  InteractiveReferenceChips,
  InteractiveReplyMessage,
  type InteractiveQuestion,
  type InteractiveReference,
  type InteractiveReply,
} from "@vitarish/intelligence";
import { InlinePhoneSignIn } from "@vitarish/auth";
import { formatList, useLocalizedText } from "@vitarish/localization";
import { ChatConversation, ChatLoadingMessage, ChatMessage, FaIcon, MarketingActionLink, type MarketingAction } from "@vitarish/ui";
import { availabilityTone, type LandingCatalog } from "../use-landing-catalog";
import { whatsappChatUrl } from "../whatsapp";
import { ClinicPanel, ClinicPanelSheet } from "./clinic-panel";
import {
  consultationSteps,
  matchedFeatures,
  proofsFor,
  type ConsultationEvent,
  type ConsultationQuestionKey,
  type ConsultationState,
  type ThreadEntry,
} from "./consultation-state";
import { ProofCard } from "./proof-card";
import styles from "../front-door.module.css";

export interface ConsultationConversation {
  isPending: boolean;
  isError: boolean;
  retry: () => void;
}

/** Opening the owner's clinic: sign in here if needed, then hand off to sign-up. */
export interface OpenClinicFlow {
  isSignedIn: boolean;
  isOpening: boolean;
  isError: boolean;
  open: () => void;
  onSignedIn: () => void;
}

interface ConsultationViewProps {
  catalog: LandingCatalog;
  state: ConsultationState;
  dispatch: Dispatch<ConsultationEvent>;
  conversation: ConsultationConversation;
  onSay: (text: string) => void;
  onRestart: () => void;
  onboardingUrl: string | null;
  whatsappNumber: string | null;
  openClinicFlow: OpenClinicFlow;
}

/** The guided consultation: a fixed set of tappable steps, with the assistant answering anything typed in between. */
export function ConsultationView({
  catalog,
  state,
  dispatch,
  conversation,
  onSay,
  onRestart,
  onboardingUrl,
  whatsappNumber,
  openClinicFlow,
}: ConsultationViewProps) {
  const { t, direction, locale } = useLocalizedText();
  const [draft, setDraft] = useState("");
  const availability = new Map(catalog.features.map((feature) => [feature.key, feature.availability]));
  const optionLabel = (question: ConsultationQuestionKey, option: string) => t(`landing.consultation.options.${question}.${option}`);
  const kindLabel = state.answers.kind[0] ? optionLabel("kind", state.answers.kind[0]) : "";
  const currentStep = state.step ? consultationSteps.indexOf(state.step) : 0;

  const references = (keys: readonly string[]): InteractiveReference[] =>
    keys.flatMap((key) => {
      const status = availability.get(key);
      return status
        ? [{ key, label: t(`landing.features.${key}.title`), statusLabel: t(`landing.availability.${status}`), tone: availabilityTone(status) }]
        : [];
    });

  const openClinic: ReactNode = onboardingUrl ? (
    <Tooltip>
      <Button isPending={openClinicFlow.isOpening} onPress={openClinicFlow.open}>
        <FaIcon aria-hidden="true" icon="hospital" />
        {t("landing.consultation.openClinic")}
      </Button>
      <Tooltip.Content>{t("landing.consultation.openClinicTooltip")}</Tooltip.Content>
    </Tooltip>
  ) : null;
  const talkToTeam: MarketingAction | null = whatsappNumber
    ? {
        label: t("landing.consultation.talkToTeam"),
        href: whatsappChatUrl(
          whatsappNumber,
          t("landing.consultation.whatsappSummary", {
            kind: kindLabel,
            booking: formatList(state.answers.booking.map((option) => optionLabel("booking", option)), locale),
            pains: formatList(state.answers.pains.map((option) => optionLabel("pains", option)), locale),
          }),
        ),
        external: true,
        tone: "outline",
        tooltip: t("landing.consultation.talkToTeamTooltip"),
        icon: <i aria-hidden="true" className="fa-brands fa-whatsapp" />,
      }
    : null;
  const actionFor = (nextAction: string | null): ReactNode => {
    if (nextAction === catalog.nextActions.openClinic) return openClinic;
    if (nextAction === catalog.nextActions.talkToTeam && talkToTeam) return <MarketingActionLink action={talkToTeam} />;
    return null;
  };

  const stepQuestion = (question: ConsultationQuestionKey): InteractiveQuestion => ({
    prompt: t(`landing.consultation.questions.${question}`),
    options: catalog.consultation[question].options.map((option) => ({ key: option.key, label: optionLabel(question, option.key) })),
    maxSelections: Number(catalog.consultation[question].maxSelections),
  });

  const assistant = { avatarAlt: t("landing.consultation.thread.assistant"), avatarFallback: "V", kind: "answer" as const };

  const renderEntry = (entry: ThreadEntry) => {
    switch (entry.type) {
      case "opening":
        return <ChatMessage key={entry.id} kind="question">{t("landing.consultation.opening")}</ChatMessage>;
      case "question":
        return (
          <ChatMessage key={entry.id} {...assistant}>
            {entry.question === "kind" ? <p>{t("landing.consultation.intro")}</p> : null}
            <InteractiveOptionPicker
              answered={state.answers[entry.question]}
              question={stepQuestion(entry.question)}
              onAnswer={(options) => dispatch({ type: "answer", question: entry.question, options })}
            />
          </ChatMessage>
        );
      case "answer":
        return (
          <ChatMessage key={entry.id} kind="question">
            {formatList(entry.options.map((option) => optionLabel(entry.question, option)), locale)}
          </ChatMessage>
        );
      case "proofs":
        return (
          <ChatMessage key={entry.id} {...assistant}>
            <p>{t("landing.consultation.proofIntro", { kind: kindLabel })}</p>
            <p className={styles.matched}>
              <FaIcon aria-hidden="true" icon="link" />
              {t("landing.consultation.matched")}
            </p>
            <InteractiveReferenceChips items={references(matchedFeatures(catalog.consultation, state.answers.pains))} />
            <div className={styles.proofs}>
              {proofsFor(catalog.consultation, state.answers.pains).map((kind) => (
                <ProofCard key={kind} kind={kind} kindLabel={kindLabel} />
              ))}
            </div>
            {state.step === "proof" ? (
              <Tooltip>
                <Button className={styles.fitContent} onPress={() => dispatch({ type: "confirm" })}>
                  <FaIcon aria-hidden="true" icon="check" />
                  {t("landing.consultation.continue")}
                </Button>
                <Tooltip.Content>{t("landing.consultation.continueTooltip")}</Tooltip.Content>
              </Tooltip>
            ) : null}
          </ChatMessage>
        );
      case "confirmed":
        return <ChatMessage key={entry.id} kind="question">{t("landing.consultation.continued")}</ChatMessage>;
      case "ready":
        return (
          <ChatMessage key={entry.id} {...assistant}>
            <p>{t("landing.consultation.ready")}</p>
            <div className={styles.actions}>
              {openClinic}
              {talkToTeam ? <MarketingActionLink action={talkToTeam} /> : null}
            </div>
          </ChatMessage>
        );
      case "signIn":
        return (
          <ChatMessage key={entry.id} {...assistant}>
            {openClinicFlow.isSignedIn ? (
              <p>{t("landing.consultation.signIn.title")}</p>
            ) : (
              <InlinePhoneSignIn
                description={t("landing.consultation.signIn.description")}
                title={t("landing.consultation.signIn.title")}
                onSignedIn={openClinicFlow.onSignedIn}
              />
            )}
          </ChatMessage>
        );
      case "signedIn":
        return (
          <ChatMessage key={entry.id} {...assistant}>
            <p>{t("landing.consultation.signIn.done")}</p>
          </ChatMessage>
        );
      case "user":
        return <ChatMessage key={entry.id} kind="question">{entry.text}</ChatMessage>;
      case "reply":
        return (
          <InteractiveReplyMessage
            key={entry.id}
            avatarAlt={assistant.avatarAlt}
            avatarFallback={assistant.avatarFallback}
            action={actionFor(entry.reply.nextAction)}
            references={references(entry.reply.references)}
            reply={entry.reply}
            onAnswer={(options) => onSay(formatList(optionsLabels(entry.reply, options), locale))}
          />
        );
    }
  };

  return (
    <div className={styles.consultation}>
      <section aria-label={t("landing.consultation.label")} className={styles.thread}>
        <div className={styles.threadTop}>
          <Stepper aria-label={t("landing.consultation.steps.label")} currentStep={currentStep}>
            {consultationSteps.map((step) => (
              <Stepper.Step key={step}>
                <Stepper.Indicator />
                <Stepper.Content>
                  <Stepper.Title>{t(`landing.consultation.steps.${step}`)}</Stepper.Title>
                </Stepper.Content>
                <Stepper.Separator />
              </Stepper.Step>
            ))}
          </Stepper>
          <div className={styles.threadTools}>
            <div className={styles.sheetOnly}>
              <ClinicPanelSheet catalog={catalog} state={state} />
            </div>
            <Tooltip>
              <Button size="sm" variant="ghost" onPress={onRestart}>
                <FaIcon aria-hidden="true" icon="rotate-left" />
                {t("landing.consultation.thread.restart")}
              </Button>
              <Tooltip.Content>{t("landing.consultation.thread.restartTooltip")}</Tooltip.Content>
            </Tooltip>
          </div>
        </div>
        <ChatConversation className={styles.messages} innerClassName={styles.messagesInner}>
          {state.thread.map(renderEntry)}
          {conversation.isPending ? (
            <ChatLoadingMessage
              avatarAlt={assistant.avatarAlt}
              avatarFallback={assistant.avatarFallback}
              loadingLabel={t("landing.consultation.thread.thinking")}
              message={t("landing.consultation.thread.thinkingBody")}
              title={t("landing.consultation.thread.thinking")}
            />
          ) : null}
        </ChatConversation>
        {openClinicFlow.isError ? (
          <Alert status="danger">
            <Alert.Indicator />
            <Alert.Content>
              <Alert.Description>{t("landing.consultation.signIn.openError")}</Alert.Description>
            </Alert.Content>
            <div className={styles.actions}>
              <Tooltip>
                <Button size="sm" variant="ghost" onPress={openClinicFlow.open}>
                  <FaIcon aria-hidden="true" icon="rotate-right" />
                  {t("landing.consultation.signIn.retry")}
                </Button>
                <Tooltip.Content>{t("landing.consultation.signIn.retryTooltip")}</Tooltip.Content>
              </Tooltip>
              {onboardingUrl ? (
                <MarketingActionLink
                  action={{
                    label: t("landing.consultation.signIn.continue"),
                    href: onboardingUrl,
                    tone: "outline",
                    tooltip: t("landing.consultation.signIn.continueTooltip"),
                    icon: <FaIcon aria-hidden="true" icon="hospital" />,
                  }}
                />
              ) : null}
            </div>
          </Alert>
        ) : null}
        {conversation.isError ? (
          <Alert status="danger">
            <Alert.Indicator />
            <Alert.Content>
              <Alert.Description>{t("landing.consultation.thread.error")}</Alert.Description>
            </Alert.Content>
            <Tooltip>
              <Button size="sm" variant="ghost" onPress={conversation.retry}>
                <FaIcon aria-hidden="true" icon="rotate-right" />
                {t("landing.consultation.thread.retry")}
              </Button>
              <Tooltip.Content>{t("landing.consultation.thread.retryTooltip")}</Tooltip.Content>
            </Tooltip>
          </Alert>
        ) : null}
        <AgentConversationComposer
          dictation={false}
          direction={direction}
          isPending={conversation.isPending}
          label={t("landing.consultation.thread.composerLabel")}
          placeholder={t("landing.consultation.thread.composerPlaceholder")}
          preferredLanguage={direction === "rtl" ? "fa-IR" : "en-US"}
          sessionKey="landing-consultation"
          submitLabel={t("landing.consultation.thread.send")}
          value={draft}
          onChange={setDraft}
          onSubmit={(text) => {
            setDraft("");
            onSay(text);
          }}
        />
      </section>
      <div className={styles.panelColumn}>
        <ClinicPanel catalog={catalog} state={state} />
      </div>
    </div>
  );
}

function optionsLabels(reply: InteractiveReply, keys: readonly string[]): string[] {
  return keys.flatMap((key) => reply.question?.options.find((option) => option.key === key)?.label ?? []);
}
