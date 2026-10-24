"use client";

import { useState } from "react";
import { PromptSuggestion } from "@heroui-pro/react";
import { AgentConversationComposer } from "@vitarish/intelligence";
import { setLocaleCookie, supportedLocales, useLocalizedText } from "@vitarish/localization";
import { FaIcon, HeroHeader, MarketingHeader } from "@vitarish/ui";
import styles from "./front-door.module.css";

export function FrontDoorHeader() {
  const { t, locale } = useLocalizedText();
  const otherLocale = supportedLocales.find((candidate) => candidate !== locale);

  return (
    <MarketingHeader
      actions={otherLocale ? [
        {
          label: t("landing.header.language"),
          tone: "quiet",
          tooltip: t("landing.header.languageTooltip"),
          icon: <FaIcon aria-hidden="true" icon="language" />,
          onPress: () => {
            setLocaleCookie(otherLocale);
            window.location.reload();
          },
        },
      ] : []}
      brand={
        <a aria-label={t("landing.brand.home")} className={styles.brand} href="/">
          <img alt="" height={32} src="/512.webp" width={32} />
          <span>{t("landing.brand.name")}</span>
        </a>
      }
      className={styles.header}
      navigationLabel={t("landing.header.navigation")}
    />
  );
}

/**
 * The opening screen: one question, one composer, and a few ways in. Choosing "I run a clinic"
 * starts the guided consultation; anything else goes to the assistant as a message.
 */
export function FrontDoorHero({
  isPending,
  isSignedIn,
  onAsk,
  onStartClinic,
}: {
  isPending: boolean;
  /** Dictation needs a session, so it is offered only once the visitor has signed in. */
  isSignedIn: boolean;
  onAsk: (message: string) => void;
  onStartClinic: () => void;
}) {
  const { t, direction } = useLocalizedText();
  const [value, setValue] = useState("");
  const starters = [
    { key: "day", icon: "sun", onPress: () => onAsk(t("landing.hero.starters.day")) },
    { key: "booking", icon: "calendar-check", onPress: () => onAsk(t("landing.hero.starters.booking")) },
  ] as const;

  return (
    <HeroHeader className={styles.hero} variant="centered">
      <HeroHeader.Content className={styles.heroContent}>
        <HeroHeader.Title className={styles.heroTitle}>{t("landing.hero.title")}</HeroHeader.Title>
        <div className={styles.heroComposer}>
          <AgentConversationComposer
            dictation={isSignedIn}
            direction={direction}
            isPending={isPending}
            label={t("landing.hero.promptLabel")}
            placeholder={t("landing.hero.placeholder")}
            preferredLanguage={direction === "rtl" ? "fa-IR" : "en-US"}
            sessionKey="landing-front-door"
            submitLabel={t("landing.hero.send")}
            value={value}
            onChange={setValue}
            onSubmit={(text) => {
              setValue("");
              onAsk(text);
            }}
          />
        </div>
        <PromptSuggestion aria-label={t("landing.hero.startersLabel")} className={styles.starters}>
          <PromptSuggestion.Items>
            <PromptSuggestion.Item className={styles.starter} onPress={onStartClinic}>
              <FaIcon aria-hidden="true" icon="hospital" />
              <PromptSuggestion.ItemTitle>{t("landing.hero.starters.clinic")}</PromptSuggestion.ItemTitle>
            </PromptSuggestion.Item>
            {starters.map((starter) => (
              <PromptSuggestion.Item key={starter.key} className={styles.starter} onPress={starter.onPress}>
                <FaIcon aria-hidden="true" icon={starter.icon} />
                <PromptSuggestion.ItemTitle>{t(`landing.hero.starters.${starter.key}`)}</PromptSuggestion.ItemTitle>
              </PromptSuggestion.Item>
            ))}
          </PromptSuggestion.Items>
        </PromptSuggestion>
      </HeroHeader.Content>
    </HeroHeader>
  );
}
