"use client";

import dynamic from "next/dynamic";
import { useState } from "react";
import { Accordion, Button, Chip, Link, Tooltip } from "@heroui/react";
import { supportedLocales, useLocalizedText, useSetLocaleCookie } from "@vitarish/localization";
import { FaIcon, FeatureItemCard, MarketingActionLink, ViewportReveal } from "@vitarish/ui";
import { chromaChipClassName } from "@vitarish/ui/chroma";
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
  registered: { light: "/Master-Hel-Logotype-Dark-®.svg", dark: "/Master-Hel-Logotype-®.svg" },
  copyright: { light: "/Master-Hel-Logotype-Dark-©.svg", dark: "/Master-Hel-Logotype-©.svg" },
} as const;

const GhostFibers = dynamic(() => import("./ghost-fibers"), { ssr: false });

const sectionIds = { apps: "landing-apps", questions: "landing-questions", purpose: "brand-statement-title" } as const;

const upcomingBusinessKeys = ["gym", "salon"] as const;
const upcomingIndividualKeys = ["etymology", "converter"] as const;
const upcomingChipClassName = chromaChipClassName("tertiary");

const darmanLandingUrl = "https://darman.vitarish.com";

const questionKeys = ["what"] as const;

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

/** The public brand and its released product, Darman. */
export function FrontDoorPage({ onboardingUrl, whatsappNumber }: FrontDoorPageProps) {
  const [backgroundPaused, setBackgroundPaused] = useState(false);
  const { t, locale } = useLocalizedText();
  const setLocaleCookie = useSetLocaleCookie();
  const otherLocale = supportedLocales.find((candidate) => candidate !== locale);

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <Link className={styles.headerBrand} href={`#${sectionIds.apps}`}>{t("landing.brand.name")}</Link>
        <nav className={styles.headerNav} aria-label={t("landing.apps.title")}>
          <Link href={`#${sectionIds.apps}`}>{t("landing.hero.explore")}</Link>
          <Link href={`#${sectionIds.questions}`}>{t("landing.footer.questions")}</Link>
        </nav>
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
          <div className={styles.heroScene}>
            <div className={styles.fibers} aria-hidden="true">
              <GhostFibers glowColor="white" lineColor="white" secondaryColor="white" threads={36} density={0.6} layers={2} dpr={1} speed={0.3} mouseInteraction={false} pulseOnClick={false} paused={backgroundPaused} />
            </div>
            <div className={styles.heroSignature}>
              <p className={styles.eyebrow}>{t("landing.hero.eyebrow")}</p>
              <Logotype alt={t("landing.brand.name")} className={styles.heroLogotype} mark="registered" />
            </div>
            <div className={styles.heroText}>
              <h1 className={styles.heroTitle} id="landing-hero">
                <span className={styles.heroLead}>{t("landing.hero.lead")}</span>
              </h1>
              <div className={styles.heroSummary}>
                <p className={styles.heroIntro}>{t("landing.hero.intro")}</p>
                <div className={styles.actions}>
                  <MarketingActionLink action={{ label: t("landing.hero.explore"), href: `#${sectionIds.apps}`, tooltip: t("landing.hero.explore"), icon: <FaIcon aria-hidden="true" icon="grid-2" /> }} />
                </div>
              </div>
            </div>
            <Tooltip>
              <Button isIconOnly aria-label={t(backgroundPaused ? "landing.hero.play" : "landing.hero.pause")} className={styles.motionControl} variant="ghost" onPress={() => setBackgroundPaused((value) => !value)}>
                <i aria-hidden="true" className={backgroundPaused ? "fa-light fa-play" : "fa-light fa-pause"} />
              </Button>
              <Tooltip.Content>{t(backgroundPaused ? "landing.hero.play" : "landing.hero.pause")}</Tooltip.Content>
            </Tooltip>
          </div>
        </section>
        <ViewportReveal as="section" className={styles.statement} aria-labelledby={sectionIds.purpose}>
          <p className={styles.eyebrow}>{t("landing.purpose.eyebrow")}</p>
          <h2 id={sectionIds.purpose}>{t("landing.purpose.title")}</h2>
          <p className={styles.sectionText}>{t("landing.purpose.description")}</p>
        </ViewportReveal>
        <ViewportReveal as="section" aria-labelledby={sectionIds.apps} className={styles.band}>
          <div className={styles.sectionIntro}>
            <p className={styles.eyebrow}>{t("landing.apps.eyebrow")}</p>
            <h2 className={styles.display} id={sectionIds.apps}>{t("landing.apps.title")}</h2>
            <p className={styles.sectionText}>{t("landing.apps.intro")}</p>
          </div>
          <FeatureItemCard className={styles.app} variant="secondary">
            <FeatureItemCard.Icon>
              <FaIcon aria-hidden="true" icon="user-doctor" size="xl" />
            </FeatureItemCard.Icon>
            <FeatureItemCard.Content>
              <p className={styles.eyebrow}>{t("landing.apps.practice.status")}</p>
              <FeatureItemCard.Title className={styles.appTitle}>{t("landing.apps.practice.title")}</FeatureItemCard.Title>
              <p className={styles.productLead}>{t("landing.apps.practice.lead")}</p>
              <FeatureItemCard.Description className={styles.appText}>{t("landing.apps.practice.description")}</FeatureItemCard.Description>
            </FeatureItemCard.Content>
            <FeatureItemCard.Action className={`${styles.appAction} flex flex-wrap gap-3`}>
              <MarketingActionLink
                action={{
                  label: t("landing.apps.practice.discover"),
                  href: darmanLandingUrl,
                  tone: "outline",
                  tooltip: t("landing.apps.practice.discoverTooltip"),
                  icon: <FaIcon aria-hidden="true" icon="book-open" size="1x" />,
                }}
              />
              {onboardingUrl && (
                <MarketingActionLink
                  action={{
                    label: t("landing.apps.practice.open"),
                    href: onboardingUrl,
                    tooltip: t("landing.apps.practice.openTooltip"),
                    icon: <FaIcon aria-hidden="true" icon="hospital" size="1x" />,
                  }}
                />
              )}
            </FeatureItemCard.Action>
            <p className={styles.productNote}>{t("landing.apps.practice.note")}</p>
          </FeatureItemCard>
        </ViewportReveal>
        <ViewportReveal as="section" aria-labelledby={sectionIds.questions} className={styles.band}>
          <h2 className={styles.display} id={sectionIds.questions}>{t("landing.questions.title")}</h2>
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
        </ViewportReveal>
      </main>
      <footer className={styles.footer}>
        <div className={styles.footerInner}>
          <div className={styles.footerColumns}>
            <div>
              <h2>{t("landing.brand.name")}</h2>
              <p>{t("landing.hero.lead")}</p>
            </div>
            <div>
              <h2>{t("landing.footer.businesses")}</h2>
              <ul>
                <li>
                  <Link className={styles.footerProduct} href={darmanLandingUrl}>
                    <span>{t("landing.apps.practice.title")}</span>
                    <Chip color="success" size="sm" variant="soft">{t("landing.footer.ready")}</Chip>
                  </Link>
                </li>
                {upcomingBusinessKeys.map((key) => (
                  <li className={styles.footerProduct} key={key}>
                    <span>{t(`landing.footer.products.${key}`)}</span>
                    <Chip className={upcomingChipClassName} size="sm" variant="soft">{t("landing.footer.soon")}</Chip>
                  </li>
                ))}
              </ul>
            </div>
            <div>
              <h2>{t("landing.footer.individuals")}</h2>
              <ul>
                {upcomingIndividualKeys.map((key) => (
                  <li className={styles.footerProduct} key={key}>
                    <span>{t(`landing.footer.products.${key}`)}</span>
                    <Chip className={upcomingChipClassName} size="sm" variant="soft">{t("landing.footer.soon")}</Chip>
                  </li>
                ))}
              </ul>
            </div>
          </div>
          <p className={styles.footerDisclosure}>{t("landing.footer.disclosure")}</p>
          <div className={styles.footerBottom}>
            <p>{t("landing.footer.rights")}</p>
          </div>
        </div>
      </footer>
    </div>
  );
}
