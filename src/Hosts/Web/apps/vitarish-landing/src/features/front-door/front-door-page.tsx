"use client";

import { Accordion, Button, Link, Tooltip } from "@heroui/react";
import { setLocaleCookie, supportedLocales, useLocalizedText } from "@vitarish/localization";
import { FaIcon, FeatureItemCard, MarketingActionLink } from "@vitarish/ui";
import { whatsappChatUrl } from "./whatsapp";
import styles from "./front-door.module.css";

interface FrontDoorPageProps {
  /** Where people create their own clinic; null when this deployment offers none. */
  onboardingUrl: string | null;
  /** The WhatsApp number that answers "talk to a person"; null when none is configured. */
  whatsappNumber: string | null;
}

// Each mark has dark ink for the light theme and white ink for the dark theme; an inverted ground swaps them.
const logotypes = {
  mark: { light: "/Logo-Dark-®.svg", dark: "/Logo-®.svg" },
  registered: { light: "/Master-Hel-Logotype-Dark-®.svg", dark: "/Master-Hel-Logotype-®.svg" },
  copyright: { light: "/Master-Hel-Logotype-Dark-©.svg", dark: "/Master-Hel-Logotype-©.svg" },
} as const;

const questionKeys = ["what", "who", "cost", "languages"] as const;

function Logotype({
  mark,
  alt,
  className,
  isInverted = false,
}: {
  mark: keyof typeof logotypes;
  alt: string;
  className?: string;
  isInverted?: boolean;
}) {
  const { light, dark } = logotypes[mark];
  return (
    <span className={className}>
      <img alt={alt} className="block h-full w-auto dark:hidden" src={isInverted ? dark : light} />
      <img alt={alt} className="hidden h-full w-auto dark:block" src={isInverted ? light : dark} />
    </span>
  );
}

/** The public front door: the Vitarish apps, a few questions, and the brand. */
export function FrontDoorPage({ onboardingUrl, whatsappNumber }: FrontDoorPageProps) {
  const { t, locale } = useLocalizedText();
  const otherLocale = supportedLocales.find((candidate) => candidate !== locale);

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <a aria-label={t("landing.brand.home")} className={styles.brand} href="/">
          <Logotype alt="" className={styles.headerMark} mark="mark" />
        </a>
        {otherLocale && (
          <MarketingActionLink
            action={{
              label: t("landing.header.language"),
              tone: "outline",
              tooltip: t("landing.header.languageTooltip"),
              icon: <FaIcon aria-hidden="true" icon="language" size="1x" />,
              onPress: () => {
                setLocaleCookie(otherLocale);
                window.location.reload();
              },
            }}
            className={styles.headerAction}
          />
        )}
      </header>
      <main className={styles.main} id="vitarish-landing-main">
        <section aria-labelledby="landing-hero" className={styles.hero}>
          <Logotype alt={t("landing.brand.name")} className={styles.heroLogotype} mark="registered" />
          <div className={styles.heroText}>
            <h1 className={styles.heroTitle} id="landing-hero">
              <span className={styles.heroLead}>{t("landing.hero.lead")}</span>
              <span className={styles.heroFollow}>{t("landing.hero.follow")}</span>
            </h1>
            <p className={styles.heroIntro}>{t("landing.hero.intro")}</p>
          </div>
        </section>
        <section aria-labelledby="landing-apps" className={styles.band}>
          <h2 className={styles.display} id="landing-apps">{t("landing.apps.title")}</h2>
          <FeatureItemCard className={styles.app} variant="secondary">
            <FeatureItemCard.Icon>
              <FaIcon aria-hidden="true" icon="user-doctor" size="xl" />
            </FeatureItemCard.Icon>
            <FeatureItemCard.Content>
              <FeatureItemCard.Title className={styles.appTitle}>{t("landing.apps.practice.title")}</FeatureItemCard.Title>
              <FeatureItemCard.Description className={styles.appText}>{t("landing.apps.practice.description")}</FeatureItemCard.Description>
            </FeatureItemCard.Content>
            {onboardingUrl && (
              <FeatureItemCard.Action className={styles.appAction}>
                <MarketingActionLink
                  action={{
                    label: t("landing.apps.practice.open"),
                    href: onboardingUrl,
                    tooltip: t("landing.apps.practice.openTooltip"),
                    icon: <FaIcon aria-hidden="true" icon="hospital" size="1x" />,
                  }}
                />
              </FeatureItemCard.Action>
            )}
          </FeatureItemCard>
        </section>
        <section aria-labelledby="landing-questions" className={styles.band}>
          <h2 className={styles.display} id="landing-questions">{t("landing.questions.title")}</h2>
          <Accordion className={styles.questions}>
            {questionKeys.map((key) => (
              <Accordion.Item key={key} id={key}>
                <Accordion.Heading>
                  <Accordion.Trigger className={styles.question}>
                    {t(`landing.questions.${key}.question`)}
                    <Accordion.Indicator className={styles.questionIndicator}>
                      <FaIcon aria-hidden="true" icon="plus" />
                    </Accordion.Indicator>
                  </Accordion.Trigger>
                </Accordion.Heading>
                <Accordion.Panel>
                  <Accordion.Body className={styles.answer}>{t(`landing.questions.${key}.answer`)}</Accordion.Body>
                </Accordion.Panel>
              </Accordion.Item>
            ))}
            {whatsappNumber && (
              <Accordion.Item id="contact">
                <Accordion.Heading>
                  <Accordion.Trigger className={styles.question}>
                    {t("landing.questions.contact.question")}
                    <Accordion.Indicator className={styles.questionIndicator}>
                      <FaIcon aria-hidden="true" icon="plus" />
                    </Accordion.Indicator>
                  </Accordion.Trigger>
                </Accordion.Heading>
                <Accordion.Panel>
                  <Accordion.Body className={styles.answer}>
                    {t("landing.questions.contact.answer")}{" "}
                    <Link
                      href={whatsappChatUrl(whatsappNumber, t("landing.questions.contact.message"))}
                      rel="noreferrer"
                      target="_blank"
                    >
                      {t("landing.questions.contact.link")}
                    </Link>
                  </Accordion.Body>
                </Accordion.Panel>
              </Accordion.Item>
            )}
          </Accordion>
        </section>
      </main>
      <footer className={styles.footer}>
        <div className={styles.footerTop}>
          <Tooltip>
            <Button
              isIconOnly
              aria-label={t("landing.footer.top")}
              className={styles.toTop}
              variant="ghost"
              onPress={() => window.scrollTo({ top: 0, behavior: "smooth" })}
            >
              <FaIcon aria-hidden="true" icon="arrow-up" size="2x" />
            </Button>
            <Tooltip.Content>{t("landing.footer.top")}</Tooltip.Content>
          </Tooltip>
        </div>
        <div className={styles.footerBottom}>
          <p className={styles.footerNote}>
            {t("landing.footer.description")}
            <br />
            {t("landing.footer.rights")}
          </p>
          {whatsappNumber && (
            <Link
              className={styles.footerContact}
              href={whatsappChatUrl(whatsappNumber, t("landing.questions.contact.message"))}
              rel="noreferrer"
              target="_blank"
            >
              {t("landing.questions.contact.link")}
            </Link>
          )}
        </div>
        <Logotype isInverted alt={t("landing.brand.name")} className={styles.footerLogotype} mark="copyright" />
      </footer>
    </div>
  );
}
